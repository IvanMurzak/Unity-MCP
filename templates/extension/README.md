<h1 align="center"><a href="https://github.com/IvanMurzak/Unity-AI-Tools-Template">Unity AI Tools Template</a></h1>

<img width="100%" alt="Stats" src="https://github.com/IvanMurzak/Unity-AI-Tools-Template/raw/main/docs/img/ai-template-glitch.gif"/>

Template for AI MCP Tools for [AI Game Developer (Unity-MCP)](https://github.com/IvanMurzak/Unity-MCP). Use this template to create your custom MCP tools for Unity Engine in 30 minutes. Read more about custom MCP tools [here](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#add-custom-mcp-tool).

This template repository is designed to be easily updated into a real Unity package. Please follow the instruction bellow, it will help you to go through the entire process of package creation, distribution and installing.

# Steps to make your package

### 1️⃣ Click the button - create repository

[![create new repository](https://user-images.githubusercontent.com/9135028/198753285-3d3c9601-0711-43c7-a8f2-d40ec42393a2.png)](https://github.com/IvanMurzak/Unity-AI-Tools-Template/generate)

### 2️⃣ Clone your new repository

### 3️⃣ Initialize Project

Use the initialization script to rename the package and replace all placeholders.

```powershell
./commands/init.ps1 -PackageId "com.company.package" -PackageName "My Package" -GitHubRepository "user/repo"
# optional: -FeatureName "MyFeature"  (C# identifier for the RQL module; default: the package name without spaces)
```

This script will:
- Rename directories and files.
- Replace `YOUR_PACKAGE_ID`, `YOUR_PACKAGE_NAME`, `YOUR_FEATURE_NAME`, etc. in all files and folder names.

### 4️⃣ Update `package.json`
Open `Unity-Package/Packages/YOUR_PACKAGE_ID_LOWERCASE/package.json` and update:
- `description`
- `author`
- `keywords`
- `unity` (minimum supported Unity version)

2. **Generate Meta Files**
   To ensure all Unity meta files are correctly generated:
   - Open Unity Hub.
   - Add the `Installer` folder as a project.
   - Add the `Unity-Package` folder as a project.
   - Open both projects in Unity Editor. This will generate the necessary `.meta` files.

### 5️⃣ Generate Meta Files

#### Using script
   Open Unity project to generate `.meta` files.
   **On Mac and Linux**:
   ```bash
   ./commands/open-all-projects-unix.sh
   ```
   **On Windows**:
   ```bash
   ./commands/open-all-projects-windows.ps1
   ```
#### OR Manually
   You may open the projects manually to achieve the same result.
   - Open Unity Hub.
   - Add the `Installer` folder as a project.
   - Add the `Unity-Package` folder as a project.
   - Open both projects in Unity Editor. This will generate the necessary `.meta` files.

### 6️⃣ Teach AI agents your feature (RQL first)

AI Game Developer gives agents three generic tools: `rql-query`, `rql-mutate` and `rql-schema`
([RQL](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/dev/rql.md), a query language over the Unity scene, assets and
Editor). An extension does **not** add MCP tools for reading or editing data. It adds an **RQL module** that teaches RQL its
types, and a short guide that is rendered into the `unity-rql` skill agents read. The template ships a working example in
`Unity-Package/Packages/YOUR_PACKAGE_ID_LOWERCASE/Editor/Scripts/Rql/` (replace it with your feature):

| File | What it shows |
|---|---|
| `YOUR_FEATURE_NAMEModule.cs` | `IRqlModule` (`Order => 100`), discovered automatically when the package is installed: registers the source, the adapter and `ctx.AddGuide(topic, markdown)` with example queries |
| `UnityEngine_Light_Adapter.cs` | `RqlTypeAdapter<Light>`: a virtual member (`castsShadows`) and a callable method (`ScaleIntensity`). One adapter per file, named after the full type name |
| `YOUR_FEATURE_NAMEEditorApi.cs` | the `YOUR_FEATURE_NAME_LOWERCASE` source: an `[RqlCallable]` operation without an owner object (`YOUR_FEATURE_NAME_LOWERCASE \| call CreateLight("Lamp", "Spot")`) |

What to add, in this order:

1. **Nothing**, if plain reflection is enough: public members of your components and assets are already readable and
   writable (`objects | get components | where $ is MyComponent | set speed = 3`).
2. **An adapter** (`RqlTypeAdapter<T>`) to add virtual members and methods, hide or guard members, validate writes, or
   parse literals. API: [type adapters](https://github.com/IvanMurzak/RQL/blob/main/docs/type-adapters.md).
3. **A source** (`ctx.AddSource`) for operations that have no owner object yet (create, import), or **functions**
   (`ctx.AddFunction`) for read-only computations usable in `rql-query`.
4. **A guide** (`ctx.AddGuide`): what agents need to know plus example queries. The test `EveryGuideExampleRuns` executes
   every example, so keep them runnable.

Name-clash rule: code in `com.IvanMurzak.Unity.MCP.Editor.Rql.*` resolves simple names against the enclosing namespaces
(other extensions add `...Rql.Animation`, `...Rql.Terrains` …) before the file's `using` directives. Declare aliases for the
Unity types you use inside the namespace block (`using Light = UnityEngine.Light;`), and do not name the feature namespace
after a Unity type you reference. `init.ps1 -FeatureName` sets the identifier (default: the package name without spaces).

Tests live in `Tests/Editor/Rql/`: a base class (fresh scene, `RqlUnity.Reset()`, `Q()` for queries, `M()` for mutations),
one test file per adapter / source, and module tests (discovery, the guide in `Skill_Rql.Render()`, every guide example,
one call through the MCP pipeline).

#### An MCP tool only for what RQL cannot express

Add a classic MCP tool only for binary output (images, files), long-running or asynchronous work (builds, bakes, imports
that span frames), or calls to external processes. Decide where it runs:

- **Editor** (`Unity-Package/Packages/YOUR_PACKAGE_ID_LOWERCASE/Editor`): Edit and Play Mode, Editor API, not in game builds.
- **Runtime** (`Unity-Package/Packages/YOUR_PACKAGE_ID_LOWERCASE/Runtime`): Edit and Play Mode and game builds, no Editor API.

> Read detailed instructions about custom tool development [here](https://github.com/IvanMurzak/Unity-MCP?tab=readme-ov-file#add-custom-mcp-tool).

```csharp
[AiToolType]
public partial class Tool_MyFeature
{
    [AiTool("my-feature-bake", Title = "My Feature / Bake")]
    [Description("Put here the tool description for LLM.")]
    public Task<bool> Bake
    (
        [Description("Add description to the input property, help LLM better understand it.")]
        string assetPath
    )
    {
        // validate arguments here (any thread)
        return MainThread.Instance.RunAsync(() =>
        {
            // Unity API calls here (main thread)

            return true;
        });
    }
}
```

---

# Optional steps

### 1. Version Management

To update the package version across all files (package.json, Installer.cs, etc.), use the bump version script:

```powershell
.\commands\bump-version.ps1 -NewVersion "1.0.1"
```

### 2. Setup CI/CD

To enable automatic testing and deployment:

1.  **Configure GitHub Secrets**
    Go to `Settings` > `Secrets and variables` > `Actions` > `New repository secret` and add:
    -   `UNITY_EMAIL`: Your Unity account email.
    -   `UNITY_PASSWORD`: Your Unity account password.
    -   `UNITY_LICENSE`: Content of your `Unity_lic.ulf` file.
        -   Windows: `C:/ProgramData/Unity/Unity_lic.ulf`
        -   Mac: `/Library/Application Support/Unity/Unity_lic.ulf`
        -   Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`

2.  **Enable Workflows**
    Rename the sample workflow files to enable them:
    -   `.github/workflows/release.yml-sample` ➡️ `.github/workflows/release.yml`
    -   `.github/workflows/test_pull_request.yml-sample` ➡️ `.github/workflows/test_pull_request.yml`

3.  **Update Unity Version**
    Open both `.yml` files and update the `UNITY_VERSION` (or similar variable) to match your project's Unity Editor version.

4.  **Automatic Deployment**
    The release workflow triggers automatically when you push to the `main` branch with an incremented version in `package.json`.

### 3. Add files into `Unity-Package/Packages/YOUR_PACKAGE_ID_LOWERCASE` folder

[Unity guidelines](https://docs.unity3d.com/Manual/cus-layout.html) about organizing files into the package root directory

```text
  <root>
  ├── package.json
  ├── README.md
  ├── CHANGELOG.md
  ├── LICENSE.md
  ├── Third Party Notices.md
  ├── Editor
  │   ├── [company-name].[package-name].Editor.asmdef
  │   └── EditorExample.cs
  ├── Runtime
  │   ├── [company-name].[package-name].asmdef
  │   └── RuntimeExample.cs
  ├── Tests
  │   ├── Editor
  │   │   ├── [company-name].[package-name].Editor.Tests.asmdef
  │   │   └── EditorExampleTest.cs
  │   └── Runtime
  │        ├── [company-name].[package-name].Tests.asmdef
  │        └── RuntimeExampleTest.cs
  ├── Samples~
  │        ├── SampleFolder1
  │        ├── SampleFolder2
  │        └── ...
  └── Documentation~
       └── [package-name].md
```

# Final polishing

- Update the `README.md` file (this file) with information about your package.
- Copy the updated `README.md` to `Packages/YOUR_PACKAGE_ID_LOWERCASE` as well.

> ⚠️ Everything outside of the `root` folder won't be added to your package. But still could be used for testing or showcasing your package at your repository.

### 1. Deploy to any registry you like

- [Deploy to OpenUPM](https://github.com/IvanMurzak/Unity-Package-Template/blob/main/Docs/Deploy-OpenUPM.md) (recommended)
- [Deploy using GitHub](https://github.com/IvanMurzak/Unity-Package-Template/blob/main/Docs/Deploy-GitHub.md)
- [Deploy to npmjs.com](https://github.com/IvanMurzak/Unity-Package-Template/blob/main/Docs/Deploy-npmjs.md)

### 2. Install your package into Unity Project

When your package is distributed, you can install it into any Unity project.

> Don't install into the same Unity project, please use another one.

- [Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- Open a command line at the root of Unity project (the folder which contains `Assets`)
- Execute the command (for `OpenUPM` hosted package)

  ```bash
  openupm add YOUR_PACKAGE_NAME
  ```

# Final view in Unity Package Manager

![image](https://user-images.githubusercontent.com/9135028/198777922-fdb71949-aee7-49c8-800f-7db885de9453.png)
