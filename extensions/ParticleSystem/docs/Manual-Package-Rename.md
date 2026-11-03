# Manual `Package` rename

#### 1️⃣ Customize `Packages/com.ivanmurzak.unity.mcp.particlesystem/package.json`

- 👉 **Update** `name`
  > Sample: `com.github.your_name.package`
  > Instead of the word `package` use a word or couple of words that explains the main purpose of the package.

- 👉 **Update** `unity` to setup minimum supported Unity version
- 👉 **Update** `displayName`, `version`, `description`, `author`, `keywords` to your needs

#### 2️⃣ Do you need Tests?

<details>
  <summary><b>❌ NO - click</b></summary>

  - 👉 **Delete** `Packages/com.ivanmurzak.unity.mcp.particlesystem/Tests` folder
  - 👉 **Delete** `.github/workflows` folder

</details>

<details>
  <summary><b>✅ YES - click</b></summary>

  - 👉 **Repeat** these actions for these files.

  - Update the files:
    - `Packages/com.ivanmurzak.unity.mcp.particlesystem/Tests/Base/Package.Editor.Tests.asmdef`
    - `Packages/com.ivanmurzak.unity.mcp.particlesystem/Tests/Base/Package.Tests.asmdef`

  - Apply these actions to files above:
    - 👉 **Rename** the `Package` part of the file name
    - 👉 **Replace** the `Package` keyword in the file content (multiple places)

</details>
