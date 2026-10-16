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

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Rql
    {
        public const string QueryToolId = "rql-query";

        [AiTool(QueryToolId, Title = "RQL / Query", Enabled = true, ReadOnlyHint = true, OpenWorldHint = false)]
        [AiSkillDescription(RqlGuide.QueryBlurb)]
        [AiSkillBody(RqlGuide.QueryDescription)]
        [Description(RqlGuide.QueryDescription)]
        public ResponseCallTool Query
        (
            [Description("RQL query, e.g. objects | where $ is ... | select $ref name")]
            string query
        )
        {
            return Run(() => RqlUnity.Engine.Execute(query));
        }
    }
}
