[English](ci-disable.md) | [中文](ci-disable.zh-CN.md) | [日本語](ci-disable.ja.md) | <b>Español</b> | [فارسی](ci-disable.fa.md) | [हिन्दी](ci-disable.hi.md) | [العربية](ci-disable.ar.md)

# Desactivar Unity-MCP en CI

Introducido en core **1.6.2** y en las extensiones oficiales **2.1.1**.

Estas dos exclusiones independientes dejan el manifiesto de UPM sin cambios. Ninguna se activa automáticamente
con `-batchmode` o `-runTests`: las sesiones MCP sin interfaz y las propias pruebas de MCP siguen necesitando el plugin.

## Opción 1: omitir la inicialización (recomendada para pruebas de CI normales)

Define la variable de entorno del proceso **antes de iniciar Unity**:

```bash
UNITY_MCP_DISABLE=1 "$UNITY_EDITOR" -batchmode -nographics \
  -projectPath "$PROJECT_PATH" -runTests -testPlatform PlayMode \
  -testResults "$PROJECT_PATH/test-results.xml" -logFile -
```

En GitHub Actions, pon `UNITY_MCP_DISABLE: '1'` en el mapeo `env` del paso que inicia
Unity. En un runner de Docker, reenvíala explícitamente al contenedor de Unity, por ejemplo
`docker run --env UNITY_MCP_DISABLE=1 ...`. Definirla solo en el host no garantiza
que llegue al contenedor de una acción.

`1` y `true` (sin distinguir mayúsculas, ignorando los espacios alrededor) desactivan MCP. Sin definir, `0`,
`false` y los valores no reconocidos mantienen el comportamiento normal. Reinicia Unity después de cambiarla.
Es una anulación a nivel de proceso y no se guarda en la configuración del proyecto.

MCP omite su arranque, el registro de herramientas, los callbacks del test runner, la descarga/inicio del servidor en segundo plano,
la resolución de dependencias NuGet, la recogida de logs y los hooks del editor/PlayMode. Sus fuentes siguen compilándose
y los ensamblados aún pueden cargarse. Por tanto, las dependencias existentes del proyecto deben ser válidas; este modo
no es una solución para DLL ausentes ni errores de compilación. Una inicialización explícita del runtime mientras está
desactivado lanza una excepción. Quita la variable en los jobs que ejercitan MCP de forma intencionada.

## Opción 2: excluir de la compilación los ensamblados de código fuente de MCP

Añade **`UNITY_MCP_EXCLUDE_FROM_COMPILATION`** a los **Scripting Define Symbols** de Unity en
**Project Settings > Player > Other Settings**, para cada build target que usen tus jobs de CI.
Conserva los demás símbolos. Es un símbolo de scripting, **no una variable de entorno**.

Para CI, prepara el `ProjectSettings/ProjectSettings.asset` resultante antes de iniciar Unity
(por ejemplo, usa una copia de la configuración específica para CI). Mantén la configuración habitual de desarrollo sin
este símbolo si los desarrolladores necesitan MCP. No lo definas desde un `-executeMethod` en la misma ejecución de pruebas:
eso ocurre después de la primera compilación, y cambiar los defines provoca otra compilación.

Todos los ensamblados del paquete core (incluidos el resolvedor de dependencias, el runtime, el editor, los módulos RQL
y las pruebas del paquete), además de los ensamblados actualizados de las extensiones oficiales, usan la restricción de ensamblado negada
`!UNITY_MCP_EXCLUDE_FROM_COMPILATION`. El símbolo excluye esos ensamblados en conjunto.
Actualiza también cada extensión MCP instalada a una versión que contenga esta restricción; las versiones anteriores
y las extensiones de terceros aún pueden hacer referencia a los ensamblados core excluidos. El código personalizado que
haga referencia a tipos de MCP debe usar restricciones equivalentes o `#if !UNITY_MCP_EXCLUDE_FROM_COMPILATION`.

Esto excluye los ensamblados de código fuente C# de MCP. No desinstala paquetes UPM, no elimina assets importados
ni borra/desactiva DLL precompiladas compartidas con otros paquetes. Quitar el símbolo restaura la compilación
normal. Cambiar de modo puede invalidar la caché de compilación de Unity; usa claves de caché de CI separadas
para los jobs con exclusión y los normales. La variable de entorno de la opción 1 no es necesaria en este modo.
