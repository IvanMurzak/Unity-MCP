/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Splines)        │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Splines
{
    /// <summary>
    /// The extension's RQL module (discovered through TypeCache when this package is installed): the <c>splines</c> source
    /// (containers, CreateContainer), SplineContainer methods and spline / knot views, Unity.Mathematics literals, and the
    /// agent guide rendered into the unity-rql skill.
    /// </summary>
    public sealed class SplinesModule : IRqlModule
    {
        private const string C = "objects | get components | where $ is SplineContainer";

        internal const string Guide =
@"A `SplineContainer` component holds splines; `splines[i]` has `closed`, `knotCount`, `length` (world units) and
`knots[j]` with writable `position` (container-local), `worldPosition`, `tangentIn`, `tangentOut`, `eulerAngles`,
`tangentMode` (AutoSmooth, Linear, Mirrored, Continuous, Broken) and `tension`. The `splines` source lists every
container (`splines | get containers`) and creates new ones. Writes and calls are undoable. `rql-schema ""SplineContainer""`
lists the methods; `… | get splines | get knots | first 1 | describe` lists the knot members.

```
splines | call CreateContainer(""Road"")   new GameObject + SplineContainer (one empty spline)
splines | call CreateContainer(""Loop"", ""(0, 0, 5)"", ""(0, 90, 0)"", true)   name, position, eulerAngles, closed
splines | get containers | select gameObject.name n=(splines.count()) splines.knotCount
" + C + @" | where gameObject.name == ""Road"" | call AddKnots([""(0, 0, 0)"", ""(5, 0, 2)"", ""(10, 0, 0)""])   append several knots to spline 0
" + C + @" | where gameObject.name == ""Road"" | call AddKnot(""(15, 0, 3)"")   append one knot to spline 0
" + C + @" | where gameObject.name == ""Road"" | call AddSpline()   returns the new spline index
" + C + @" | where gameObject.name == ""Road"" | call AddKnot(1, ""(0, 2, 0)"")   append to spline 1
" + C + @" | where gameObject.name == ""Road"" | call InsertKnot(1, ""(2, 0, 1)"")   insert at knot index 1 (InsertKnot(spline, knot, position) for others)
" + C + @" | where gameObject.name == ""Road"" | call RemoveKnot(0)   RemoveKnot(spline, knot) for other splines
" + C + @" | where gameObject.name == ""Road"" | call Evaluate(0.5)   point at t: position (world) localPosition tangent up
" + C + @" | where gameObject.name == ""Road"" | get splines | get knots | select index position tangentMode
" + C + @" | where gameObject.name == ""Road"" | set splines[0].knots[2].position = ""(10, 1, 0)""   move a knot (container-local)
" + C + @" | where gameObject.name == ""Road"" | set splines[0].knots[0].tangentMode = ""Linear""
" + C + @" | where gameObject.name == ""Road"" | get splines | get knots | set tangentMode = ""AutoSmooth""   every knot of every spline
" + C + @" | where gameObject.name == ""Road"" | set splines[0].closed = true
" + C + @" | where gameObject.name == ""Road"" | call RemoveSpline(1)
```

Knot methods take an optional leading spline index (`AddKnot(position)` = spline 0, `AddKnot(1, position)` = spline 1);
new knots are AutoSmooth. Writing a tangent of an AutoSmooth / Linear knot switches it to Broken so the value sticks.
Evaluate needs rql-mutate (it is a call); its result column holds the sample. Unity.Mathematics `float2` / `float3` /
`float4` / `quaternion` values print and are written like vectors: `""(1, 2, 3)""`; a quaternion also accepts euler
degrees `""(0, 90, 0)""`.";

        public int Order => 100;

        public void Configure(RqlModuleContext ctx)
        {
            ctx.AddSource("splines", () => SplinesApi.Instance,
                   "spline containers of the loaded scenes (containers) and CreateContainer(name, position, eulerAngles, closed, parent)")
               .AddAdapter(new UnityEngine_Splines_SplineContainer_Adapter())
               .AddGuide("Splines", Guide);
        }
    }
}
