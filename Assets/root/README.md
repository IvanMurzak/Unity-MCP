# Unity MCP (Server + Plugin)

[![openupm](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) ![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP) [![Stand With Ukraine](https://raw.githubusercontent.com/vshymanskyy/StandWithUkraine/main/badges/StandWithUkraine.svg)](https://stand-with-ukraine.pp.ua)

![image](https://github.com/user-attachments/assets/8f595879-a578-421a-a06d-8c194af874f7)

| Unity Version | Editmode | Playmode | Standalone |
|---------------|----------|----------|------------|
| 2022.3.61f1   | ![2022.3.61f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/2022.3.61f1_editmode.yml?label=2022.3.61f1-editmode) | ![2022.3.61f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/2022.3.61f1_playmode.yml?label=2022.3.61f1-playmode) | ![2022.3.61f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/2022.3.61f1_standalone.yml?label=2022.3.61f1-standalone) |
| 2023.2.20f1   | ![2023.2.20f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/2023.2.20f1_editmode.yml?label=2023.2.20f1-editmode) | ![2023.2.20f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/2023.2.20f1_playmode.yml?label=2023.2.20f1-playmode) | ![2023.2.20f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/2023.2.20f1_standalone.yml?label=2023.2.20f1-standalone) |
| 6000.0.46f1   | ![6000.0.46f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/6000.0.46f1_editmode.yml?label=6000.0.46f1-editmode) | ![6000.0.46f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/6000.0.46f1_playmode.yml?label=6000.0.46f1-playmode) | ![6000.0.46f1](https://img.shields.io/github/actions/workflow/status/IvanMurzak/Unity-MCP/6000.0.46f1_standalone.yml?label=6000.0.46f1-standalone) |

**[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)** is a bridge between LLM and Unity. It exposes and explains to LLM Unity's tools. LLM understands the interface and utilizes the tools in the way a user asks.

Connect **[Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)** to LLM client such as [Claude](https://claude.ai/download) or [Cursor](https://www.cursor.com/) using integrated `AI Connector` window. Custom clients are supported as well.

The project is designed to let developers to add custom tools soon. After that the next goal is to enable the same features in player's build. For not it works only in Unity Editor.

The system is extensible: you can define custom `tool`s directly in your Unity project codebase, exposing new capabilities to the AI or automation clients. This makes Unity-MCP a flexible foundation for building advanced workflows, rapid prototyping, or integrating AI-driven features into your development process.

## AI Tools

<table>
<tr>
<td valign="top">

### GameObject

- ✅ Create
- ✅ Destroy
- ✅ Find
- ✅ Modify (tag, layer, name, static)
- ✅ Set parent
- ✅ Duplicate

##### GameObject.Components

- ✅ Add Component
- ✅ Get Components
- ✅ Modify Component
- - ✅ `Field` set value
- - ✅ `Property` set value
- - ✅ `Reference` link set
- ✅ Destroy Component
- 🔲 Remove missing components

### Editor

- ✅ State (Playmode)
  - ✅ Get
  - ✅ Set
- ✅ Get Windows
- ✅ Layer
  - ✅ Get All
  - ✅ Add
  - ✅ Remove
- ✅ Tag
  - ✅ Get All
  - ✅ Add
  - ✅ Remove
- ✅ Execute `MenuItem`
- ✅ Run Tests (see note in section "Running PlayMode tests")

#### Editor.Selection

- ✅ Get selection
- ✅ Set selection

### Prefabs

- ✅ Instantiate
- 🔲 Create
- ✅ Open
- ✅ Modify (GameObject.Modify)
- ✅ Save
- ✅ Close

### Package

- 🔲 Get installed
- 🔲 Install
- 🔲 Remove
- 🔲 Update

</td>
<td valign="top">

### Assets

- ✅ Create
- ✅ Find
- ✅ Refresh
- ✅ Read
- ✅ Modify
- ✅ Rename
- ✅ Delete
- ✅ Move
- ✅ Create folder

### Scene

- ✅ Create
- ✅ Save
- ✅ Load
- ✅ Unload
- ✅ Get Loaded
- ✅ Get hierarchy
- 🔲 Search (editor)
- 🔲 Raycast (understand volume)

### Materials

- ✅ Create
- ✅ Modify (Assets.Modify)
- ✅ Read (Assets.Read)
- ✅ Assign to a Component on a GameObject

### Shader

- ✅ List All

### Scripts

- ✅ Read
- ✅ Update or Create
- ✅ Delete

### Scriptable Object

- 🔲 Create
- 🔲 Read
- 🔲 Modify
- 🔲 Remove

### Debug

- 🔲 Read logs (console)

### Component

- ✅ Get All

</td>
</tr>
</table>

> **Legend:**
> ✅ = Implemented & available, 🔲 = Planned / Not yet implemented

#### Running PlayMode tests

To be able to run Play Mode tests via the TestRunner MCP tool, you should consider configuring Unity to **not** perform a domain reload when entering Play Mode (`Edit -> Project Settings -> Editor -> Enter Play Mode Settings` - set to `Reload Scene only` or `Do not reload Domain or Scene`). Otherwise, starting the Play Mode tests will interrupt the TestRunner MCP tool, leading to a cycle of tests restarting.

# Installation

1. [Install .NET 9.0](https://dotnet.microsoft.com/en-us/download)
2. [Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)

- Open command line in Unity project folder
- Run the command

```bash
openupm add com.ivanmurzak.unity.mcp
```

# Usage

1. Make sure your project path doesn't have a space symbol " ".
> - ✅ `C:/MyProjects/Project`
> - ❌ `C:/My Projects/Project`

2. Open Unity project, go 👉 `Window/AI Connector (Unity-MCP)`.

![Unity_WaSRb5FIAR](https://github.com/user-attachments/assets/e8049620-6614-45f1-92d7-cc5d00a6b074)

3. Install MCP client
> - [Install Cursor](https://www.cursor.com/) (recommended)
> - [Install Claude](https://claude.ai/download)

4. Sign-in into MCP client
5. Click `Configure` at your MCP client.

![image](https://github.com/user-attachments/assets/19f80179-c5b3-4e9c-bdf6-07edfb773018)

6. Restart your MCP client.
7. Make sure `AI Connector` is "Connected" or "Connecting..." after restart.
8. Test AI connection in your Client (Cursor, Claude Desktop). Type any question or task into the chat. Something like:

  ```text
  Explain my scene hierarchy
  ```

# Add custom `tool`

> ⚠️ It only works with MCP client that supports dynamic tool list update.

Unity-MCP is designed to support custom `tool` development by project owner. MCP server takes data from Unity plugin and exposes it to a Client. So anyone in the MCP communication chain would receive the information about a new `tool`. Which LLM may decide to call at some point.

To add a custom `tool` you need:

1. To have a class with attribute `McpPluginToolType`.
2. To have a method in the class with attribute `McpPluginTool`.
3. [optional] Add `Description` attribute to each method argument to let LLM to understand it.
4. [optional] Use `string? optional = null` properties with `?` and default value to mark them as `optional` for LLM.

> Take a look that the line `MainThread.Instance.Run(() =>` it allows to run the code in Main thread which is needed to interact with Unity API. If you don't need it and running the tool in background thread is fine for the tool, don't use Main thread for efficiency purpose.

```csharp
[McpPluginToolType]
public class Tool_GameObject
{
    [McpPluginTool
    (
        "MyCustomTask",
        Title = "Create a new GameObject"
    )]
    [Description("Explain here to LLM what is this, when it should be called.")]
    public string CustomTask
    (
        [Description("Explain to LLM what is this.")]
        string inputData
    )
    {
        // do anything in background thread

        return MainThread.Instance.Run(() =>
        {
            // do something in main thread if needed

            return $"[Success] Operation completed.";
        });
    }
}
```

# Add custom in-game `tool`

> ⚠️ Not yet supported. The work is in progress


# Contribution

Feel free to add a new `tool` into the project.

1. Fork the project.
2. Clone the fork and open the resulting folder in Unity.
3. Implement the new `tool` in your forked repository.
4. Create Pull Request into original [Unity-MCP](https://github.com/IvanMurzak/Unity-MCP) repository.
