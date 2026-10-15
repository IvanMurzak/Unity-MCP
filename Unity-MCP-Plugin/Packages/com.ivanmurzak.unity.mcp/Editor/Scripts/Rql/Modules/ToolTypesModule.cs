/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using com.IvanMurzak.Unity.MCP.Editor.Rql.Adapters;
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql
{
    /// <summary>Adapters for the types tool methods take (RqlToolBinding): GameObjectRef, script-execute parameters.</summary>
    public sealed class ToolTypesModule : IRqlModule
    {
        public int Order => 10;

        public void Configure(RqlModuleContext ctx)
        {
            ctx.AddAdapter(new AIGD_GameObjectRef_Adapter())
               .AddAdapter(new ReflectorNet_SerializedMemberList_Adapter());
#if UNITY_6000_5_OR_NEWER
            ctx.AddAdapter(new UnityEngine_EntityId_Adapter());
#endif
        }
    }
}
