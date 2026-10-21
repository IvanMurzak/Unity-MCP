#!/usr/bin/env bash
#
# Opens all Unity projects in the repository using Unity Editor.
#
# Launches Unity Editor for each project by reading the required version
# from ProjectSettings/ProjectVersion.txt:
#   - Installer
#   - Unity-Package
#   - Unity-Tests/2022.3.62f3
#   - Unity-Tests/2023.2.22f1
#   - Unity-Tests/6000.3.1f1
#
# Usage:
#   ./open-all-projects-unix.sh
#   ./open-all-projects-unix.sh /custom/path/to/editors

set -e

# Root directory (parent of commands)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

# Editors path from argument or auto-detect
EDITORS_PATH="$1"

# Detect OS and set default paths
detect_editors_path() {
    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        local paths=(
            "/Applications/Unity/Hub/Editor"
            "$HOME/Applications/Unity/Hub/Editor"
        )
    else
        # Linux
        local paths=(
            "$HOME/Unity/Hub/Editor"
            "/opt/Unity/Hub/Editor"
            "/usr/local/Unity/Hub/Editor"
        )
    fi

    for path in "${paths[@]}"; do
        if [[ -d "$path" ]]; then
            echo "$path"
            return 0
        fi
    done

    return 1
}

# Get Unity executable path based on OS
get_unity_exe() {
    local editors_path="$1"
    local version="$2"

    if [[ "$OSTYPE" == "darwin"* ]]; then
        # macOS
        echo "$editors_path/$version/Unity.app/Contents/MacOS/Unity"
    else
        # Linux
        echo "$editors_path/$version/Editor/Unity"
    fi
}

# Auto-detect editors path if not provided
if [[ -z "$EDITORS_PATH" ]]; then
    EDITORS_PATH=$(detect_editors_path) || true
fi

# Verify editors path exists
if [[ -z "$EDITORS_PATH" ]] || [[ ! -d "$EDITORS_PATH" ]]; then
    echo "Error: Unity editors folder not found." >&2
    echo "Please specify the correct path as an argument." >&2
    echo "Example: ./open-all-projects-unix.sh /path/to/Unity/Hub/Editor" >&2
    exit 1
fi

# Define projects to open
PROJECTS=(
    "Installer"
    "Unity-Package"
    "Unity-Tests/2022.3.62f3"
    "Unity-Tests/2023.2.22f1"
    "Unity-Tests/6000.3.1f1"
)

echo "Opening Unity projects..."
echo "Editors path: $EDITORS_PATH"
echo ""

for project in "${PROJECTS[@]}"; do
    PROJECT_PATH="$REPO_ROOT/$project"

    if [[ ! -d "$PROJECT_PATH" ]]; then
        echo "Warning: Project not found: $PROJECT_PATH" >&2
        continue
    fi

    # Read the project version from ProjectSettings/ProjectVersion.txt
    VERSION_FILE="$PROJECT_PATH/ProjectSettings/ProjectVersion.txt"
    if [[ ! -f "$VERSION_FILE" ]]; then
        echo "Warning: ProjectVersion.txt not found for: $project" >&2
        continue
    fi

    # Parse the version (format: "m_EditorVersion: 2022.3.62f1")
    UNITY_VERSION=$(grep -oP 'm_EditorVersion:\s*\K.+' "$VERSION_FILE" 2>/dev/null || \
                    sed -n 's/m_EditorVersion: *//p' "$VERSION_FILE" | tr -d '\r')
    UNITY_VERSION=$(echo "$UNITY_VERSION" | xargs)  # Trim whitespace

    if [[ -z "$UNITY_VERSION" ]]; then
        echo "Warning: Could not parse Unity version for: $project" >&2
        continue
    fi

    # Find the Unity Editor executable
    UNITY_EXE=$(get_unity_exe "$EDITORS_PATH" "$UNITY_VERSION")
    if [[ ! -x "$UNITY_EXE" ]]; then
        echo "Warning: Unity $UNITY_VERSION not installed. Skipping: $project" >&2
        continue
    fi

    echo "Opening: $project (Unity $UNITY_VERSION)"

    # Launch Unity Editor with the project path (in background)
    "$UNITY_EXE" -projectPath "$PROJECT_PATH" &

    # Stagger between launches: Unity's LicensingClient and UPM package-cache
    # extraction both race when multiple Editors start within a few seconds of
    # each other. 2s is too short — the first Editor spawns its LicensingClient
    # (e.g. protocol 1.16.2 for Unity 2023.2+) and subsequent Editors of an
    # older version (e.g. 2022.3) fail to attach with "Unsupported protocol
    # version" + exit 199. The UPM cache also corrupts under ENOENT when two
    # Editors try to extract the same .tgz into the per-user cache at once.
    # 30 seconds gives the first Editor enough time to finish the licensing
    # handshake AND finish UPM extraction before the next one starts.
    sleep 30
done

echo ""
echo "Done! All projects are being opened."
