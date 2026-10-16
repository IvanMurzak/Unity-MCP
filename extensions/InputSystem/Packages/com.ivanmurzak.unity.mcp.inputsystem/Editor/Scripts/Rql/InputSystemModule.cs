/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-InputSystem)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.InputSystem
{
    /// <summary>
    /// The extension's RQL module (discovered through TypeCache when this package is installed): the <c>input</c> source
    /// (assets, layouts, interactions, processors, CreateAsset), the InputActionAsset adapter with its map / action / binding /
    /// control scheme views, and the agent guide rendered into the unity-rql skill.
    /// </summary>
    public sealed class InputSystemModule : IRqlModule
    {
        public const string GuideTopic = "Input System";

        /// <summary>The asset the guide's examples work on.</summary>
        public const string ExamplePath = "Assets/Input/Player.inputactions";

        private const string A = "unity.asset(\"" + ExamplePath + "\")";

        internal const string Guide =
@"An InputActionAsset (`.inputactions` file) has `maps` → `actions` → `bindings` and `controlSchemes`; `actions` also lists
every action of every map. Every `set` and `call` is validated (control paths need a known device layout, interactions /
processors / composites must be registered, groups must be control schemes of the asset), written to the `.inputactions`
file and re-imported at once, and is undoable. Paths look like `""<Keyboard>/space""`, `""<Gamepad>/leftStick""`,
`""<Mouse>/leftButton""`; groups are scheme names joined by `;` (`""Keyboard&Mouse;Gamepad""`).

```
input | call CreateAsset(""" + ExamplePath + @""", ""Player"")   path, map? (first action map); missing folders are created
input | get assets | select $ref name path
" + A + @" | call AddControlScheme(""Keyboard&Mouse"", [""<Keyboard>"", ""<Mouse>""])   name, requiredDevices?, optionalDevices?, bindingGroup? (default: name)
" + A + @" | call AddControlScheme(""Gamepad"", [""<Gamepad>""])
" + A + @" | call AddMap(""UI"")
" + A + @" | get maps | where name == ""Player"" | call AddAction(""Jump"")   name, type? (Button, Value, PassThrough), expectedControlType?, binding?
" + A + @" | get maps | where name == ""Player"" | call AddAction(""Move"", ""Value"", ""Vector2"")
" + A + @" | get actions | where name == ""Jump"" | call AddBinding(""<Keyboard>/space"", ""Keyboard&Mouse"")   path, groups?, interactions?, processors?
" + A + @" | get actions | where name == ""Jump"" | call AddBinding(""<Gamepad>/buttonSouth"", ""Gamepad"", ""hold(duration=0.4)"")
" + A + @" | get actions | where name == ""Move"" | call AddComposite(""2DVector"", {up: ""<Keyboard>/w"", down: ""<Keyboard>/s"", left: ""<Keyboard>/a"", right: ""<Keyboard>/d""}, ""Keyboard&Mouse"")   composite, {part: path}, groups?, interactions?, processors?
" + A + @" | get actions | where name == ""Move"" | call AddBinding(""<Gamepad>/leftStick"", ""Gamepad"", null, ""stickDeadzone"")
" + A + @" | get maps | select name actionCount
" + A + @" | get actions | select map name type expectedControlType interactions bindingCount
" + A + @" | get actions | get bindings | select action index path name groups interactions processors isComposite isPartOfComposite
" + A + @" | get controlSchemes | select name bindingGroup requiredDevices optionalDevices
" + A + @" | get actions | get bindings | where path == ""<Gamepad>/buttonSouth"" | set path = ""<Gamepad>/buttonEast"", interactions = ""tap""
" + A + @" | get actions | get bindings | where isPartOfComposite and name == ""up"" | set path = ""<Keyboard>/upArrow""
" + A + @" | get actions | where name == ""Jump"" | set name = ""Leap"", type = ""PassThrough"", interactions = ""press""
" + A + @" | get actions | where name == ""Move"" | set processors = ""invertVector2"", initialStateCheck = true
" + A + @" | get controlSchemes | where name == ""Keyboard&Mouse"" | set requiredDevices = [""<Keyboard>""], optionalDevices = [""<Mouse>""]
" + A + @" | get controlSchemes | where name == ""Gamepad"" | set name = ""Controller""   also renames the group of its bindings
" + A + @" | get maps | where name == ""Player"" | set name = ""Gameplay""
" + A + @" | get actions | get bindings | where groups == ""Controller"" | call Remove()   a composite root removes its parts
" + A + @" | get actions | where name == ""Leap"" | call Remove()
" + A + @" | get controlSchemes | where name == ""Controller"" | call Remove()
" + A + @" | get maps | where name == ""UI"" | call Remove()
input | select interactions processors   registered names; input | get layouts lists device and control layouts
```

Assets referenced by a scene component work the same way: `objects | get components | where $ is PlayerInput | get actions | get maps`.
Bindings are addressed by id, so removing some bindings in one query does not shift the others. `call` needs rql-mutate.
Created assets are not removed by Undo; undoing an edit rewrites the file.";

        public int Order => 100;

        public void Configure(RqlModuleContext ctx)
        {
            // InputActionAsset and InputActionMap enumerate their actions, but RQL must treat them as objects with members.
            ctx.TreatAsObject(typeof(UnityEngine.InputSystem.InputActionAsset));
            ctx.TreatAsObject(typeof(UnityEngine.InputSystem.InputActionMap));
            ctx.AddSource("input", () => InputEditorApi.Instance,
                   "Input System: assets (.inputactions), layouts, interactions, processors; CreateAsset(path, map?)")
               .AddAdapter(new UnityEngine_InputSystem_InputActionAsset_Adapter())
               .AddGuide(GuideTopic, Guide);
        }
    }
}
