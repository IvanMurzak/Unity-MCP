// The C# snippets `compileProject` runs through `script-execute`, and the parser of their answer. They use only public
// editor API plus reflection on the Console's `LogEntries` (internal, stable since Unity 2017), so any plugin version
// that has `script-execute` serves them.

import { parseCompilerMessages, type CompilerMessage } from '../utils/compiler-messages.js';

/** Import changed assets; changed scripts compile. The result says whether a compile started. */
export const REFRESH_SNIPPET = `UnityEditor.AssetDatabase.Refresh();
UnityEditor.EditorApplication.isCompiling`;

export type CompileState = 'busy' | 'ok' | 'failed';

export interface CompileStatus {
  state: CompileState;
  messages: CompilerMessage[];
}

/**
 * The compile state (`state:busy|ok|failed`, first line) and, once settled, the Console's compiler messages (one per
 * line). Only lines that look like compiler diagnostics leave the editor, so a full Console stays a small answer.
 */
export function compileStatusSnippet(): string {
  return `var busy = UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating;
var sb = new System.Text.StringBuilder();
sb.AppendLine(busy ? "state:busy" : (UnityEditor.EditorUtility.scriptCompilationFailed ? "state:failed" : "state:ok"));
var editorAssembly = typeof(UnityEditor.Editor).Assembly;
var entries = editorAssembly.GetType("UnityEditor.LogEntries");
var entryType = editorAssembly.GetType("UnityEditor.LogEntry");
if (!busy && entries != null && entryType != null)
{
    var staticFlags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
    var instanceFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
    var start = entries.GetMethod("StartGettingEntries", staticFlags);
    var end = entries.GetMethod("EndGettingEntries", staticFlags);
    var get = entries.GetMethod("GetEntryInternal", staticFlags);
    var messageField = entryType.GetField("message", instanceFlags);
    if (start != null && end != null && get != null && messageField != null)
    {
        var count = (int)start.Invoke(null, null);
        try
        {
            var entry = System.Activator.CreateInstance(entryType);
            for (var i = 0; i < count; i++)
            {
                get.Invoke(null, new object[] { i, entry });
                var text = messageField.GetValue(entry) as string;
                if (text == null) continue;
                var line = text.Split('\\n')[0].Trim();
                if (line.Contains(": error CS") || line.Contains(": warning CS")) sb.AppendLine(line);
            }
        }
        finally { end.Invoke(null, null); }
    }
}
sb.ToString()`;
}

/**
 * The string a snippet returned. `script-execute` answers a value as `<type>: <json>` (`System.String: "state:ok\r\n"`);
 * a bare string is taken as is.
 */
export function unwrapStringResult(text: string): string {
  const m = /^System\.String:\s*("[\s\S]*")\s*$/.exec(text.trim());
  if (!m) return text;
  try {
    const value: unknown = JSON.parse(m[1]);
    return typeof value === 'string' ? value : text;
  } catch {
    return text;
  }
}

/** Parse the status snippet's answer; undefined when it is not one. */
export function parseCompileStatus(text: string): CompileStatus | undefined {
  const lines = unwrapStringResult(text).split(/\r?\n/);
  const m = /^state:(busy|ok|failed)$/.exec(lines[0]?.trim() ?? '');
  if (!m) return undefined;
  return { state: m[1] as CompileState, messages: parseCompilerMessages(lines.slice(1).join('\n')) };
}
