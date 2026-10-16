# Unity Package Template

<img width="100%" alt="Stats" src="https://user-images.githubusercontent.com/9135028/198754538-4dd93fc6-7eb2-42ae-8ac6-d7361c39e6ef.gif"/>

Unity Editor supports NPM packages. It is way more flexible solution in comparison with classic Plugin that Unity is using for years. NPM package supports versioning and dependencies. You may update / downgrade any package very easily. Also, Unity Editor has UPM (Unity Package Manager) that makes the process even simpler.

This template repository is designed to be easily updated into a real Unity package. Please follow the instruction bellow, it will help you to go through the entire process of package creation, distribution and installing.

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


#### 3️⃣ Customize `Packages/com.ivanmurzak.unity.mcp.particlesystem/package.json`

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

- 👉 **Delete** `Packages/com.ivanmurzak.unity.mcp.particlesystem/Tests` folder
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

#### 4️⃣ Add files into `Packages/com.ivanmurzak.unity.mcp.particlesystem` folder

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
- Copy the updated `README.md` to `Packages/com.ivanmurzak.unity.mcp.particlesystem` as well.

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
  openupm add AI Particle System
  ```

# Final view in Unity Package Manager

![image](https://user-images.githubusercontent.com/9135028/198777922-fdb71949-aee7-49c8-800f-7db885de9453.png)
