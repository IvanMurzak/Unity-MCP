[English](benchmark.md) | [中文](benchmark.zh-CN.md) | [日本語](benchmark.ja.md) | <b>Español</b> | [فارسی](benchmark.fa.md) | [हिन्दी](benchmark.hi.md) | [العربية](benchmark.ar.md)

# Benchmark: herramientas compactas en el Editor de Unity

Volver al [README](README.es.md).

AI Game Developer da al agente unas pocas herramientas compactas basadas en **RQL**, un lenguaje de consultas sobre los datos del Editor, en lugar de una herramienta por acción. Ejecutamos las mismas tareas en el Editor de Unity real con ambos catálogos de herramientas. El agente resolvió el mismo número de tareas con **unas 5 veces menos tokens de prompt**, menos turnos y menos llamadas a herramientas, y terminó cada tarea más rápido. Una segunda prueba, más grande, en un mundo de juego de prueba (sandbox) muestra el mismo patrón.

## Cifras clave (Editor de Unity)

| 5.3× | −31% | −39% | 59 / 60 |
| :---: | :---: | :---: | :---: |
| menos tokens de prompt por tarea | llamadas a herramientas por tarea | tiempo por tarea | tareas resueltas, en ambos catálogos |

<p align="center"><img src="img/benchmark/unity-editor.svg" alt="Benchmark en el Editor de Unity: tokens de prompt por tarea, 138,272 con herramientas por acción frente a 26,013 con herramientas RQL; llamadas a herramientas por tarea, 5.5 frente a 3.8; tareas resueltas, 59 de 60 en ambos casos" width="100%"></p>

## Resultados en el Editor de Unity

| Métrica | Herramientas por acción | Herramientas RQL |
| --- | ---: | ---: |
| Tareas resueltas (comprobación superada, sin cambios no deseados) | 59 / 60 | 59 / 60 |
| Tokens de prompt por tarea | 138,272 | **26,013** |
| Tokens de respuesta por tarea | 673 | **306** |
| Turnos del modelo por tarea | 5.4 | **4.1** |
| Llamadas a herramientas / errores por tarea | 5.5 / 0.5 | **3.8 / 0.2** |
| Tamaño de los resultados de herramientas por tarea (caracteres) | 6,273 | **657** |
| Definiciones de herramientas enviadas en cada solicitud (caracteres) | 93,194 | **22,439** |
| Tiempo real por tarea | 8.8 s | **5.4 s** |
| Tiempo de ejecución de herramientas por tarea | 169 ms | **42 ms** |
| Coste por tarea | 1× | **aproximadamente ⅓** |

**Por qué es más barato.** La mayor parte de la diferencia viene de dos sitios: las definiciones de herramientas que el agente recibe con cada solicitud son 4.2 veces más pequeñas, y los resultados de las herramientas, 9.5 veces más pequeños. En las tareas masivas y de consulta, el catálogo por acción recurrió a escribir C# (`script-execute` 68 veces en todas las ejecuciones, frente a 7 con RQL); con RQL, el agente escribe una sola consulta.

**Ejecución de control.** Tras las migraciones de las extensiones y las correcciones del núcleo (deshacer en cadenas de propietarios, rutas de assets, `unity.find`, formato de números), el catálogo RQL resolvió **60 / 60** tareas con 23,274 tokens de prompt, 3.4 llamadas a herramientas y 4.8 s por tarea.

## Resultados en el mundo de juego de prueba (sandbox)

El proyecto RQL mide el lenguaje por sí solo, fuera de Unity: un mundo de juego generado y un agente que usa RQL o herramientas de reflexión simples (`find_objects`, `get_object` como volcado JSON, `describe_type`, `set_value`, `call_method`). La comprobación más limpia usó 30 tareas nuevas escritas después de congelar todo el ajuste, ejecutadas una sola vez, 3 veces cada una.

<p align="center"><img src="img/benchmark/sandbox.svg" alt="Benchmark en el sandbox: tokens por tarea, 169,190 con herramientas de reflexión simples frente a 7,817 con RQL; llamadas a herramientas por tarea, 36.9 frente a 1.9; tareas resueltas, 66.7% frente a 100%" width="100%"></p>

| Ejecución | Tareas resueltas | Tokens por tarea | Llamadas a herramientas por tarea | Coste por éxito |
| --- | ---: | ---: | ---: | ---: |
| Herramientas de reflexión, tareas escritas tras el ajuste | 66.7% | 169,190 | 36.9 | 40× |
| RQL, tareas escritas tras el ajuste | **100%** | **7,817** | **1.9** | **1×** |
| Herramientas de reflexión, tareas reservadas | 91.4% | 125,566 | 34.0 | 22× |
| RQL, tareas reservadas | **100%** | **7,444** | **1.9** | **1×** |

Las herramientas de reflexión fallan sobre todo porque se quedan sin pasos en las tareas de «buscar en todo», leyendo el mundo objeto por objeto.

## Metodología

**Editor de Unity.** Medido con Unity-MCP 0.93.4 (todas las herramientas habilitadas, una herramienta por acción: `gameobject-*`, `assets-*`, `scene-*`, `object-*`, `console-get-logs`, `script-execute`, `tests-run`, …) frente al catálogo RQL (`rql-query`, `rql-mutate`, `rql-schema`, `script-execute`, `tests-run`, `screenshot-isolated`, `unity-tool-list`). Mismo arnés de pruebas, escena y modelo para ambos: `deepseek-flash`, razonamiento desactivado, temperatura 0. 20 tareas × 3 ejecuciones, Unity 6000.6.4f1. Cada condición recibió exactamente las herramientas que ve un cliente MCP, con sus propios nombres, descripciones y esquemas de entrada, llamadas a través del plugin. Una tarea se supera solo cuando pasa su comprobación y nada más cambió en la escena.

**Sandbox.** Mismas tareas, evaluador, modelo y presupuestos para ambos conjuntos de herramientas; solo cambian las herramientas. Modelo DeepSeek V4.1-Flash, como máximo 12 pasos por tarea, 3 ejecuciones por tarea. Un evaluador oculto calcula la respuesta esperada de forma independiente; las tareas de modificación fallan ante cualquier cambio no deseado. Las tareas reservadas nunca se usaron para el ajuste; el segundo conjunto de tareas se escribió después de congelar el ajuste y se ejecutó una sola vez.

## Advertencias

- **Una sola familia de modelos.** Ambas pruebas usaron modelos de DeepSeek. Otros modelos pueden dar cifras distintas.
- **Versiones medidas.** Las cifras del Editor de Unity comparan Unity-MCP 0.93.4 con el catálogo RQL en una escena y una versión de Unity; las herramientas y los modelos cambian con el tiempo.
- **Muestras pequeñas.** 60 ejecuciones por condición en el Editor; de 81 a 90 ejecuciones por condición en los resultados del sandbox anteriores.
- **Mismo autor.** Las tareas de RQL y el sandbox los diseñó el autor del lenguaje. Las tareas reservadas y el conjunto de tareas escrito tras el ajuste reducen este sesgo, pero no lo eliminan.
- **El coste es una proporción.** El almacenamiento en caché de prompts hace que el coste dependa en parte de otro tráfico; el número de tokens y de llamadas a herramientas son las señales más estables.

## Datos sin procesar

- Informe del Editor de Unity: [`evaluation/reports/unity-mcp-old-vs-rql-branch.md`](https://github.com/IvanMurzak/RQL/blob/main/evaluation/reports/unity-mcp-old-vs-rql-branch.md) en el repositorio de RQL.
- Resultados del sandbox: [RQL `RESULTS.md`](https://github.com/IvanMurzak/RQL/blob/main/RESULTS.md), secciones 3.3 y 3.4, y la [metodología de evaluación](https://github.com/IvanMurzak/RQL/blob/main/docs/evaluation-methodology.md).

Ambas mediciones proceden del arnés de evaluación del proyecto [RQL](https://github.com/IvanMurzak/RQL).
