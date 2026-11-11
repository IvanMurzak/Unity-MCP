/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#if UNITY_EDITOR
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
    /// <summary>
    /// A modal window (a standard Win32 dialog: <c>EditorUtility.DisplayDialog</c>, "Enter Safe Mode?", the API
    /// Updater consent, the package-signature prompt) that holds the editor's main thread in its own message loop, so
    /// no work posted to the main thread runs until a person answers it.
    /// </summary>
    public sealed class EditorModalWindow
    {
        public EditorModalWindow(string title, string message, IReadOnlyList<string> buttons)
        {
            Title = title;
            Message = message;
            Buttons = buttons;
        }

        public string Title { get; }
        /// <summary>The dialog's text lines (its static labels).</summary>
        public string Message { get; }
        /// <summary>Button labels, without the <c>&amp;</c> mnemonic marker.</summary>
        public IReadOnlyList<string> Buttons { get; }

        const int MaxMessageLength = 300;

        /// <summary><c>"Title": message [A | B]</c>, the format <c>unity-mcp-cli</c> prints for a blocking dialog.</summary>
        public string Describe()
        {
            var message = string.Join(" ", Message.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (message.Length > MaxMessageLength)
                message = message.Substring(0, MaxMessageLength) + "...";
            var text = string.Join(": ", new[] { Title.Length > 0 ? $"\"{Title}\"" : string.Empty, message }.Where(s => s.Length > 0));
            return $"{text}{(text.Length > 0 ? " " : string.Empty)}[{string.Join(" | ", Buttons)}]";
        }

        /// <summary>
        /// The error an MCP call gets while the window blocks the main thread. <paramref name="started"/>: the call's
        /// main-thread work was already running (it may have opened the window itself), so it finishes once the window
        /// is answered; otherwise it was not run at all.
        /// </summary>
        public string BlockedCallMessage(bool started)
            => $"The Unity Editor is blocked by a modal window: {Describe()}. " +
               "No tool can run until a person answers it in the Unity Editor: ask the user to answer it there, and do not " +
               "press its buttons on their behalf (it can be a security confirmation such as \"Import Anyway\"). " +
               (started
                   ? "This call was already running on the Editor's main thread (it may have opened the window); it continues once the window is answered, but its result is not returned."
                   // Not "this call was not run": the MCP server reads that phrase as a plugin-reload refusal.
                   : "The call did not run: call it again once the window is answered.");

        /// <summary>
        /// The first visible standard dialog owned by the native thread <paramref name="nativeThreadId"/> (the editor's
        /// main thread), or null when there is none or the platform is not Windows. Safe to call from any thread: it
        /// sends no message to the (blocked) owner thread.
        /// </summary>
        public static EditorModalWindow? Find(uint nativeThreadId)
        {
            if (nativeThreadId == 0 || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return null;
            try
            {
                return Win32.FindDialog(nativeThreadId);
            }
            catch (Exception)
            {
                return null; // user32 unavailable or a window closed mid-scan: no verdict
            }
        }

        /// <summary>The calling thread's native id on Windows (0 elsewhere): pass the main thread's to <see cref="Find"/>.</summary>
        public static uint CurrentNativeThreadId()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return 0;
            try
            {
                return Win32.GetCurrentThreadId();
            }
            catch (Exception)
            {
                return 0;
            }
        }

        static class Win32
        {
            delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
            [DllImport("user32.dll")] static extern bool EnumThreadWindows(uint threadId, EnumWindowsProc callback, IntPtr lParam);
            [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc callback, IntPtr lParam);
            [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
            [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassNameW(IntPtr hWnd, StringBuilder text, int count);
            // Reads the text without sending WM_GETTEXT, which would wait on the window's owner: the blocked main thread.
            [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int InternalGetWindowText(IntPtr hWnd, StringBuilder text, int count);

            const string DialogClass = "#32770";

            static string ClassName(IntPtr hWnd)
            {
                var sb = new StringBuilder(256);
                GetClassNameW(hWnd, sb, sb.Capacity);
                return sb.ToString();
            }

            static string Text(IntPtr hWnd)
            {
                var sb = new StringBuilder(1024);
                InternalGetWindowText(hWnd, sb, sb.Capacity);
                return sb.ToString();
            }

            public static EditorModalWindow? FindDialog(uint threadId)
            {
                var dialog = IntPtr.Zero;
                EnumThreadWindows(threadId, (hWnd, _) =>
                {
                    if (!IsWindowVisible(hWnd) || ClassName(hWnd) != DialogClass)
                        return true;
                    dialog = hWnd;
                    return false;
                }, IntPtr.Zero);
                if (dialog == IntPtr.Zero)
                    return null;

                var texts = new List<string>();
                var buttons = new List<string>();
                EnumChildWindows(dialog, (child, _) =>
                {
                    if (!IsWindowVisible(child))
                        return true;
                    var text = Text(child);
                    if (text.Length == 0)
                        return true;
                    var cls = ClassName(child);
                    if (cls == "Button")
                        buttons.Add(text.Replace("&", string.Empty));
                    else if (cls == "Static")
                        texts.Add(text);
                    return true;
                }, IntPtr.Zero);
                return new EditorModalWindow(Text(dialog), string.Join("\n", texts), buttons);
            }
        }
    }
}
#endif
