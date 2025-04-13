# Unity MCP (Server + Plugin)

<img width="100%" alt="Stats" src="https://user-images.githubusercontent.com/9135028/198754538-4dd93fc6-7eb2-42ae-8ac6-d7361c39e6ef.gif"/>

[![openupm](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) ![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP) [![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

| Unity Version | Editmode | Playmode | Standalone |
|---------------|----------|----------|------------|
| 2022.3.61f1   | ![2022.3.61f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/2022.3.61f1_editmode.yml?label=2022.3.61f1-editmode) | ![2022.3.61f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/2022.3.61f1_playmode.yml?label=2022.3.61f1-playmode) | ![2022.3.61f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/2022.3.61f1_standalone.yml?label=2022.3.61f1-standalone) |
| 2023.2.20f1   | ![2023.2.20f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/2023.2.20f1_editmode.yml?label=2023.2.20f1-editmode) | ![2023.2.20f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/2023.2.20f1_playmode.yml?label=2023.2.20f1-playmode) | ![2023.2.20f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/2023.2.20f1_standalone.yml?label=2023.2.20f1-standalone) |
| 6000.0.46f1   | ![6000.0.46f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/6000.0.46f1_editmode.yml?label=6000.0.46f1-editmode) | ![6000.0.46f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/6000.0.46f1_playmode.yml?label=6000.0.46f1-playmode) | ![6000.0.46f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/6000.0.46f1_standalone.yml?label=6000.0.46f1-standalone) |

AI gate to Unity Editor and Unity player's build.

Supports custom `tool` if any exists in your source code of a Unity project.

## AI Tools

### GameObject

- Create
- Destroy
- Add Component
- Remove Component
- Find by name (optional recursive)
- Find by path (optional recursive)

### Component

- Get All from the project (optional search by substring)

### Prefabs

### Materials

### Scripts

## Add custom `tool`

To add a custom `tool` just need to create `.cs` file in your project source. There is a sample a tool.

> Take a look that the line `=> MainThread.Run(() =>` it allows to run the code in Main thread which is needed to interact with Unity API. If you don't need it and running the tool in background thread is fine for the tool, don't use Main thread for efficience purpose.

```csharp
public partial class Tool_GameObject
{
    [McpPluginTool
    (
        "GameObject_Create",
        Title = "Create a new GameObject",
        Description = "Create a new GameObject."
    )]
    public string Create
    (
        [Description("Path to the GameObject (excluding the name of the GameObject).")]
        string path,
        [Description("Name of the GameObject.")]
        string name
    )
    => MainThread.Run(() =>
    {
        var targetParent = string.IsNullOrEmpty(path) ? null : GameObject.Find(path);
        if (targetParent == null && !string.IsNullOrEmpty(path))
            return $"[Error] Parent GameObject '{path}' not found.";

        var go = new GameObject(name);
        go.transform.position = new Vector3(0, 0, 0);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = new Vector3(1, 1, 1);
        if (targetParent != null)
            go.transform.SetParent(targetParent.transform, false);

        EditorUtility.SetDirty(go);
        EditorApplication.RepaintHierarchyWindow();

        return $"[Success] Created GameObject '{name}' at path '{path}'.";
    });
}
```

# Steps to make your package

#### 1️⃣ Click the button to create new repository on GitHub using this template.

[![create new repository](https://user-images.githubusercontent.com/9135028/198753285-3d3c9601-0711-43c7-a8f2-d40ec42393a2.png)](https://github.com/IvanMurzak/Unity-Package-Template/generate)

#### 2️⃣ Clone your new repository and open it in Unity Editor

#### 3️⃣ Rename `Package`

Your package should have unique identifier. It is called a `name` of the package. It support only limited symbols. There is a sample of the package name.

```text
com.github.your_name.package
```

- 👉 Instead of the word `package` use a word or couple of words that explains the main purpose of the package.
- 👉 The `name` should be unique in the world.

###### Option 1: Use script to rename package (recommended)

For MacOS

```bash

```

For Windows

```bash
cd Commands
.\package_rename.bat Username PackageName
```

###### Option 2: Manual package rename

Follow the instruction - [manual package rename](https://github.com/IvanMurzak/Unity-Package-Template/blob/main/Docs/Manual-Package-Rename.md)


#### 3️⃣ Customize `Assets/root/package.json`

- 👉 **Update** `name`
  > Sample: `com.github.your_name.package`
  > Instead of the word `package` use a word or couple of words that explains the main purpose of the package.
  > The `name` should be unique in the world.

- 👉 **Update** `unity` to setup minimum supported Unity version
- 👉 **Update**
  - `displayName` - visible name of the package,
  - `version` - the version of the package (1.0.0),
  - `description` - short description of the package,
  - `author` - author of the package and url to the author (could be GitHub profile),
  - `keywords` - array of keywords that describes the package.

#### 4️⃣ Do you need Tests?

<details>
  <summary><b>❌ NO</b></summary>

- 👉 **Delete** `Assets/root/Tests` folder
- 👉 **Delete** `.github/workflows` folder

</details>

<details>
  <summary><b>✅ YES</b></summary>

- 👉 Make sure you executed `package-rename` script from the step #2. If not, please follow [manual package rename](https://github.com/IvanMurzak/Unity-Package-Template/blob/main/Docs/Manual-Package-Rename.md) instructions

- 👉 Add GitHub Secrets
  > At the GitHub repository, go to "Settings", then "Secrets and Variables", then "Actions", then click on "New repository secret"
   1. Add `UNITY_EMAIL` - email of your Unity ID's account
   2. Add `UNITY_PASSWORD` - password of your Unity ID's account
   3. Add `UNITY_LICENSE` - license content. Could be taken from `Unity_lic.ulf` file. Just open it in any text editor and copy the entire content
      1. Windows: The `Unity_lic.ulf` file is located at `C:/ProgramData/Unity/Unity_lic.ulf`
      2. MacOS: `/Library/Application Support/Unity/Unity_lic.ulf`
      3. Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`

</details>

#### 4️⃣ Add files into `Assets/root` folder

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

##### Final polishing

- Update the `README.md` file (this file) with information about your package.
- Copy the updated `README.md` to `Assets/root` as well.

> ⚠️ Everything outside of the `root` folder won't be added to your package. But still could be used for testing or showcasing your package at your repository.

#### 5️⃣ Deploy to any registry you like

- [Deploy to OpenUPM](https://github.com/IvanMurzak/Unity-Package-Template/blob/main/Docs/Deploy-OpenUPM.md) (recommended)
- [Deploy using GitHub](https://github.com/IvanMurzak/Unity-Package-Template/blob/main/Docs/Deploy-GitHub.md)
- [Deploy to npmjs.com](https://github.com/IvanMurzak/Unity-Package-Template/blob/main/Docs/Deploy-npmjs.md)

#### 6️⃣ Install your package into Unity Project

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
