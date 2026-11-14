[English](commands.md) | <b>中文</b> | [日本語](commands.ja.md) | [Español](commands.es.md) | [فارسی](commands.fa.md) | [हिन्दी](commands.hi.md) | [العربية](commands.ar.md)

# unity-mcp-cli 参考

[`unity-mcp-cli`](../README.md) 的所有命令和选项、退出码、机器可读输出以及库 API。
运行 `unity-mcp-cli <command> --help` 可在终端中查看相同的详细信息。

**命令：** [`configure`](#configure) · [`create-project`](#create-project) · [`update`](#update) · [`install-extension`](#install-extension) · [`install-plugin`](#install-plugin) · [`install-unity`](#install-unity) · [`login`](#login) · [`unity-account`](#unity-account) · [`open`](#open) · [`close`](#close) · [`tools`](#tools) · [`rql`](#rql) · [`run-tool`](#run-tool) · [`exec`](#exec) · [`test`](#test) · [`compile`](#compile) · [`dialog`](#dialog) · [`commands`](#commands) · [`wait-for-ready`](#wait-for-ready) · [`setup-mcp`](#setup-mcp) · [`setup-skills`](#setup-skills) · [`remove-plugin`](#remove-plugin) · [`status`](#status)

**另见：** [全局选项](#全局选项) · [退出码与机器可读输出](#退出码与机器可读输出) · [完整自动化示例](#完整自动化示例) · [工作原理](#工作原理) · [库 API](#库-api-v0670)

# 命令

## `configure`

在 `UserSettings/AI-Game-Developer-Config.json` 中配置 MCP 工具、提示词和资源。

```bash
unity-mcp-cli configure ./MyGame --list
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | Unity 项目路径（位置参数或 `--path`） |
| `--list` | 否 | 列出当前配置并退出 |
| `--enable-tools <names>` | 否 | 向 MCP 客户端显示指定的工具（逗号分隔） |
| `--disable-tools <names>` | 否 | 对 MCP 客户端隐藏指定的工具（逗号分隔；隐藏的工具按名称调用时仍会运行） |
| `--enable-all-tools` | 否 | 显示所有工具 |
| `--disable-all-tools` | 否 | 隐藏所有工具 |
| `--enable-prompts <names>` | 否 | 向 MCP 客户端显示指定的提示词（逗号分隔） |
| `--disable-prompts <names>` | 否 | 对 MCP 客户端隐藏指定的提示词（逗号分隔；隐藏的工具按名称调用时仍会运行） |
| `--enable-all-prompts` | 否 | 显示所有提示词 |
| `--disable-all-prompts` | 否 | 隐藏所有提示词 |
| `--enable-resources <names>` | 否 | 向 MCP 客户端显示指定的资源（逗号分隔） |
| `--disable-resources <names>` | 否 | 对 MCP 客户端隐藏指定的资源（逗号分隔；隐藏的工具按名称调用时仍会运行） |
| `--enable-all-resources` | 否 | 显示所有资源 |
| `--disable-all-resources` | 否 | 隐藏所有资源 |

**示例 — 启用指定工具并禁用所有提示词：**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-tools screenshot-scene-view,script-read \
  --disable-all-prompts
```

**示例 — 全部启用：**

```bash
unity-mcp-cli configure ./MyGame \
  --enable-all-tools \
  --enable-all-prompts \
  --enable-all-resources
```

## `create-project`

使用 Unity 编辑器创建一个新的 Unity 项目。

```bash
unity-mcp-cli create-project /path/to/new/project
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | 项目的创建路径（位置参数或 `--path`） |
| `--unity <version>` | 否 | 要使用的 Unity 编辑器版本（默认为已安装的最高版本） |

**示例 — 使用指定的编辑器版本创建项目：**

```bash
unity-mcp-cli create-project ./MyGame --unity 2022.3.62f1
```

## `update`

更新所有内容：先将 Unity 项目的 MCP 插件和已安装的扩展更新到最新版本，再更新 CLI 本身。

```bash
unity-mcp-cli update              # the project around the current folder, then the CLI
unity-mcp-cli update ./MyGame      # a project by path
unity-mcp-cli update --check       # only report; exit code 2 when something is outdated
unity-mcp-cli update --no-self     # leave the CLI as it is
unity-mcp-cli update --no-project  # update only the CLI
unity-mcp-cli update --no-extensions
```

在项目之外运行时只更新 CLI。通过 `file:` 或 git 安装的包保持不变，并且不会降级任何内容。

## `install-extension`

将 Unity-MCP 扩展之一——为 Animation、Cinemachine、Input System、Navigation、Particle System、ProBuilder、Splines、Terrain、Tilemap 和 Timeline 添加 MCP 工具的可选工具包——安装到 Unity 项目的 `Packages/manifest.json` 中。这是 *AI Game Developer* 编辑器窗口中 **Extensions** 部分的命令行等价操作，并且从同一份共享目录中安装。

```bash
# See what is installable (no project and no network required)
unity-mcp-cli install-extension --list

# Run from inside the Unity project folder — the path is optional
cd ./MyGame && unity-mcp-cli install-extension Tilemap
```

| 参数 / 选项 | 必需 | 描述 |
|---|---|---|
| `[id]` | 是（除非使用 `--list`） | 要安装的扩展——OpenUPM 包 id（`com.ivanmurzak.unity.mcp.tilemap`）或目录名称（`Tilemap`）。匹配不区分大小写。未知的 id 会列出所有可安装的扩展，方便你自行纠正。 |
| `[path]` | 否 | Unity 项目路径（位置参数或 `--path`）。默认为当前目录，校验方式与 `install-plugin` 相同。 |
| `--extension-version <version>` | 否 | 要安装的扩展版本（默认为 OpenUPM 上的最新版本）。**不是 `--version`**——那是 CLI 自身的全局版本标志。 |
| `--list` | 否 | 列出可安装的扩展并退出。 |

此命令会：
1. 添加带有所有必需作用域的 **OpenUPM 作用域注册表**（正是 `com.ivanmurzak` 作用域让扩展能够被解析）
2. 将扩展的包 id 添加到 `dependencies`，在安装时解析 OpenUPM 的 `latest`
3. 对自动解析的版本**从不降级**——如果已安装了更高的版本，则保留该版本并打印警告。传入 `--extension-version` 可强制使用指定的值，包括降级。
4. 是**幂等的**——重复运行时若发现扩展已是最新版本，会如实报告且不写入任何内容

**示例 — 安装指定版本的扩展：**

```bash
unity-mcp-cli install-extension Cinemachine ./MyGame --extension-version 1.0.17
```

**示例 — 在进程内通过库调用：**

```ts
import { installExtension, EXTENSIONS_CATALOG } from 'unity-mcp-cli';

const result = await installExtension({
  unityProjectPath: './MyGame',
  extensionId: 'Tilemap',
});
if (result.kind === 'success') {
  console.log(result.outcome, result.fromVersion, '->', result.toVersion);
}
```

> 运行此命令后，请打开（或切换到）Unity 编辑器，以便 Package Manager 解析新的依赖。

## `install-plugin`

将 Unity-MCP 插件安装到 Unity 项目的 `Packages/manifest.json` 中。还可以在同一次运行中下载与 RID 匹配的 MCP 服务器二进制文件，并兑换团队注册码。

```bash
# Run from inside the Unity project folder — the path is optional
cd ./MyGame && unity-mcp-cli install-plugin
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 否 | Unity 项目路径（位置参数或 `--path`）。默认为当前目录；解析出的目录会被校验是否为真正的 Unity 项目（以 `Packages/manifest.json` 为标志），校验失败时，错误信息会准确列出检查过的内容。 |
| `--plugin-version <version>` | 否 | 要安装的插件版本（默认为 [OpenUPM](https://openupm.com/packages/com.ivanmurzak.unity.mcp/) 上的最新版本） |
| `--with-server` | 否 | 同时将与 RID 匹配的 [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server) 二进制文件下载到 CLI 管理的目录中 |
| `--server-version <version>` | 否 | 配合 `--with-server` 下载的服务器版本（默认：CLI 固定的服务器版本） |
| `--server-source <path-or-url>` | 否 | 离线/CI 覆盖——从本地 zip 路径或 URL 安装服务器（跳过校验和验证） |
| `--enroll <code>` | 否 | 用注册码兑换插件凭据（存入共享的本机存储并固定到此项目） |
| `--enroll-stdin` | 否 | 从 stdin 而不是 argv 读取注册码（避免其出现在 shell 历史记录中） |

此命令会：
1. 添加带有所有必需作用域的 **OpenUPM 作用域注册表**
2. 将 `com.ivanmurzak.unity.mcp` 添加到 `dependencies`
3. **从不降级**——如果已安装了更高的版本，则保留该版本

**示例 — 安装指定版本的插件：**

```bash
unity-mcp-cli install-plugin ./MyGame --plugin-version 0.51.6
```

**示例 — 兑换团队注册码（路径默认为当前工作目录）：**

```bash
cd ./MyGame && unity-mcp-cli install-plugin --enroll <code>
```

> 运行此命令后，请在 Unity 编辑器中打开项目以完成包的安装。

## `install-unity`

通过 Unity Hub CLI 安装某个 Unity 编辑器版本。

```bash
unity-mcp-cli install-unity 6000.3.1f1
```

| 参数 / 选项 | 必需 | 描述 |
|---|---|---|
| `[version]` | 否 | 要安装的 Unity 编辑器版本（例如 `6000.3.1f1`） |
| `--path <path>` | 否 | 从现有项目中读取所需的版本 |

如果既未提供参数也未提供选项，该命令会安装 Unity Hub 发布列表中最新的稳定版本。

**示例 — 安装项目所需的编辑器版本：**

```bash
unity-mcp-cli install-unity --path ./MyGame
```

## `login`

登录 **ai-game.dev**，并将凭据保存在共享的本机凭据存储中（`~/.ai-game-dev/credentials.json`）。运行基于浏览器的 **OAuth 2.1 设备授权流程**——无需创建或粘贴个人访问令牌。登录会为整台机器授权：它会签发一个**智能体**凭据，并由此派生出一个引擎工具凭据；所有使用方（CLI 的云端命令、Unity 编辑器插件、桌面应用）都会自动刷新它——令牌在后台轮换，因此每台机器只需登录一次，而不是每个会话登录一次。

```bash
unity-mcp-cli login
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `--project <path>` | 否 | 将凭据保存在项目本地存储（`<path>/.ai-game-dev/`）中，而不是共享的本机存储 |
| `--force` | 否 | 即使凭据已存在也重新进行身份验证 |
| `--tools-only` | 否 | **仅授权引擎工具**（不保存智能体凭据，因此桌面应用无法接管）。适用于 CI / 自动化运行器，它们随后会显示为各自独立、可撤销的设备组 |
| `--yes` | 否 | 对提示默认回答"是"——以非交互方式确认**切换账户**时必需（以不同于本机当前持有的账户登录） |

该命令会打印一个简短的用户代码和一个验证 URL，打开你的浏览器，并轮询直到你批准登录。如果凭据已存在，它会提前退出并显示 *"Already signed in"*——传入 `--force` 可替换它。以与本机当前持有的账户**不同的账户**登录时，会先请求确认（拒绝则一切保持不变）；在脚本中传入 `--yes` 进行确认。

**示例 — 登录（共享的本机存储）：**

```bash
unity-mcp-cli login
```

**示例 — CI 运行器，仅引擎工具：**

```bash
unity-mcp-cli login --tools-only
```

**示例 — 将凭据保存在指定项目旁边：**

```bash
unity-mcp-cli login --project ./MyGame
```

## `unity-account`

显示正在运行的 Unity 编辑器所登录的 **Unity 账户**，并启动其登录。Asset Store 的
"My Assets"（`assetStore` RQL 数据源）需要它；Personal 许可证无需登录即可使用，因此编辑器可以在
未登录状态下运行。这不是 ai-game.dev 账户：那个是 [`login`](#login)。

`--sign-in` 会在该编辑器中打开 Unity 的登录界面；由**用户**在那里通过 Unity Hub 完成登录。直接打开的编辑器
（不是通过 Hub 打开的，例如由旧版 `open` 打开的）无法登录：它只提供"Install Unity Hub"，因此
回答中会建议用 [`close`](#close) 和 [`open`](#open) 重新打开项目，它们会通过 Hub 启动编辑器。以这种方式打开的编辑器
会直接使用 Hub 已登录的账户，无需任何登录操作。凭据不会经过 CLI。该命令需要项目已在带窗口的编辑器中打开
（[`open`](#open)），并且从不启动无界面编辑器，因为后者无法显示登录界面。智能体可以通过
`rql` 执行相同的查询：`assetStore | select signedIn userName` 和 `assetStore | call SignIn()`。

```bash
unity-mcp-cli unity-account ./MyGame            # signedIn|userName
unity-mcp-cli unity-account ./MyGame --sign-in  # opens Unity's sign-in for the user
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 否 | Unity 项目路径（位置参数或 `--path`；默认：当前目录） |
| `--sign-in` | 否 | 当编辑器未登录时打开 Unity 的登录界面（已登录时：刷新访问令牌） |
| `--format <format>` | 否 | `text`、`json` 或 `ndjson` |

收到编辑器的回答时退出码为 `0`，没有编辑器响应时为 `7`。对于过旧、不支持此命令的插件，会提示运行
[`update`](#update)。

## `open`

在 Unity 编辑器中打开 Unity 项目。默认情况下，如果提供了连接选项，会设置 MCP 连接环境变量。使用 `--no-connect` 可在不建立 MCP 连接的情况下打开。

安装了 Unity Hub 时，编辑器会像 Hub 自己那样通过 Hub 打开（`-useHub -hubIPC`，不传凭据：
编辑器向 Hub 请求其会话，Hub 未运行时会在后台启动它）。随后编辑器
使用 Hub 已登录的 Unity 账户，Asset Store 的 "My Assets" 需要这个账户（参见
[`unity-account`](#unity-account)）。无界面（`--headless`）编辑器的打开方式与以前相同。

```bash
# Explicit path
unity-mcp-cli open ./MyGame

# From inside the Unity project folder — path defaults to the current directory
cd ./MyGame && unity-mcp-cli open
```

| 选项 | 环境变量 | 必需 | 描述 |
|---|---|---|---|
| `[path]` | — | 否 | Unity 项目路径（位置参数或 `--path`）。默认为当前工作目录。 |
| `--unity <version>` | — | 否 | 要使用的特定 Unity 编辑器版本（默认使用项目设置中的版本，回退为已安装的最高版本） |
| `--editor-path <path>` | — | 否 | Unity 编辑器可执行文件的显式路径。跳过 Unity Hub 查找，适用于自定义安装位置。 |
| `--no-connect` | — | 否 | 不设置 MCP 连接环境变量直接打开 |
| `--url <url>` | `UNITY_MCP_CLOUD_URL` | 否 | 要连接的 MCP 服务器 URL（CLI 仍会导出旧版别名 `UNITY_MCP_HOST`，插件同样接受它） |
| `--keep-connected` | `UNITY_MCP_KEEP_CONNECTED` | 否 | 强制保持连接 |
| `--token <token>` | `UNITY_MCP_TOKEN` | 否 | 身份验证令牌 |
| `--auth <option>` | `UNITY_MCP_AUTH_OPTION` | 否 | 身份验证模式：`none`、`oauth` 或 `token` |
| `--tools <names>` | `UNITY_MCP_TOOLS` | 否 | 要启用的工具列表（逗号分隔） |
| `--transport <method>` | `UNITY_MCP_TRANSPORT` | 否 | 传输方式：`streamableHttp` 或 `stdio` |
| `--start-server <value>` | `UNITY_MCP_START_SERVER` | 否 | 设为 `true` 或 `false` 以控制 MCP 服务器是否自动启动 |
| `--headless` | — | 否 | 以无窗口方式启动编辑器（`-batchmode`；图形保持开启，因此摄像机仍可渲染），并等待其 MCP 服务器响应。隐含 `--keep-connected`。如果该项目已有运行中的编辑器则复用它；在此启动的编辑器归 CLI 所有（参见 [`close --owned-only`](#close)），并行调用只会启动一个。 |
| `--idle-timeout <seconds>` | `AIGD_SESSION_IDLE_SECONDS`（无界面）/ `UNITY_MCP_IDLE_TIMEOUT`（有窗口） | 否 | 在指定秒数内没有工具调用时退出编辑器（会先保存已更改的场景和资源） |
| `--ready-timeout <ms>` | — | 否 | 配合 `--headless`：等待编辑器就绪的时长（默认：`300000`） |
| `--no-auto-dismiss-launch-errors` | — | 否 | 禁用自动关闭 Unity 编辑器"启动时存在编译错误"对话框（默认：启用） |
| `--launch-dismiss-timeout-ms <ms>` | — | 否 | 启动错误自动关闭轮询循环的总超时时间（毫秒）（默认：`30000`） |
| `--launch-dismiss-poll-interval-ms <ms>` | — | 否 | 启动错误自动关闭循环的轮询间隔（毫秒）（默认：`1500`） |

不使用 `--headless` 时，如果 CLI 已为该项目启动了无界面编辑器（`exec`、`open --headless`），会先请求它保存并退出，以便能打开带窗口的编辑器。

编辑器进程以分离模式启动。默认情况下，启动编辑器后，`open` 会轮询 Unity 的"启动时存在编译错误"对话框（Unity 2020.2+ 上为 `"Enter Safe Mode?"`，较旧版本上为 `"Hold On" / "Compiler Errors"`）并点击 `Ignore`，让编辑器完成初始化——否则，任何需要在 Unity 自身无法编译的状态之后运行的编辑器内自动化（例如 NuGet 依赖解析器）都无法自我修复。该对话框会在 Unity 启动完成、连接到 Package Manager 并开始编译之后出现——根据经验，在快速机器上约 6 秒，在慢速机器上更久——因此轮询循环有一个宽限窗口，如果在此期间没有看到对话框，循环会提前退出。宽限窗口必须覆盖 Unity 的整个启动阶段，否则循环会在对话框出现之前就退出（issue #737）；在没有对话框的情况下，它从不会运行满 `--launch-dismiss-timeout-ms`。如果观察到对话框（并成功关闭），轮询会持续到总超时，以便再次出现的对话框（解析器修复一个错误 → 对话框随下一个错误再次出现）也能被关闭。库模式调用方可以提供一个 `AbortSignal`（`OpenProjectOptions` 上的 `launchDismissAbortSignal`），在自己的就绪信号触发时立即中止循环。

### 自动关闭对话框的平台要求

| 平台 | 要求 | 说明 |
|---|---|---|
| **Windows** | 内置（Win32 API） | 通过 PowerShell 调用 `EnumWindows` / `EnumChildWindows` / `SendMessageW(BM_CLICK)`。无需额外设置。 |
| **macOS** | 必须为终端（或 `unity-mcp-cli` 二进制文件）授予**辅助功能权限**。系统设置 → 隐私与安全性 → 辅助功能。 | 通过 AppleScript / `osascript` 实现。没有此权限时，`osascript` 在每次轮询时都会报错，对话框也无法被关闭。 |
| **Linux/X11** | `PATH` 上需要有 `xdotool`（例如 `sudo apt-get install xdotool`）。 | 首个版本**不**支持 Wayland——请关注上游 issue 以了解 Wayland 支持进展。 |

要完全禁用此功能，请传入 `--no-auto-dismiss-launch-errors`。

**示例 — 打开并建立 MCP 连接：**

```bash
unity-mcp-cli open ./MyGame \
  --url http://localhost:8080 \
  --keep-connected
```

**示例 — 不建立 MCP 连接直接打开（简单打开）：**

```bash
unity-mcp-cli open ./MyGame --no-connect
```

**示例 — 使用身份验证和指定工具打开：**

```bash
unity-mcp-cli open ./MyGame \
  --url http://my-server:8080 \
  --token my-secret-token \
  --auth token \
  --tools rql-query,rql-mutate,rql-schema
```

## `close`

优雅地终止为指定项目路径运行的 Unity 编辑器实例。它是 [`open`](#open) 的对称操作——适用于需要干净地收尾、又不想诉诸操作系统级强制结束进程的脚本化工作流（CI 智能体、流水线执行器、集成测试夹具）。

```bash
unity-mcp-cli close ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 否 | Unity 项目路径（位置参数，默认为当前目录） |
| `--timeout <seconds>` | 否 | 礼貌退出的超时时间，单位为秒（默认：30） |
| `--force` | 否 | 如果编辑器在 `--timeout` 内未退出，则强制结束它 |
| `--owned-only` | 否 | 只关闭由 CLI 启动的编辑器（`exec`、`open --headless`）；用户打开的编辑器绝不会被触碰 |

**工作原理：**

0. CLI 启动的编辑器会被记录在项目的会话租约中（`.ai-game-dev/session.json`：PID、进程启动时间、nonce）。如果租约指向一个存活的编辑器，`close` 会请求其插件保存并退出（通过 `editor-session-quit` 系统工具，以租约 nonce 校验，发往编辑器启动时租约所记录的服务器 URL）——这也能触达 `WM_CLOSE` 无法触达的无界面编辑器——并跳过以下步骤。使用 `--owned-only` 时到此为止：没有租约就不关闭任何东西。如果在 `--timeout` 之后（未使用 `--force`）该编辑器仍在运行且有模态对话框打开，`close` 会以退出码 18 退出并给出对话框名称，与步骤 3 相同。
1. 通过读取 `<project>/Temp/UnityLockfile`（4 字节小端 uint32）解析正在运行的编辑器的 PID，并与进程枚举结果交叉核对，以处理过期的锁文件。
2. 发送礼貌退出信号——Linux/macOS 上为 `SIGTERM`，Windows 上为 `taskkill`（不带 `/F`）——让 Unity 完成自动保存 / 资源导入。
3. 每 250ms 轮询一次，直到进程退出或 `--timeout` 到期。在此期间，它会查找编辑器的模态对话框（Windows、macOS、Linux）：在有未保存更改时礼貌退出会打开 "Scene(s) Have Been Modified" [Save | Don't Save | Cancel]，随后编辑器会等待人工处理。未使用 `--force` 时，一旦连续两次扫描都看到该对话框（大约在退出请求后 3 秒），`close` 就会以退出码 18 停止，并给出对话框及其按钮的名称（在编辑器中或用 [`dialog --press`](#dialog) 回答它）；使用 `--force` 时，它仍会等满 `--timeout` 再强制结束。
4. 如果超时到期且设置了 `--force`，则回退为 `SIGKILL` / `taskkill /F`。
5. 幂等——关闭一个已关闭的编辑器（或其编辑器从未运行过的项目）会以 0 退出，并显示 `no running Editor for project at <path>`。
6. 拒绝对任何不是 Unity 项目根目录的路径执行操作（`ProjectSettings/ProjectVersion.txt` 必须存在）——防止意外结束主机上所有 Unity 进程。

> **Windows 无界面注意事项：** 礼貌退出步骤使用 `taskkill`（不带 `/F`），它发送的是 `WM_CLOSE`。该消息只能送达在与 CLI **相同的桌面/会话**中拥有顶层窗口的进程。如果 Unity 是由会话 0 中的 Windows 服务（或任何其他非交互式桌面）启动的，礼貌退出会被静默丢弃，`--timeout` 会到期，此时 `--force` 成为关闭编辑器的唯一途径。请在无界面 CI 运行器中相应地做好规划。由 CLI 启动的编辑器（步骤 0）不受影响：它通过其插件退出。

**示例 — 关闭，60 秒后回退为强制结束：**

```bash
unity-mcp-cli close ./MyGame --timeout 60 --force
```

**示例 — 在自动化脚本末尾干净地收尾：**

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
unity-mcp-cli close ./MyGame
```

## `tools`

列出项目的 Unity 编辑器所提供的 AI 工具——场景、资源、测试、截图以及已安装的扩展所添加的一切——
包括每个工具的作用和输入，或详细显示单个工具（完整描述、输入、调用示例、
其 JSON 输入 schema）。没有编辑器运行时会启动一个无界面编辑器，与 [`exec`](#exec) 相同。列表通过 MCP 读取
（`tools/list`）并保存在项目中（已被 git 忽略），因此在编辑器关闭时 `--cached` 也能立即给出结果。
隐藏的工具（不提供给 MCP 客户端的工具）会按名称列出；`run-tool` 仍可调用它们。

```bash
unity-mcp-cli tools                 # every tool, one line each, with its inputs
unity-mcp-cli tools rql-mutate      # one tool: description, inputs, example call, schema
unity-mcp-cli tools screenshot      # a regex filter over names and descriptions
unity-mcp-cli tools --format json   # full schemas, machine-readable
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[name]` | 否 | 工具名称（显示详情）或正则表达式（过滤） |
| `--path <path>` | 否 | Unity 项目路径（默认：当前目录） |
| `--cached` | 否 | 不联系编辑器，直接根据已保存的列表回答 |
| `--no-start` | 否 | 从不启动编辑器；没有编辑器响应时使用已保存的列表 |
| `--once` | 否 | 如果编辑器是本次调用启动的，则在之后关闭它 |
| `--ready-timeout <ms>` | 否 | 等待已启动的编辑器（默认：300000） |
| `--url <url>` / `--token <token>` | 否 | 服务器覆盖 |
| `--format <format>` | 否 | `text`、`json` 或 `ndjson` |

## `rql`

在命令行中执行 RQL，无需处理 JSON 引号：一条查询，或用 `;` 分隔的多条查询。最后一个阶段为 `set` 或 `call` 的查询会发往
`rql-mutate`（多条查询时：只要其中任意一条如此，就必须每一条都如此；每条都是独立的全有或全无的更改，第一条失败的查询会终止其余查询），其他查询发往 `rql-query`；`--schema [type]` 会运行 `rql-schema`。会像 `run-tool` 那样启动无界面编辑器。
Unity 会立即将更改保存到磁盘，因此在执行 `set` / `call` 之后，CLI 会打印它写入的场景和资源文件
（`Saved to disk: Assets/Scenes/Forest.unity`）。按其他语言的拼写方式写出的运算符（`startsWith`、`like`）会收到
提示，告知 RQL 中的写法。`rql --help` 会列出常用示例。

```bash
unity-mcp-cli rql 'objects | where name starts-with "Tree" | count'
unity-mcp-cli rql 'objects | where name starts-with "Rock" | set transform.localScale = transform.localScale * 1.5'
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[query]` | 是，除非使用 `--schema` / `-f` | RQL 查询（用单引号括起来；其中的字符串使用双引号） |
| `-f, --file <file>` | 否 | 从文件读取查询（`-`：stdin），适用于会去掉内部引号的 shell |
| `--schema [type]` | 否 | 列出 RQL 数据源，或某个类型的成员 |
| `--dry-run` | 否 | 用于 `set` / `call`：显示将会更改的内容，但不做任何更改 |
| `--max-targets <n>` | 否 | 用于 `set` / `call`：允许超过 200 个目标（最多 1000 个） |
| `--path <path>` | 否 | Unity 项目路径（默认：当前目录） |
| `--no-start` / `--once` | 否 | 与 `run-tool` 相同（`--url`、`--token`、`--raw`、`--timeout`、`--ready-timeout` 也可使用） |
| `--format <format>` | 否 | `text`、`json` 或 `ndjson` |

## `run-tool`

通过 HTTP API 直接执行 MCP 工具。服务器 URL 和授权令牌会根据当前的连接模式（Custom 或 Cloud），从项目的配置文件（`UserSettings/AI-Game-Developer-Config.json`）中**自动解析**。

```bash
unity-mcp-cli run-tool rql-mutate ./MyGame --input '{"query":"editor | call CreateGameObject({name: \"Cube\", primitive: \"Cube\"})"}'
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `<tool-name>` | 是 | 要执行的 MCP 工具名称 |
| `[path]` | 否 | Unity 项目路径（位置参数或 `--path`）——用于读取配置和检测端口 |
| `--url <url>` | 否 | 直接覆盖服务器 URL（绕过配置） |
| `--token <token>` | 否 | 覆盖 bearer 令牌（绕过配置） |
| `--input <json>` | 否 | 工具参数的 JSON 字符串（默认为 `{}`） |
| `--input-file <file>` | 否 | 从文件读取 JSON 参数 |
| `--raw` | 否 | 输出原始 JSON（无格式化、无加载动画） |
| `--format <format>` | 否 | `text`（默认）、`json` 或 `ndjson`——参见[机器可读输出](#退出码与机器可读输出) |
| `--timeout <ms>` | 否 | 请求超时时间，单位为毫秒（默认：60000） |
| `--no-start` | 否 | 从不启动编辑器；没有编辑器响应时失败（退出码 7） |
| `--once` | 否 | 如果编辑器是本次调用启动的，则在调用后关闭它（一次性 CI 用法） |
| `--ready-timeout <ms>` | 否 | 等待已启动的编辑器就绪的时长（默认：300000） |

**启动编辑器。** 当项目没有编辑器响应时，`run-tool` 会完全像
[`exec`](#exec) 那样启动一个无界面编辑器（共享会话租约，从不关闭不是它启动的编辑器）——但仅限于在
`ProjectSettings/ProjectVersion.txt` 中写明了 Unity 版本的项目，因此它从不猜测使用哪个编辑器。`--no-start` 关闭此行为；
`--once` 使调用变为一次性：本次调用启动的编辑器会在调用后再次关闭（编辑器始终未能
就绪时也会关闭）。`run-system-tool` 的行为相同。

工具返回错误时退出码为 6；未知工具为 2（`NOT_FOUND`），超时为 14，无法访问编辑器为 7，编辑器
忙碌为 17，调用被模态对话框阻塞为 18——包含 HTTP 状态码的完整表格见
[退出码](#退出码与机器可读输出)。

**URL 解析优先级：**
1. `--url` → 直接使用
2. 配置文件 → `host`（Custom 模式）或硬编码的云端端点（Cloud 模式）
3. 根据项目路径确定的端口

**授权**信息会自动从项目配置中读取（Custom 模式下为 `token`，Cloud 模式下为 `cloudToken`）。使用 `--token` 可显式覆盖从配置中得到的令牌。

**示例 — 调用工具（URL 和授权来自配置）：**

```bash
unity-mcp-cli run-tool rql-query ./MyGame --input '{"query":"objects | where name == \"Player\" | select path name active"}'
```

**示例 — 显式覆盖 URL：**

```bash
unity-mcp-cli run-tool rql-mutate --url http://localhost:8080 --input '{"query":"editor | call SaveScenes()"}'
```

**示例 — 通过管道传递原始 JSON 输出：**

```bash
unity-mcp-cli run-tool unity-tool-list ./MyGame --raw | jq .
```

**示例 — CI 中的一次性调用（启动无界面编辑器，之后关闭它）：**

```bash
unity-mcp-cli run-tool package-list ./MyGame --url http://localhost:28343 --once --format json
```

## `exec`

在 Unity 编辑器中运行 C# 并打印结果。如果项目已经打开（带窗口或无界面），`exec` 会使用该编辑器且从不关闭它。否则，它会启动一个无界面编辑器，该编辑器保持预热以供后续调用使用，并在 `--idle` 秒内没有工具调用时自行退出（会保存已更改的场景和资源）。它启动的编辑器会被记录在项目的会话租约中（`.ai-game-dev/session.json`），并在文件锁下启动，因此并行的 `exec` 调用共享同一个编辑器；`close --owned-only` 会关闭它，`open`（带窗口）会在打开窗口之前关闭它。这些会话文件仅属于本机：`.ai-game-dev/.gitignore` 会将它们排除在 git 之外。如果项目在一个不是由 CLI 启动的编辑器中打开，并且其插件在 60 秒内没有响应（未连接，或连接到了 ai-game.dev 云端），`exec` 会停止并给出提示，而不是一直等待：请连接插件、将其连接切换为 Custom（本地服务器），或传入 `--url`。

```bash
# The last expression is the result
unity-mcp-cli exec 'GameObject.FindObjectsOfType<Camera>().Length'

# From a file or stdin, with extra namespaces and helper files
unity-mcp-cli exec -f ./snippet.cs --using MyGame.Gameplay --include Helpers/
echo 'Application.unityVersion' | unity-mcp-cli exec
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[code]` | 否 | C# 代码片段：普通语句，最后一个表达式即为结果。也可以使用 `-f` 或 stdin。 |
| `--path <path>` | 否 | Unity 项目路径（默认：当前目录） |
| `-f, --file <file>` | 否 | 从文件读取代码（`-` 表示 stdin） |
| `--using <namespace>` | 否 | 额外的命名空间，可重复 |
| `--include <path>` | 否 | 辅助 `.cs` 文件或文件夹（递归），相对于 `<Project>/.ai-game-dev/snippets`，可重复 |
| `--window` | 否 | 以带窗口而非无界面的方式启动编辑器（仅在编辑器尚未运行时） |
| `--idle <seconds>` | 否 | 由 `exec` 启动的编辑器在空闲指定秒数后退出（默认：300） |
| `--ready-timeout <ms>` | 否 | 等待编辑器就绪的时长（默认：300000） |
| `--timeout <ms>` | 否 | 工具调用超时时间（默认：310000） |
| `--raw` | 否 | 打印原始的工具响应 JSON |
| `--format <format>` | 否 | `text`（默认）、`json` 或 `ndjson` |
| `--once` | 否 | 如果编辑器是本次调用启动的，则在调用后关闭它 |
| `--url <url>` | 否 | 覆盖服务器 URL；本地 URL 还会让新启动的编辑器启动该服务器 |
| `--token <token>` | 否 | 覆盖 bearer 令牌 |

`exec` 会调用 `script-execute` 工具，因此代码片段、`usings` 和 `include` 需要支持 Code-Exec 的插件版本。编译错误或异常会以退出码 6 退出，并指向你自己代码中的行。

> **尚未登录？** 新项目以 **Cloud** 连接模式启动。在没有登录 ai-game.dev 的情况下，`exec`、
> `run-tool`、`compile` 和 `tools` 会改用项目的**本地** MCP 服务器（它们会在 stderr 上说明）：它们启动的编辑器
> 会在内存中被告知提供该服务器——项目配置不会被更改。运行 `login` 即可通过云端工作。
>
> 当项目的脚本无法编译时，由 CLI 启动的无界面编辑器会继续运行
> （`-ignorecompilererrors`），以便 `compile` 能够报告错误；其日志为 `Logs/unity-mcp-cli-editor.log`。

## `test`

像 CI 那样运行项目的 Unity 测试：使用**批处理模式编辑器和 Unity 自带的 `-runTests`**——不涉及 AI Game
Developer 插件和 MCP 服务器，因此即使项目的插件损坏或缺失也能工作。结论
来自本次运行写出的 NUnit 报告（同一路径下的旧报告会先被删除），而从不仅凭
编辑器的退出码判断。

```bash
unity-mcp-cli test ./MyGame                                  # EditMode, report in ./test-results.xml
unity-mcp-cli test ./MyGame --mode PlayMode --filter "MyGame.Tests.Combat" --junit-output junit.xml
unity-mcp-cli test ./MyGame --format github                  # ::error annotations at the failing test's source line
unity-mcp-cli test ./MyGame --format json --timeout 900 -- -nographics
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 否 | Unity 项目路径（位置参数或 `--path`，默认：当前目录） |
| `--mode <mode>` | 否 | `EditMode`（默认）或 `PlayMode` |
| `--filter <pattern>` | 否 | Unity `-testFilter`：完整测试名称或正则表达式，多个之间用 `;` 分隔 |
| `--category <names>` | 否 | Unity `-testCategory` |
| `--assembly <names>` | 否 | Unity `-assemblyNames` |
| `--output <file>` | 否 | NUnit 报告路径（默认：`<project>/Logs/test-results.xml`） |
| `--junit-output <file>` | 否 | 同时写出 JUnit 报告 |
| `--log-file <file>` | 否 | 编辑器日志（默认：报告路径加上 `.log`） |
| `--timeout <seconds>` | 否 | 超过此时长后结束编辑器（及其子进程）（默认：3600） |
| `--nographics` | 否 | 在没有 GPU 的情况下运行 |
| `--unity <version>` / `--editor-path <path>` | 否 | 要使用的编辑器（默认：项目的版本） |
| `--format <format>` | 否 | `text`、`json`、`ndjson` 或 `github` |
| `-- <args>` | 否 | 额外的编辑器参数；由 `test` 管理的参数（`-batchmode`、`-runTests`、`-quit`、...）会被拒绝 |

**退出码：** `0` 全部通过（运行零个测试也算通过，但会给出警告），`8` 有测试失败，`6` 没有结论（编译
错误——以 `file(line,col): error CS...` 形式列出——许可证问题、崩溃，或项目**已在某个
编辑器中打开**：第二个编辑器无法打开它，因此请改为在该编辑器上使用 `run-tool tests-run`），`14` 超时，`2` 参数
错误。按 Ctrl+C 或取消 CI 作业也会停止编辑器。

## `compile`

针对项目的编辑器回答"我的 C# 能编译通过吗？"：刷新资源以便编译已更改的脚本，等待编译
及其域重载完成，并从编辑器的 Console 中报告编译器错误和警告，附带文件、行号和
列号。没有编辑器运行时会启动一个无界面编辑器（像 `exec` 一样保持预热以供下次调用）；对已预热的
编辑器调用只需几秒钟。

```bash
unity-mcp-cli compile ./MyGame
unity-mcp-cli compile ./MyGame --strict --format github   # warnings fail too
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 否 | Unity 项目路径（位置参数或 `--path`） |
| `--strict` | 否 | 编译器警告也视为失败 |
| `--timeout <seconds>` | 否 | 等待编译和域重载完成（默认：180） |
| `--ready-timeout <ms>` | 否 | 等待已启动的编辑器（默认：300000） |
| `--once` | 否 | 如果编辑器是本次调用启动的，则在之后关闭它 |
| `--url <url>` / `--token <token>` | 否 | 服务器覆盖，与 `exec` 相同 |
| `--format <format>` | 否 | `text`、`json`、`ndjson` 或 `github` |

**退出码：** `0` 编译通过，`6` 有编译错误（使用 `--strict` 时也包括警告），`7` 无法访问任何编辑器，`14`
超时，`18` 模态对话框阻塞了编辑器。诊断信息以编译器自身的
`file(line,col): error CS0103: ...` 格式输出到 stderr。

## `dialog`

模态对话框（保存提示、"Enter Safe Mode?"、`EditorUtility.DisplayDialog`）会阻塞编辑器的主线程，因此
每个工具调用都要等到有人回答它为止。`dialog` 会列出项目编辑器中的对话框，并可以按下某个
按钮。`status`、`run-tool`、`exec`、`compile` 和 `wait-for-ready` 会将这类对话框报告为退出码 `18`
（`AWAITING_INPUT`），而不是普通的超时。支持 Windows、macOS 和 Linux；无界面（`-batchmode`）编辑器从不显示
对话框。

| 平台 | 方式 | 要求 |
|---|---|---|
| **Windows** | 通过 PowerShell 调用 Win32（`EnumWindows`、`BM_CLICK`） | 无 |
| **macOS** | 通过 `osascript -l JavaScript` 使用 System Events UI 脚本：编辑器的对话框窗口和表单（sheet）、其中的文本和按钮 | 为运行 CLI 的应用（你的终端或 AI 智能体应用）授予**辅助功能权限**：系统设置 → 隐私与安全性 → 辅助功能。没有此权限时，`dialog` 会失败并给出该提示，退出码 18 检查也找不到任何对话框。 |
| **Linux** | 通过 `python3` 辅助程序（PyGObject）使用 AT-SPI 辅助功能总线：编辑器的对话框和警告窗口（GTK，Unity 的 `EditorUtility.DisplayDialog` 使用它）、其中的标签和按钮，通过其 `click` 动作按下。支持 X11 和 Wayland | 带有 PyGObject 和 AT-SPI typelib 的 `python3`（Debian/Ubuntu：`python3-gi gir1.2-atspi-2.0`；Fedora：`python3-gobject at-spi2-core`；Arch：`python-gobject at-spi2-core`）以及正在运行的辅助功能总线（`at-spi2-core`，在 GNOME、KDE 和大多数桌面环境中按需启动）。请在运行编辑器的桌面会话中运行 CLI。缺少软件包、总线不可达或没有图形会话时，`dialog` 会失败并给出说明修复方法的提示，退出码 18 检查也找不到任何对话框。 |

Windows 还会列出 Unity 自己的模态窗口，例如 Unity 6.6 在导入未签名的
`.unitypackage`（所有 Asset Store 包）之前显示的 "Missing signature"：Unity 自行绘制其文本和按钮，因此列表中只显示
它们的标题和一个按钮 **Cancel**，`--press Cancel` 会将其作为 Escape 键发送。其他任何回答（Import Anyway）
都由用户在编辑器中做出。

对话框 id 来自列表，并在该对话框打开期间保持不变（Windows：其窗口句柄；macOS 和
Linux：其 pid、标题、文本和按钮的哈希值）。按钮标签从不会被放入脚本中，只作为数据传递。

在无界面的 Linux 机器上，请在同一个 `dbus-run-session` 中，于 `Xvfb` 下运行编辑器（不带 `-batchmode`）和 CLI，
并安装 `at-spi2-core`。如果同一用户还有桌面会话，请为该私有会话
指定单独的 `XDG_RUNTIME_DIR`：否则它的辅助功能总线会替换（并在退出时删除）桌面的
`at-spi` 套接字。

```bash
unity-mcp-cli dialog ./MyGame                          # list: title and buttons
unity-mcp-cli dialog ./MyGame --press "Don't Save"     # answer it
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 否 | Unity 项目路径（位置参数或 `--path`） |
| `--press <button>` | 否 | 按下带有此标签的按钮（不区分大小写） |
| `--id <id>` | 否 | 配合 `--press`：要回答的对话框（来自列表） |
| `--format <format>` | 否 | `text`、`json` 或 `ndjson` |

## `commands`

列出每个命令及其参数、选项和默认值，以及退出码表——直接从 CLI 自身读取，因此
永远不会与实际不一致。供智能体和工具使用。

```bash
unity-mcp-cli commands --format json
```

## `wait-for-ready`

等待 Unity 编辑器和 MCP 服务器完成连接并准备好接受工具调用。按可配置的间隔轮询服务器，直到它成功响应或超时。适用于自动化脚本和 AI 智能体编排：`open` 会启动 Unity，但智能体需要知道何时可以开始调用工具。

```bash
unity-mcp-cli wait-for-ready ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 否 | Unity 项目路径（位置参数或 `--path`）——用于读取配置和检测端口 |
| `--url <url>` | 否 | 直接覆盖服务器 URL（绕过配置） |
| `--token <token>` | 否 | 覆盖 bearer 令牌（绕过配置） |
| `--timeout <ms>` | 否 | 最长等待时间，单位为毫秒（默认：120000） |
| `--interval <ms>` | 否 | 轮询间隔，单位为毫秒（默认：3000） |
| `--format <format>` | 否 | `text`（默认）、`json` 或 `ndjson` |

就绪时退出码为 `0`，超时为 `14`，模态对话框阻塞编辑器时为 `18`。

**示例 — 使用默认超时（120 秒）等待：**

```bash
unity-mcp-cli open ./MyGame
unity-mcp-cli wait-for-ready ./MyGame
unity-mcp-cli run-tool tests-run ./MyGame --input '{"testMode":"EditMode"}'
```

**示例 — CI 中使用更短的超时：**

```bash
unity-mcp-cli wait-for-ready ./MyGame --timeout 60000 --interval 2000
```

**示例 — 显式指定服务器 URL：**

```bash
unity-mcp-cli wait-for-ready --url http://localhost:8080 --timeout 30000
```

## `setup-mcp`

为 AI 智能体写入 MCP 配置文件，无需 Unity 编辑器 UI 即可完成无界面/CI 设置。支持全部 14 种智能体（Claude Code、Cursor、Gemini、Codex 等）。

配置通过按项目区分的 URL **固定**到此项目（http 传输使用 `https://ai-game.dev/mcp/p/<pin>`，stdio 使用 `project=<pin>` 参数）。对于 Cloud http 传输，它还会为每个智能体携带此项目的**项目密钥**（`Authorization: Bearer agd_pk_…`）：这是一种永不过期、可撤销、仅绑定到此项目的凭据，会复用 `~/.ai-game-dev/project-keys.json` 中已有的密钥，或使用你通过 [`login`](#login) 登录的账户签发。未登录时（或无法获取密钥时），配置只包含 URL，智能体会使用其自身的 OAuth 登录。传入 `--oauth` 可始终写入仅含 URL 的配置；传入 `--regenerate-key` 可替换密钥（此项目中其他携带旧密钥的智能体配置会被迁移到新密钥并打印出来，然后撤销旧密钥；如果其中任何一个无法重写，旧密钥会保持有效，并有警告说明）；传入 `--token` 可改为写入显式的 PAT；传入 `--no-pin` 则使用共享的、未固定的端点。stdio 和本地服务器配置从不携带项目密钥。Antigravity 根据安装方式读取 `~/.gemini/config/mcp_config.json` 或 `~/.gemini/antigravity/mcp_config.json`，因此两个文件都会被写入，并打印每个写入的路径；如果其中任何一个无法写入，命令就会失败。此输出与 Unity 编辑器中 **Configure** 按钮的结果一致。

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[agent-id]` | 是 | 要配置的智能体（使用 `--list` 查看全部） |
| `[path]` | 否 | Unity 项目路径（默认为当前工作目录） |
| `--transport <transport>` | 否 | 传输方式：`stdio` 或 `http`（默认：`http`） |
| `--url <url>` | 否 | 覆盖服务器 URL（用于 http 传输） |
| `--token <token>` | 否 | 显式 PAT——作为 `Authorization` 头写入，取代项目密钥 |
| `--oauth` | 否 | 写入仅含 URL 的 Cloud 配置（智能体使用其自身的 OAuth 登录），取代项目密钥 |
| `--regenerate-key` | 否 | 签发新的项目密钥，重写配置，并撤销旧密钥（仅限 Cloud http） |
| `--no-pin` | 否 | 写入未固定的 URL / 省略 `project=` 参数（默认：通过 `/mcp/p/<pin>` 固定到此项目） |
| `--list` | 否 | 列出所有可用的智能体 ID |

**示例 — 列出所有支持的智能体：**

```bash
unity-mcp-cli setup-mcp --list
```

**示例 — 使用 stdio 传输配置 Cursor：**

```bash
unity-mcp-cli setup-mcp cursor ./MyGame --transport stdio
```

**示例 — 替换此项目的密钥（旧密钥将失效）：**

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame --regenerate-key
```

**示例 — 写入未固定的配置（共享的 `/mcp` 端点）：**

```bash
unity-mcp-cli setup-mcp claude-code ./MyGame --no-pin
```

## `setup-skills`

为 AI 智能体写入 Skill 文件。始终离线写入（无需编辑器，无需服务器）：`unity-mcp-cli` skill，说明智能体
如何从冷启动开始使用此 CLI（第一个编辑器命令会启动无界面编辑器；`rql` 常用示例；`compile` / `test`；
`--format json`；退出码和 `dialog`），并附带 `references/rql.md` 和 `references/install.md` 提供更深入的内容，另外还有
`ai-game-dev-app`。这些常用示例与 `--help` 来自同一来源，因此两者不会不一致。然后，当
项目的编辑器有响应时，它还会向插件请求每个工具的 skill；编辑器关闭不算错误。

```bash
unity-mcp-cli setup-skills claude-code ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[agent-id]` | 是 | 要为其生成 skill 的智能体（使用 `--list` 查看全部） |
| `[path]` | 否 | Unity 项目路径（默认为当前工作目录） |
| `--url <url>` | 否 | 覆盖服务器 URL |
| `--token <token>` | 否 | 覆盖身份验证令牌 |
| `--list` | 否 | 列出所有智能体及其 skill 支持状态 |
| `--offline` | 否 | 只写入 `unity-mcp-cli` skill；不询问编辑器 |
| `--timeout <ms>` | 否 | 请求超时时间，单位为毫秒（默认：60000） |

**示例 — 列出支持 skill 的智能体：**

```bash
unity-mcp-cli setup-skills --list
```

## `remove-plugin`

从 Unity 项目的 `Packages/manifest.json` 中移除 Unity-MCP 插件。

```bash
unity-mcp-cli remove-plugin ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 是 | Unity 项目路径（位置参数或 `--path`） |

此命令会：
1. 从 `dependencies` 中移除 `com.ivanmurzak.unity.mcp`
2. **保留作用域注册表和作用域**——其他包可能依赖它们
3. 如果插件未安装，则**不执行任何操作**

> 运行此命令后，请在 Unity 编辑器中打开项目以应用更改。

## `status`

检查 Unity 编辑器和 MCP 服务器的连接状态。显示 Unity 是否正在运行、本地 MCP 服务器是否可访问，以及根据配置解析出的服务器（例如云端）是否可访问。

```bash
unity-mcp-cli status ./MyGame
```

| 选项 | 必需 | 描述 |
|---|---|---|
| `[path]` | 否 | Unity 项目路径（位置参数或 `--path`） |
| `--url <url>` | 否 | 直接覆盖服务器 URL（绕过配置） |
| `--token <token>` | 否 | 覆盖 bearer 令牌（绕过配置） |
| `--timeout <ms>` | 否 | 探测超时时间，单位为毫秒（默认：5000） |
| `--format <format>` | 否 | `text`（默认）、`json` 或 `ndjson` |

MCP 服务器有响应时退出码为 `0`，无响应时为 `7`，模态对话框阻塞编辑器时为 `18`（对话框会列在
输出中；参见 [`dialog`](#dialog)）。

**输出示例：**

```
Unity-MCP Status
  Project: /path/to/MyGame
──────────────────────────────────────────────────
Unity Editor Process
✔  Unity is running (PID: 53740)
Local MCP Server
  URL: http://localhost:22958
✖  Not available (connection refused)
Config Server
  URL: https://ai-game.dev/mcp
✔  Connected
──────────────────────────────────────────────────
✔  MCP server is reachable — ready for tool calls
```

## 全局选项

以下选项适用于所有命令：

| 选项 | 描述 |
|---|---|
| `-v, --verbose` | 启用详细的诊断输出，便于排查问题 |
| `--version` | 显示 CLI 版本 |
| `--help` | 显示该命令的帮助 |

**示例 — 以详细输出模式运行任意命令：**

```bash
unity-mcp-cli install-plugin ./MyGame --verbose
```

## 退出码与机器可读输出

`unity-mcp-cli`、`unreal-mcp-cli` 和 `godot-cli` 共用同一张退出码表，因此脚本或智能体无需读取消息，就能区分"修正
命令行"、"编辑器出错"和"测试失败"：

| 代码 | 名称 | 含义 |
|---|---|---|
| `0` | `SUCCESS` | 命令成功 |
| `1` | `ERROR` | 未分类的错误 |
| `2` | `USAGE` | 参数错误或未知工具；未执行任何操作。请修正命令行——不要原样重试 |
| `6` | `COMMAND_FAILED` | 命令已运行但失败（工具错误、编译错误、测试运行没有结论、凭据被拒绝） |
| `7` | `UNREACHABLE` | 无法启动或访问编辑器或 MCP 服务器；修复环境后再重试 |
| `8` | `TESTS_FAILED` | 测试已运行，部分失败 |
| `14` | `TIMEOUT` | 在得到结果之前超时 |
| `17` | `BUSY` | 编辑器忙碌，未执行该调用；请稍后重试 |
| `18` | `AWAITING_INPUT` | 模态对话框阻塞了编辑器；需要人工回答（参见 [`dialog`](#dialog)） |

`--format json`（适用于 `run-tool`、`run-system-tool`、`exec`、`test`、`compile`、`dialog`、`status`、`wait-for-ready` 和
`commands`）会在 stdout 上只打印一个结果封装对象；其他所有内容都输出到 stderr。`--format ndjson` 会将
同样的封装对象打印为一行，并在 stderr 上流式输出进度行（`{"type":"progress",...}`）。在
`--format json` 下出现命令行错误时，仍会打印封装对象（代码为 `USAGE`）。

```json
{
  "success": false,
  "command": "test",
  "data": { "verdict": "failed", "summary": { "total": 3, "passed": 2, "failed": 1 } },
  "errors": [{ "code": "TESTS_FAILED", "message": "1 test(s) failed." }],
  "warnings": []
}
```

`errors[].code` 是一个稳定的标记（`USAGE`、`NOT_FOUND`、`NOT_AUTHENTICATED`、`TOOL_FAILED`、`UNREACHABLE`、`TIMEOUT`、
`BUSY`、`AWAITING_INPUT`、`TESTS_FAILED`、`TEST_RUN_ERROR`、`TEST_TIMED_OUT`、`COMPILATION_FAILED`、`PROJECT_OPEN`、
`ERROR`、...）；请匹配它，而不要匹配消息文本。

**未成功的工具调用**（`run-tool`、`run-system-tool`、`exec`、`rql`、`tools`）在三个 CLI 中都映射到相同的退出码
和错误代码（该表位于 `@baizor/gamedev-cli-core` 中；`commands --format json` 会将其列为
`errorCodes`）。当 MCP 服务器有响应时，由其 HTTP 状态码决定结果，封装对象中的错误也会携带该状态码：
`errors[{code, message, httpStatus}]`。

| 情况 | HTTP 状态码 | 退出码 | `errors[].code` |
|---|---|---|---|
| 参数错误：错误的标志、被 CLI 或服务器拒绝的工具参数（此处未列出的任何 4xx） | 400（422 `[invalid_params]`） | `2` | `USAGE` |
| 没有该名称的工具（用 `tools` 列出工具），或该路径下没有项目 | 404 | `2` | `NOT_FOUND` |
| 没有可用的凭据，或服务器拒绝了凭据：请用 `login` 登录 | 401, 403 | `6` | `NOT_AUTHENTICATED` |
| 工具已运行并报告了错误（`status: "error"`，此处未列出的任何 5xx） | 409, 422, 500 | `6` | `TOOL_FAILED` |
| 项目代码无法构建，因此没有启动编辑器 | — | `6` | `COMPILATION_FAILED` |
| 连接被拒绝或重置、没有引擎插件连接到服务器、调用发出后插件丢失 | 502 | `7` | `UNREACHABLE` |
| 未及时响应：`--timeout`，或服务器自身的工具超时 | 408, 504 | `14` | `TIMEOUT` |
| 忙碌且未执行该调用（正在重载、被限流、另一个 CLI 调用占用着编辑器）；请稍后重试 | 503, 429 | `17` | `BUSY` |
| 模态对话框阻塞了编辑器 | — | `18` | `AWAITING_INPUT` |
| 其他任何情况（调用被中止、意外错误） | — | `1` | `ERROR` |

# 完整自动化示例

用一个脚本从零开始搭建完整的 Unity MCP 项目：

```bash
# 1. Create a new Unity project
unity-mcp-cli create-project ./MyAIGame --unity 6000.3.1f1

# 2. Install the Unity-MCP plugin
unity-mcp-cli install-plugin ./MyAIGame

# 3. Enable all MCP tools
unity-mcp-cli configure ./MyAIGame --enable-all-tools

# 4. Sign in to ai-game.dev (browser OAuth; credential saved to ~/.ai-game-dev)
unity-mcp-cli login

# 5. Open the project (auto-connects and generates skills for claude-code)
unity-mcp-cli open ./MyAIGame

# 6. Wait for Unity Editor and MCP server to be ready
unity-mcp-cli wait-for-ready ./MyAIGame

# 7. Check that the scripts compile, then close the editor
unity-mcp-cli compile ./MyAIGame
unity-mcp-cli close ./MyAIGame

# 8. Run the tests in a batch-mode editor (no plugin needed); JUnit for the CI dashboard
unity-mcp-cli test ./MyAIGame --junit-output junit.xml
```

# 工作原理

### 确定性端口

CLI 会根据每个 Unity 项目的目录路径为其生成一个**确定性端口**（SHA256 哈希映射到 20000–29999 端口范围）。这与 Unity 插件中的端口生成方式一致，确保服务器和插件无需手动配置即可自动使用同一个端口。

### 插件安装

`install-plugin` 命令会直接修改 `Packages/manifest.json`：
- 添加 [OpenUPM](https://openupm.com/) 作用域注册表（`package.openupm.com`）
- 注册所有必需的作用域（`com.ivanmurzak`、`extensions.unity`）
- 添加 `com.ivanmurzak.unity.mcp` 依赖，并进行感知版本的更新（从不降级）

### 配置文件

`configure` 命令会读写 `UserSettings/AI-Game-Developer-Config.json`，该文件控制：
- **工具**——AI 智能体可用的 MCP 工具
- **提示词**——注入到 LLM 对话中的预定义提示词
- **资源**——向 AI 智能体公开的只读数据
- **连接设置**——主机 URL、身份验证令牌、传输方式、超时

### Unity Hub 集成

管理编辑器或创建项目的命令会使用 **Unity Hub CLI**（`--headless` 模式）。如果未安装 Unity Hub，CLI 会**自动下载并安装它**：
- **Windows**——通过 `UnityHubSetup.exe /S` 静默安装（可能需要管理员权限）
- **macOS**——下载 DMG，挂载它，并将 `Unity Hub.app` 复制到 `/Applications`
- **Linux**——将 `UnityHub.AppImage` 下载到 `~/Applications/`

# 库 API (v0.67.0+)

除了 CLI 可执行文件之外，`unity-mcp-cli` 还将其核心命令公开为一个类型化、无副作用的库，以便其他 Node.js / TypeScript 工具无需调用 shell 即可嵌入相同的安装 / 配置流程。

```ts
import { installPlugin, removePlugin, configure, setupMcp } from 'unity-mcp-cli';

const result = await installPlugin({
  unityProjectPath: './MyUnityProject',
  // version: '0.67.0',        // optional — defaults to latest from OpenUPM
  onProgress: (event) => {
    // phase is one of: 'start' | 'dependencies-resolved' | 'manifest-patched' | 'done'
    console.log(event.phase, event.message);
  },
});

if (!result.success) {
  console.error('Install failed:', result.error?.message);
  return;
}

console.log(`Installed v${result.installedVersion}`);
for (const warning of result.warnings) console.warn(warning);
for (const step of result.nextSteps) console.log(step);
```

每个函数都返回一个类型化的 `{ success, ... }` 结果对象；错误从不会被抛出到公共边界之外。库入口没有任何顶层副作用——`import 'unity-mcp-cli'` 从不解析 argv，也从不向 stdout 或 stderr 写入内容。

完整的导出函数和类型列表请参见 [`CHANGELOG.md`](../CHANGELOG.md#0670---2026-04-21)。
