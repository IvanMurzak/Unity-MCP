/*
┌─────────────────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)                        │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-AI-Cinemachine)    │
│  Copyright (c) 2026 Ivan Murzak                                             │
│  Licensed under the MIT License.                                            │
│  See the LICENSE file in the project root for more information.             │
└─────────────────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Rql;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.Cinemachine
{
    using CinemachineBrain = global::Unity.Cinemachine.CinemachineBrain;
    using CinemachineCamera = global::Unity.Cinemachine.CinemachineCamera;
    using NoiseSettings = global::Unity.Cinemachine.NoiseSettings;
    using Stage = global::Unity.Cinemachine.CinemachineCore.Stage;

    /// <summary>
    /// The <c>cinemachine</c> source: the cameras and brains of the loaded scenes, the names the pipeline methods accept, and
    /// the operations without an owner object, e.g. <c>cinemachine | call CreateCamera("FollowCam", "Player")</c>. Operations
    /// on an existing camera are methods of CinemachineCamera (SetBody, SetAim, SetNoise, SetTargets).
    /// </summary>
    [RqlDescription("Cinemachine: cameras, brains, noiseProfiles, bodyTypes / aimTypes / extensionTypes, CreateCamera(…), EnsureBrain(camera?)")]
    public sealed class CinemachineApi
    {
        public const string MainCameraName = "Main Camera";

        public static readonly CinemachineApi Instance = new CinemachineApi();

        [RqlDescription("every CinemachineCamera in the loaded scenes, inactive ones included")]
        public List<CinemachineCamera> cameras => Components<CinemachineCamera>();

        [RqlDescription("every CinemachineBrain in the loaded scenes (the Unity Cameras Cinemachine drives)")]
        public List<CinemachineBrain> brains => Components<CinemachineBrain>();

        [RqlDescription("NoiseSettings assets for SetNoise (Cinemachine presets such as \"6D Shake\", \"Handheld_normal_mild\", and project assets)")]
        public List<NoiseSettings> noiseProfiles => CinemachinePipeline.NoiseProfiles();

        [RqlDescription("component names SetBody accepts (plus \"None\")")]
        public List<string> bodyTypes => CinemachinePipeline.StageTypeNames(Stage.Body);

        [RqlDescription("component names SetAim accepts (plus \"None\")")]
        public List<string> aimTypes => CinemachinePipeline.StageTypeNames(Stage.Aim);

        [RqlDescription("CinemachineExtension components (add with objects | … | call AddComponent(name))")]
        public List<string> extensionTypes => CinemachinePipeline.ExtensionTypeNames();

        [RqlCallable("create a GameObject with a CinemachineCamera (follow / lookAt: a GameObject name or ref; position and eulerAngles in world " +
                     "space; priority) and return the camera; makes sure the main Camera has a CinemachineBrain (creating a \"Main Camera\" if " +
                     "the scene has none). Add a Body / Aim afterwards with SetBody / SetAim. Undoable")]
        public CinemachineCamera CreateCamera(string name = "CinemachineCamera", object? follow = null, object? lookAt = null,
            Vector3? position = null, Vector3? eulerAngles = null, int priority = 0)
        {
            var followTarget = CinemachinePipeline.ResolveTarget(follow, nameof(follow));
            var lookAtTarget = CinemachinePipeline.ResolveTarget(lookAt, nameof(lookAt));

            var go = new GameObject(string.IsNullOrEmpty(name) ? "CinemachineCamera" : name);
            go.transform.SetPositionAndRotation(position ?? Vector3.zero, Quaternion.Euler(eulerAngles ?? Vector3.zero));
            Undo.RegisterCreatedObjectUndo(go, "RQL create CinemachineCamera " + go.name);
            var camera = Undo.AddComponent<CinemachineCamera>(go);
            if (priority != 0) camera.Priority.Value = priority;
            if (followTarget != null) camera.Follow = followTarget;
            if (lookAtTarget != null) camera.LookAt = lookAtTarget;

            EnsureBrain();
            MarkSceneDirty(go);
            return camera;
        }

        [RqlCallable("make sure a Camera has a CinemachineBrain (camera: a GameObject name or ref; default: the main Camera, created as " +
                     "\"Main Camera\" when the scene has none) and return the brain. Undoable")]
        public CinemachineBrain EnsureBrain(object? camera = null)
        {
            Camera unityCamera;
            if (camera != null && !(camera is string s && s.Length == 0))
            {
                var target = CinemachinePipeline.ResolveTarget(camera, nameof(camera))!;
                unityCamera = target.GetComponent<Camera>();
                if (unityCamera == null) throw new ArgumentException($"camera: GameObject '{target.name}' has no Camera");
            }
            else unityCamera = Camera.main != null ? Camera.main : CreateMainCamera();

            var brain = unityCamera.GetComponent<CinemachineBrain>();
            if (brain != null) return brain;
            brain = Undo.AddComponent<CinemachineBrain>(unityCamera.gameObject);
            MarkSceneDirty(unityCamera.gameObject);
            return brain;
        }

        private static Camera CreateMainCamera()
        {
            var go = new GameObject(MainCameraName) { tag = "MainCamera" };
            go.transform.position = new Vector3(0f, 1f, -10f); // where Unity's default scene puts it
            Undo.RegisterCreatedObjectUndo(go, "RQL create Main Camera");
            Undo.AddComponent<AudioListener>(go);
            return Undo.AddComponent<Camera>(go);
        }

        private static List<T> Components<T>() where T : Component =>
            RqlUnity.RootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToList();

        private static void MarkSceneDirty(GameObject go)
        {
            if (!EditorApplication.isPlaying && go.scene.IsValid()) EditorSceneManager.MarkSceneDirty(go.scene);
        }
    }
}
