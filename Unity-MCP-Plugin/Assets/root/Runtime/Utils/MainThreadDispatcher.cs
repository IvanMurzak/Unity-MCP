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
using System.Collections.Concurrent;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Runtime.Utils
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class MainThreadDispatcher : MonoBehaviour
    {
        public static int MainThreadId = Thread.CurrentThread.ManagedThreadId;
        public static bool IsMainThread => Thread.CurrentThread.ManagedThreadId == MainThreadId;

        static readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();
        static MainThreadDispatcher instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            // Save the current main thread ID
            MainThreadId = Thread.CurrentThread.ManagedThreadId;

            if (instance != null)
                return;

            // Only create the dispatcher in Play mode
            if (!Application.isPlaying)
                return;

            var obj = new GameObject("MainThreadDispatcher");
            instance = obj.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(obj);
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void EditorInitialize()
        {
            // Save the current main thread ID
            MainThreadId = Thread.CurrentThread.ManagedThreadId;

            if (instance != null)
                UnityEngine.Object.DestroyImmediate(instance.gameObject);

            UnityEditor.Compilation.CompilationPipeline.compilationFinished += OnCompilationFinished;
        }
        static void OnCompilationFinished(object obj) => Initialize();
#endif

        public static void Enqueue(Action action) => _actions.Enqueue(action);

        void Awake()
        {
            // Save the current main thread ID
            MainThreadId = Thread.CurrentThread.ManagedThreadId;
        }
        void Update()
        {
            while (_actions.TryDequeue(out var action))
                action.Invoke();
        }
    }
}
