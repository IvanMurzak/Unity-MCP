[English](ci-disable.md) | <b>中文</b> | [日本語](ci-disable.ja.md) | [Español](ci-disable.es.md) | [فارسی](ci-disable.fa.md) | [हिन्दी](ci-disable.hi.md) | [العربية](ci-disable.ar.md)

# 在 CI 中禁用 Unity-MCP

自核心包 **1.6.2** 和官方扩展 **2.1.1** 起提供。

以下两种相互独立的退出方式都不会修改 UPM manifest。两者都不会因 `-batchmode` 或 `-runTests`
而自动启用：无界面的 MCP 会话以及 MCP 自身的测试仍然需要该插件。

## 方式 1：跳过初始化（推荐用于普通的 CI 测试）

**在启动 Unity 之前**设置进程环境变量：

```bash
UNITY_MCP_DISABLE=1 "$UNITY_EDITOR" -batchmode -nographics \
  -projectPath "$PROJECT_PATH" -runTests -testPlatform PlayMode \
  -testResults "$PROJECT_PATH/test-results.xml" -logFile -
```

在 GitHub Actions 中，将 `UNITY_MCP_DISABLE: '1'` 放入启动 Unity 的那个步骤的 `env` 映射中。
对于 Docker 运行器，需要显式地将它转发到 Unity 容器中，例如
`docker run --env UNITY_MCP_DISABLE=1 ...`。只在宿主机上设置它，不一定能
传递到 action 的容器中。

`1` 和 `true`（不区分大小写，忽略首尾空白）会禁用 MCP。未设置、`0`、
`false` 以及无法识别的值都会保持正常行为。修改后需要重启 Unity。
这是一个进程级覆盖，不会保存到项目设置中。

MCP 会跳过其启动流程、工具注册、测试运行器回调、后台服务器下载/启动、
NuGet 依赖解析、日志收集以及编辑器/PlayMode 钩子。其源代码仍会编译，
程序集仍可加载。因此现有的项目依赖必须是有效的；此模式
并不能绕过缺失 DLL 或编译错误的问题。在禁用状态下显式进行运行时初始化
会报告异常。对于有意测试 MCP 的作业，请移除该变量。

## 方式 2：将 MCP 源码程序集排除在编译之外

在 **Project Settings > Player > Other Settings** 下，为 CI 作业使用的每个构建目标，
向 Unity 的 **Scripting Define Symbols** 添加 **`UNITY_MCP_EXCLUDE_FROM_COMPILATION`**。
保留其他符号。这是一个脚本符号，**不是环境变量**。

对于 CI，请在启动 Unity 之前准备好相应的 `ProjectSettings/ProjectSettings.asset`
（例如使用 CI 专用的设置副本）。如果开发者需要 MCP，请让常规开发设置保持不含
此符号。不要在同一次测试运行中通过 `-executeMethod` 设置它：
那发生在第一次编译之后，而修改定义符号会触发另一次编译。

所有核心包程序集（包括依赖解析器、运行时、编辑器、RQL 模块
和包测试），以及更新后的官方扩展程序集，都使用取反的程序集
约束 `!UNITY_MCP_EXCLUDE_FROM_COMPILATION`。该符号会将这些程序集一并排除。
请将每个已安装的 MCP 扩展也更新到包含此约束的版本；旧版本
和第三方扩展可能仍会引用被排除的核心程序集。引用 MCP 类型的自定义代码
必须使用相应的约束或 `#if !UNITY_MCP_EXCLUDE_FROM_COMPILATION`。

此方式排除的是 MCP 的 C# 源码程序集。它不会卸载 UPM 包、移除已导入的资源，
也不会删除/禁用与其他包共享的预编译 DLL。移除该符号即可恢复正常
编译。切换模式可能会使 Unity 的编译缓存失效；请为排除模式和正常模式的作业
使用不同的 CI 缓存键。在此模式下不需要方式 1 中的环境变量。
