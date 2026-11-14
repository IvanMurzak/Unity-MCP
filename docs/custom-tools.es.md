[English](custom-tools.md) | [中文](custom-tools.zh-CN.md) | [日本語](custom-tools.ja.md) | <b>Español</b> | [فارسی](custom-tools.fa.md) | [हिन्दी](custom-tools.hi.md) | [العربية](custom-tools.ar.md)

# Herramientas personalizadas y runtime

Añade tus propias **herramientas MCP** y **prompts** en el código del proyecto, y usa MCP dentro de un juego compilado. Volver al [README](README.es.md).

El plugin los encuentra por su atributo y los envía al servidor MCP, de modo que todos los agentes conectados los ven. Los agentes que leen Skills los reciben en la siguiente generación de skills.

## Conceptos

| | Qué es | Para qué sirve |
| --- | --- | --- |
| **Tool** | Un método C# que el agente puede llamar, con parámetros tipados y descritos, y un resultado | Trabajo repetitivo o de varios pasos en el Editor, flujos de trabajo propios del proyecto, validación, exponer sistemas del juego |
| **Prompt** | Una plantilla inyectada en la conversación como mensaje de User o Assistant | Convenciones del proyecto, notas de arquitectura, procedimientos paso a paso |
| **Resource** | Datos de solo lectura que el agente puede inspeccionar | Estado del proyecto que el agente debe conocer sin llamar a una herramienta |

## Añadir una herramienta

1. Una clase con el atributo `[AiToolType]`.
2. Un método con el atributo `[AiTool]` (id, `Title` opcional).
3. Opcional: `[Description]` en el método y en cada parámetro, para que el modelo sepa cuándo y cómo llamarlo.
4. Opcional: los parámetros anulables con valores predeterminados (`string? name = null`) son opcionales para el modelo.

Las API de Unity deben ejecutarse en el hilo principal: envuelve esa parte en `MainThread.Instance.Run(...)`. El trabajo que no toca las API de Unity puede quedarse en el hilo en segundo plano.

```csharp
[AiToolType]
public class Tool_GameObject
{
    [AiTool("my-custom-task", Title = "Create a new GameObject")]
    [Description("Explain to the model what this does and when to call it.")]
    public string CustomTask
    (
        [Description("Explain to the model what this parameter is.")]
        string inputData
    )
    {
        // background thread work

        return MainThread.Instance.Run(() =>
        {
            // main thread work (Unity API)
            return "[Success] Operation completed.";
        });
    }
}
```

`Visible = false` en `[AiTool]` oculta por defecto una herramienta de la lista de herramientas del agente (sigue ejecutándose cuando se llama por su nombre). Muestra u oculta cualquier herramienta en la ventana de AI Game Developer, con la herramienta `tool-set-visibility` o con `unity-mcp-cli configure <path> --enable-tools <ids>`.

## Añadir un prompt

```csharp
[AiPromptType]
public static class Prompt_ScriptingCode
{
    [AiPrompt(Name = "add-event-system", Role = Role.User)]
    [Description("Implement UnityEvent-based communication system between GameObjects.")]
    public static string AddEventSystem()
    {
        return "Create event system using UnityEvents, UnityActions, or custom event delegates for decoupled communication between game systems and components.";
    }
}
```

## Runtime (dentro del juego)

El plugin también se ejecuta dentro de un juego compilado. En runtime no hay herramientas predeterminadas; registras las tuyas. En el Editor (modos Edit y Play) el plugin del editor se configura desde `UserSettings/AI-Game-Developer-Config.json`; en una build, configúralo desde el código:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void SetupMcp()
{
    var mcpPlugin = UnityMcpPluginRuntime.Initialize(builder =>
        {
            builder.WithConfig(c =>
            {
                c.Host = "http://localhost:8080";
                c.CredentialProvider = () => System.Threading.Tasks.Task.FromResult<string?>("my-token");
            });
            // register every tool in this assembly
            builder.WithToolsFromAssembly(Assembly.GetExecutingAssembly());
        })
        .Build();

    mcpPlugin.Connect();     // connect to the MCP server (async, retries)
    // mcpPlugin.Disconnect();
}
```

### Ejemplo: un bot de ajedrez

Deja que el modelo juegue en el lado del bot en una partida de ajedrez. Marca las herramientas exclusivas del juego con `[AiToolType(Target = ToolTarget.Runtime)]` (`using com.IvanMurzak.McpPlugin.Common;`): solo el juego las registra, y el servidor MCP envía sus llamadas al juego en ejecución incluso mientras el Editor está conectado.

```csharp
[AiToolType(Target = ToolTarget.Runtime)]
public static class ChessGameAI
{
    [AiTool("chess-do-turn", Title = "Do the turn")]
    [Description("Do the turn in the chess game. Returns true if the turn was accepted, false otherwise.")]
    public static Task<bool> DoTurn(int figureId, Vector2Int position)
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.DoTurn(figureId, position));
    }

    [AiTool("chess-get-board", Title = "Get the board")]
    [Description("Get the current state of the chess board.")]
    public static Task<BoardData> GetBoard()
    {
        return MainThread.Instance.RunAsync(() => ChessGameController.Instance.GetBoardData());
    }
}
```

`UNITY_MCP_DISABLE` desactiva la inicialización; en ese caso `Initialize` lanza una excepción. Consulta [Desactivar en CI](ci-disable.es.md).

## Más

- Herramientas integradas: [Herramientas MCP predeterminadas](default-mcp-tools.es.md)
- Extensiones como ejemplos de paquetes de herramientas: [`extensions/`](https://github.com/IvanMurzak/Unity-MCP/tree/main/extensions)
- [Wiki](https://github.com/IvanMurzak/Unity-MCP/wiki)
