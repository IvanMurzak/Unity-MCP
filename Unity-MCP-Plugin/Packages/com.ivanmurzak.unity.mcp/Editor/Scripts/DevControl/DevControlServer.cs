/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.UI;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.DevControl
{
    /// <summary>
    /// DEV-ONLY inject/control HTTP bridge for the "Game Developer" (AI Game Developer) editor window. A
    /// <see cref="HttpListener"/> bound to <c>http://127.0.0.1:&lt;port&gt;/</c> (loopback ONLY) whose
    /// accept loop runs on a background thread; it parses JSON in/out (<see cref="System.Text.Json"/>),
    /// routes via the pure-managed <see cref="DevControlRouter"/>, and hops every window-touching handler
    /// onto the editor main thread (<see cref="MainThread"/> — the Unity editor impl dispatches via
    /// <c>EditorApplication.update</c>), because ALL Unity API / UI Toolkit access must happen there.
    ///
    /// <para>
    /// SECURITY: the security boundary IS this server. It binds 127.0.0.1 only (never a routable
    /// interface) and is started ONLY when <c>UNITY_MCP_DEV_CONTROL=1</c> (see
    /// <c>Startup.StartDevControlIfEnabled</c>), so a shipped plugin never listens. Editor-only (lives
    /// under <c>Editor/</c>); disposed on domain reload (<c>AssemblyReloadEvents.beforeAssemblyReload</c>)
    /// and on editor quit.
    /// </para>
    /// </summary>
    public sealed class DevControlServer : IDisposable
    {
        /// <summary>Default port when <c>UNITY_MCP_DEV_CONTROL_PORT</c> is unset (matches infra worktree.py).</summary>
        public const int DefaultPort = 9922;

        /// <summary>Loopback-only bind host — never a routable interface (the security boundary).</summary>
        const string BindHost = "127.0.0.1";

        readonly int _port;
        readonly HttpListener _listener = new HttpListener();
        readonly CancellationTokenSource _cts = new CancellationTokenSource();
        Thread? _acceptThread;
        bool _disposed;

        /// <summary>The base URL this server listens on (after <see cref="Start"/>).</summary>
        public string BaseUrl => $"http://{BindHost}:{_port}/";

        /// <summary>
        /// Construct the bridge listening on <paramref name="port"/> (loopback only). Call
        /// <see cref="Start"/> to begin accepting; the ctor does not open the socket.
        /// </summary>
        public DevControlServer(int port)
        {
            _port = port;
        }

        /// <summary>
        /// Open the loopback listener and start the background accept loop. Logs
        /// <c>[dev-control] Listening on http://127.0.0.1:&lt;port&gt;</c> on success; a bind failure
        /// (port in use) is logged as an error and leaves the server inert rather than throwing into
        /// editor boot.
        /// </summary>
        public void Start()
        {
            try
            {
                _listener.Prefixes.Add(BaseUrl);
                _listener.Start();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[dev-control] failed to bind {BaseUrl}: {ex.Message}");
                return;
            }

            _acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "unity-mcp-dev-control",
            };
            _acceptThread.Start();

            Debug.Log($"[dev-control] Listening on http://{BindHost}:{_port}");
        }

        void AcceptLoop()
        {
            while (!_cts.IsCancellationRequested && _listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = _listener.GetContext(); // blocks until a request or the listener is stopped
                }
                catch (Exception)
                {
                    // Listener stopped/disposed (teardown) or a transient accept error — exit on
                    // cancellation, otherwise keep accepting. Never let one bad accept kill the server.
                    if (_cts.IsCancellationRequested || !_listener.IsListening)
                        break;
                    continue;
                }

                try
                {
                    HandleRequest(context);
                }
                catch (Exception ex)
                {
                    // A handler-level failure must never kill the accept loop: answer 500 and keep serving.
                    TryWrite(context, 500, $"{{\"ok\":false,\"error\":{JsonString(ex.Message)}}}");
                }
            }
        }

        void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var method = request.HttpMethod ?? string.Empty;
            // Strip query string + a trailing slash so the router matches the canonical path.
            var path = (request.Url?.AbsolutePath ?? string.Empty).TrimEnd('/');
            if (path.Length == 0)
                path = "/";

            var command = DevControlRouter.Route(method, path);
            if (command == DevControlRouter.Command.Unknown)
            {
                TryWrite(context, 404, $"{{\"ok\":false,\"error\":\"no route for {method} {JsonInner(path)}\"}}");
                return;
            }

            if (command == DevControlRouter.Command.Health)
            {
                var (present, state) = RunOnMainThread(() =>
                {
                    var win = MainWindowEditor.DevFindOpenInstance();
                    return (win != null, win?.DevStateJson() ?? "null");
                });
                TryWrite(context, 200, $"{{\"ok\":true,\"windowPresent\":{(present ? "true" : "false")},\"state\":{state}}}");
                return;
            }

            if (command == DevControlRouter.Command.State)
            {
                var (present, state) = RunOnMainThread(() =>
                {
                    var win = MainWindowEditor.DevFindOpenInstance();
                    return (win != null, win?.DevStateJson() ?? "null");
                });
                if (!present)
                {
                    TryWrite(context, 409, "{\"ok\":false,\"error\":\"window not open\"}");
                    return;
                }
                TryWrite(context, 200, $"{{\"ok\":true,\"window\":{state}}}");
                return;
            }

            // The remaining commands read a JSON body.
            var body = ReadBody(request);
            using var doc = ParseJson(body);
            var root = doc?.RootElement;

            switch (command)
            {
                case DevControlRouter.Command.InjectConnectionStatus:
                {
                    var value = GetString(root, "status");
                    if (!DevControlRouter.TryParseConnectionStatus(value, out var status))
                    {
                        TryWrite(context, 400, $"{{\"ok\":false,\"error\":\"invalid connection-status {JsonInner(value)}\"}}");
                        break;
                    }
                    var ok = RunOnMainThread(() =>
                    {
                        var win = MainWindowEditor.DevFindOpenInstance();
                        if (win == null) return false;
                        win.DevInjectConnectionStatus(status);
                        return true;
                    });
                    WriteWindowResult(context, ok);
                    break;
                }
                case DevControlRouter.Command.InjectServerStatus:
                {
                    var value = GetString(root, "status");
                    if (!DevControlRouter.TryParseServerStatus(value, out var status))
                    {
                        TryWrite(context, 400, $"{{\"ok\":false,\"error\":\"invalid server-status {JsonInner(value)}\"}}");
                        break;
                    }
                    var ok = RunOnMainThread(() =>
                    {
                        var win = MainWindowEditor.DevFindOpenInstance();
                        if (win == null) return false;
                        win.DevInjectServerStatus(status);
                        return true;
                    });
                    WriteWindowResult(context, ok);
                    break;
                }
                case DevControlRouter.Command.ControlServerUrl:
                {
                    var url = GetString(root, "url");
                    var ok = RunOnMainThread(() =>
                    {
                        var win = MainWindowEditor.DevFindOpenInstance();
                        if (win == null) return false;
                        win.DevSetServerUrl(url ?? string.Empty);
                        return true;
                    });
                    WriteWindowResult(context, ok);
                    break;
                }
                case DevControlRouter.Command.ControlSelectAgent:
                {
                    // Accept either {agent} or {agentId} (the spec uses both across endpoint docs).
                    var agent = GetString(root, "agent") ?? GetString(root, "agentId");
                    var (windowPresent, selected) = RunOnMainThread(() =>
                    {
                        var win = MainWindowEditor.DevFindOpenInstance();
                        if (win == null) return (false, false);
                        return (true, win.DevSelectAgent(agent ?? string.Empty));
                    });
                    if (!windowPresent)
                    {
                        TryWrite(context, 409, "{\"ok\":false,\"error\":\"window not open\"}");
                        break;
                    }
                    TryWrite(context, selected ? 200 : 404,
                        selected ? "{\"ok\":true}" : $"{{\"ok\":false,\"error\":\"unknown agent {JsonInner(agent)}\"}}");
                    break;
                }
                case DevControlRouter.Command.ControlClick:
                {
                    var target = GetString(root, "target");
                    if (!DevControlRouter.TryNormalizeClickTarget(target, out var normalized))
                    {
                        TryWrite(context, 400, $"{{\"ok\":false,\"error\":\"invalid click target {JsonInner(target)}\"}}");
                        break;
                    }
                    var (windowPresent, clicked) = RunOnMainThread(() =>
                    {
                        var win = MainWindowEditor.DevFindOpenInstance();
                        if (win == null) return (false, false);
                        return (true, win.DevClick(normalized));
                    });
                    if (!windowPresent)
                    {
                        TryWrite(context, 409, "{\"ok\":false,\"error\":\"window not open\"}");
                        break;
                    }
                    TryWrite(context, clicked ? 200 : 409,
                        clicked ? "{\"ok\":true}" : $"{{\"ok\":false,\"error\":\"target not clickable {JsonInner(target)}\"}}");
                    break;
                }
                default:
                    TryWrite(context, 404, "{\"ok\":false,\"error\":\"unhandled command\"}");
                    break;
            }
        }

        void WriteWindowResult(HttpListenerContext context, bool windowPresent)
            => TryWrite(context, windowPresent ? 200 : 409,
                windowPresent ? "{\"ok\":true}" : "{\"ok\":false,\"error\":\"window not open\"}");

        /// <summary>
        /// Run <paramref name="work"/> on the editor main thread and return its result, marshalling any
        /// exception back to this background thread. <see cref="MainThread.Run{T}(System.Func{T})"/>
        /// dispatches onto <c>EditorApplication.update</c> in the Unity editor impl and blocks until the
        /// result is available (or runs inline when already on the main thread).
        /// </summary>
        static T RunOnMainThread<T>(Func<T> work) => MainThread.Instance.Run(work);

        static string ReadBody(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
                return string.Empty;
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8);
            return reader.ReadToEnd();
        }

        static JsonDocument? ParseJson(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;
            try { return JsonDocument.Parse(body); }
            catch { return null; }
        }

        static string? GetString(JsonElement? root, string property)
        {
            if (root is not { ValueKind: JsonValueKind.Object } obj)
                return null;
            if (!obj.TryGetProperty(property, out var el))
                return null;
            return el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
        }

        static void TryWrite(HttpListenerContext context, int statusCode, string json)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.OutputStream.Close();
            }
            catch
            {
                // Client hung up mid-write — nothing actionable; the accept loop keeps serving.
            }
        }

        /// <summary>JSON-encode a string AS a complete JSON string literal (with surrounding quotes).</summary>
        // Fully-qualified: `using com.IvanMurzak.ReflectorNet.Utils` also defines a JsonSerializer (CS0104 ambiguity).
        static string JsonString(string? value) => System.Text.Json.JsonSerializer.Serialize(value ?? string.Empty);

        /// <summary>JSON-escape a string WITHOUT surrounding quotes (for embedding inside a larger literal).</summary>
        static string JsonInner(string? value)
        {
            var s = JsonString(value);
            return s.Length >= 2 ? s.Substring(1, s.Length - 2) : s;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            try { _cts.Cancel(); } catch { /* already disposed */ }

            // Stop unblocks the GetContext() call in the accept loop.
            try { if (_listener.IsListening) _listener.Stop(); } catch { }
            try { _listener.Close(); } catch { }

            try { _acceptThread?.Join(TimeSpan.FromSeconds(2)); } catch { }

            _cts.Dispose();
        }
    }
}
