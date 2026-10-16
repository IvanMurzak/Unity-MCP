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
using System.ComponentModel;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using Rql;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Rql
    {
        public const string MutateToolId = "rql-mutate";

        [AiTool(MutateToolId, Title = "RQL / Mutate", Enabled = true, DestructiveHint = true, OpenWorldHint = false)]
        [AiSkillDescription(RqlGuide.MutateBlurb)]
        [AiSkillBody(RqlGuide.MutateDescription)]
        [Description(RqlGuide.MutateDescription)]
        public ResponseCallTool Mutate
        (
            [Description("RQL query ending with a set or call stage, e.g. objects | where name == \"Player\" | set active = false")]
            string query,
            [Description("true = preview the receipt without changing anything")]
            bool dry_run = false
        )
        {
            return Run(() => RqlUnity.Mutate(query, dry_run));
        }
    }
}
