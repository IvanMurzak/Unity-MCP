using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity MCP (Model Context Protocol) Server
/// Allows GitHub Copilot to interact with Unity Editor remotely
/// This runs in the background while Unity Editor is open
/// </summary>
public class UnityMCPServer : EditorWindow
{
    private static TcpListener listener;
    private static Thread serverThread;
    private static bool isServerRunning = false;
    private static int serverPort = 8080;
    private static List<string> logs = new List<string>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/Unity MCP/Start Server")]
    public static void StartServer()
    {
        if (isServerRunning)
        {
            Debug.LogWarning("MCP Server is already running!");
            return;
        }

        try
        {
            serverThread = new Thread(new ThreadStart(ServerLoop));
            serverThread.IsBackground = true;
            serverThread.Start();

            Log("Unity MCP Server started on port " + serverPort);
            Debug.Log("✅ Unity MCP Server started successfully on port " + serverPort);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start MCP Server: " + e.Message);
        }
    }

    [MenuItem("Tools/Unity MCP/Stop Server")]
    public static void StopServer()
    {
        if (!isServerRunning)
        {
            Debug.LogWarning("MCP Server is not running!");
            return;
        }

        try
        {
            isServerRunning = false;

            if (listener != null)
            {
                listener.Stop();
            }

            if (serverThread != null && serverThread.IsAlive)
            {
                serverThread.Abort();
            }

            Log("Unity MCP Server stopped");
            Debug.Log("🛑 Unity MCP Server stopped");
        }
        catch (Exception e)
        {
            Debug.LogError("Error stopping server: " + e.Message);
        }
    }

    [MenuItem("Tools/Unity MCP/Show Dashboard")]
    public static void ShowWindow()
    {
        GetWindow<UnityMCPServer>("MCP Server");
    }

    private void OnGUI()
    {
        GUILayout.Label("Unity MCP Server Dashboard", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Status:", isServerRunning ? "🟢 Running" : "🔴 Stopped");
        EditorGUILayout.LabelField("Port:", serverPort.ToString());

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(isServerRunning ? "Stop Server" : "Start Server"))
        {
            if (isServerRunning)
                StopServer();
            else
                StartServer();
        }

        if (GUILayout.Button("Clear Logs"))
        {
            logs.Clear();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Logs:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        foreach (string log in logs)
        {
            EditorGUILayout.LabelField(log, EditorStyles.wordWrappedLabel);
        }
        EditorGUILayout.EndScrollView();
    }

    private static void ServerLoop()
    {
        try
        {
            listener = new TcpListener(IPAddress.Any, serverPort);
            listener.Start();
            isServerRunning = true;

            Log("Server listening on port " + serverPort);

            while (isServerRunning)
            {
                if (listener.Pending())
                {
                    TcpClient client = listener.AcceptTcpClient();
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }
        catch (ThreadAbortException)
        {
            // Expected when stopping server
        }
        catch (Exception e)
        {
            Log("Server error: " + e.Message);
        }
        finally
        {
            if (listener != null)
            {
                listener.Stop();
            }
        }
    }

    private static void HandleClient(TcpClient client)
    {
        try
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[4096];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            if (bytesRead > 0)
            {
                string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Log("Received: " + request);

                string response = ProcessCommand(request);

                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                stream.Write(responseBytes, 0, responseBytes.Length);
            }

            client.Close();
        }
        catch (Exception e)
        {
            Log("Client handler error: " + e.Message);
        }
    }

    private static string ProcessCommand(string command)
    {
        try
        {
            command = command.Trim();

            // Simple command protocol: COMMAND:ARGS
            string[] parts = command.Split(':');
            string cmd = parts[0].ToUpper();
            string args = parts.Length > 1 ? parts[1] : "";

            switch (cmd)
            {
                case "PING":
                    return "PONG";

                case "STATUS":
                    return GetUnityStatus();

                case "CREATE":
                    return ExecuteOnMainThread(() => CreateGameObject(args));

                case "SCENE":
                    return GetCurrentSceneInfo();

                case "COMPILE":
                    return ExecuteOnMainThread(() => TriggerCompilation());

                case "PLAY":
                    return ExecuteOnMainThread(() => EnterPlayMode());

                case "STOP":
                    return ExecuteOnMainThread(() => ExitPlayMode());

                default:
                    return "ERROR:Unknown command: " + cmd;
            }
        }
        catch (Exception e)
        {
            return "ERROR:" + e.Message;
        }
    }

    private static string GetUnityStatus()
    {
        return string.Format("OK:Unity {0} | Project: {1} | Playing: {2}",
            Application.unityVersion,
            Application.productName,
            EditorApplication.isPlaying);
    }

    private static string CreateGameObject(string name)
    {
        if (string.IsNullOrEmpty(name))
            name = "NewGameObject";

        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);

        return "OK:Created GameObject: " + name;
    }

    private static string GetCurrentSceneInfo()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        int objectCount = scene.rootCount;

        return string.Format("OK:Scene: {0} | Objects: {1} | Path: {2}",
            scene.name, objectCount, scene.path);
    }

    private static string TriggerCompilation()
    {
        AssetDatabase.Refresh();
        return "OK:Compilation triggered";
    }

    private static string EnterPlayMode()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = true;
            return "OK:Entering Play Mode";
        }
        return "OK:Already in Play Mode";
    }

    private static string ExitPlayMode()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return "OK:Exiting Play Mode";
        }
        return "OK:Already stopped";
    }

    // Execute commands on Unity's main thread (required for most Unity APIs)
    private static string ExecuteOnMainThread(Func<string> action)
    {
        string result = "ERROR:Timeout";
        bool completed = false;

        EditorApplication.delayCall += () =>
        {
            try
            {
                result = action();
            }
            catch (Exception e)
            {
                result = "ERROR:" + e.Message;
            }
            finally
            {
                completed = true;
            }
        };

        // Wait for completion (with timeout)
        int timeout = 5000; // 5 seconds
        int waited = 0;
        while (!completed && waited < timeout)
        {
            Thread.Sleep(100);
            waited += 100;
        }

        return result;
    }

    private static void Log(string message)
    {
        string timestampedMessage = string.Format("[{0}] {1}",
            DateTime.Now.ToString("HH:mm:ss"), message);

        logs.Add(timestampedMessage);

        // Keep only last 100 logs
        if (logs.Count > 100)
        {
            logs.RemoveAt(0);
        }
    }

    // Auto-start on Unity launch (optional)
    [InitializeOnLoadMethod]
    private static void AutoStart()
    {
        // Uncomment to auto-start server when Unity opens
        // StartServer();
    }

    // Cleanup on Unity close
    private void OnDestroy()
    {
        if (isServerRunning)
        {
            StopServer();
        }
    }
}

/// <summary>
/// Example client code for testing the MCP server
/// Usage: Send TCP messages to localhost:8080
/// Examples:
///   - PING
///   - STATUS
///   - CREATE:MyGameObject
///   - SCENE
///   - COMPILE
///   - PLAY
///   - STOP
/// </summary>
public static class UnityMCPClient
{
    public static string SendCommand(string host, int port, string command)
    {
        try
        {
            using (TcpClient client = new TcpClient(host, port))
            {
                NetworkStream stream = client.GetStream();

                byte[] data = Encoding.UTF8.GetBytes(command);
                stream.Write(data, 0, data.Length);

                byte[] buffer = new byte[4096];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
        }
        catch (Exception e)
        {
            return "ERROR:" + e.Message;
        }
    }

    // Test method accessible from Unity console or another script
    [MenuItem("Tools/Unity MCP/Test Connection")]
    public static void TestConnection()
    {
        string response = SendCommand("localhost", 8080, "PING");
        Debug.Log("Server response: " + response);
    }
}