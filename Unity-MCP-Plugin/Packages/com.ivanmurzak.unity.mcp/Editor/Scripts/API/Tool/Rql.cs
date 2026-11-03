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
using System;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Rql;
using Rql;
using Rql.Execution;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    /// <summary>
    /// RQL tools: query and change the live Editor state (scene graph, components, assets, Editor operations) with one
    /// language instead of one tool per operation. Results are compact text tables (fewer tokens than JSON objects).
    /// </summary>
    [AiToolType]
    public partial class Tool_Rql
    {
        /// <summary>
        /// Ends a receipt cut to its first rows. RQL's default points to the json format, which these tools do not offer: the
        /// rows not shown are changes that were made, read back with a query.
        /// </summary>
        internal const string ReceiptMoreRowsNote = "(they were applied too: query the objects to read them)";

        /// <summary>As <see cref="ReceiptMoreRowsNote"/> for a dry run, which changes nothing.</summary>
        internal const string DryRunMoreRowsNote = "(they were checked too)";

        private static ResponseCallTool Run(Func<QueryResult> execute) => MainThread.Instance.Run(() =>
        {
            var result = execute();
            var text = AgentFormat.Render(result, OutputFormat.Table, AgentFormat.DefaultReceiptRows,
                result.DryRun ? DryRunMoreRowsNote : ReceiptMoreRowsNote);
            return result.Ok ? ResponseCallTool.Success(text) : ResponseCallTool.Error(text);
        });
    }
}
