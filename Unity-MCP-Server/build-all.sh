#!/bin/bash
# Universal bash script to build self-contained executables for all platforms
# Works on macOS and Linux

set -uo pipefail

CONFIGURATION="Release"
PROJECT_FILE="com.IvanMurzak.Unity.MCP.Server.csproj"
SPECIFIED_PLATFORMS=()

all_runtimes=(
    "win-x64"
    "win-x86"
    "win-arm64"
    "linux-x64"
    "linux-arm64"
    "osx-x64"
    "osx-arm64"
)

# Argument parsing
while [[ $# -gt 0 ]]; do
    case $1 in
        -c|--configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        Debug|Release)
            CONFIGURATION="$1"
            shift
            ;;
        *.csproj)
            PROJECT_FILE="$1"
            shift
            ;;
        *)
            # Check if it's a known runtime
            is_runtime=false
            for runtime in "${all_runtimes[@]}"; do
                if [ "$1" == "$runtime" ]; then
                    SPECIFIED_PLATFORMS+=("$1")
                    is_runtime=true
                    break
                fi
            done
            if [ "$is_runtime" = false ]; then
                echo "Unknown argument: $1"
                echo "Usage: $0 [Debug|Release] [-c|--configuration <config>] [*.csproj] [runtime...]"
                echo "Known runtimes: ${all_runtimes[*]}"
                exit 1
            fi
            shift
            ;;
    esac
done

SCRIPT_DIR="$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
PUBLISH_ROOT="${SCRIPT_DIR}/publish"

echo "🚀 Building self-contained executables..."

# Clean publish root
if [ -d "${PUBLISH_ROOT}" ]; then
    echo "🧹 Cleaning existing publish folder..."
    rm -rf "${PUBLISH_ROOT}" || { echo "Failed to remove publish folder"; exit 1; }
fi
mkdir -p "${PUBLISH_ROOT}" || { echo "Failed to create publish folder"; exit 1; }

# Filter runtimes if platforms are specified
runtimes=()
if [ ${#SPECIFIED_PLATFORMS[@]} -gt 0 ]; then
    runtimes=("${SPECIFIED_PLATFORMS[@]}")
else
    runtimes=("${all_runtimes[@]}")
fi

echo "Configuration: ${CONFIGURATION}"
echo "Project File: ${PROJECT_FILE}"
echo "Target runtimes: ${runtimes[*]}"
echo ""

success=0
failed=0

for runtime in "${runtimes[@]}"; do
    echo "🔨 Building for ${runtime}..."

    OUTPUT_PATH="${PUBLISH_ROOT}/${runtime}"
    mkdir -p "${OUTPUT_PATH}"

    echo "Running: dotnet publish ${PROJECT_FILE} -c ${CONFIGURATION} -r ${runtime} --self-contained true -p:PublishSingleFile=true -o ${OUTPUT_PATH}"
    if dotnet publish "${PROJECT_FILE}" \
        -c "${CONFIGURATION}" \
        -r "${runtime}" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -o "${OUTPUT_PATH}" 2>&1; then
        echo "✅ Successfully built ${runtime} -> ${OUTPUT_PATH}"
        ((success++))
    else
        echo "❌ Failed to build ${runtime}"
        ((failed++))
    fi
    echo ""
done

echo "📊 Build Summary:"
echo "Success: $success"
echo "Failed: $failed"

if [ $failed -eq 0 ]; then
    echo ""
    echo "🎉 All builds completed successfully!"
    echo "📁 Executables are located in: ${PUBLISH_ROOT}/{runtime}/"

    echo ""
    echo "📦 Creating zip archives for each runtime..."

    # Change to publish directory
    cd "${PUBLISH_ROOT}"

    zip_success=0
    zip_failed=0

    for runtime in "${runtimes[@]}"; do
        if [ -d "${runtime}" ]; then
            echo "🗜️  Creating zip for ${runtime}..."

            ZIP_NAME="unity-mcp-server-${runtime}.zip"

            if zip -r "${ZIP_NAME}" "${runtime}/" > /dev/null 2>&1; then
                echo "✅ Successfully created ${ZIP_NAME}"
                ((zip_success++))
            else
                echo "❌ Failed to create ${ZIP_NAME}"
                ((zip_failed++))
            fi
        else
            echo "⚠️  Skipping ${runtime} - directory not found"
            ((zip_failed++))
        fi
    done

    echo ""
    echo "📊 Zip Creation Summary:"
    echo "Success: $zip_success"
    echo "Failed: $zip_failed"

    if [ $zip_failed -eq 0 ]; then
        echo ""
        echo "🎉 All zip archives created successfully!"
        echo "📁 Zip files are located in: ${PUBLISH_ROOT}/"
        echo "📋 Created files:"
        ls -la *.zip 2>/dev/null || echo "No zip files found"
    else
        echo ""
        echo "⚠️  Some zip creations failed. Check the output above."
    fi
else
    echo ""
    echo "⚠️  Some builds failed. Check the output above. Partial outputs: ${PUBLISH_ROOT}"
    exit 1
fi
