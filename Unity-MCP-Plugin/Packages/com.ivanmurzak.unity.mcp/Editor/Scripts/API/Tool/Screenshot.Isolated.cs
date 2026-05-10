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
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.McpPlugin.Common.Model;
using com.IvanMurzak.ReflectorNet.Utils;
using AIGD;
using com.IvanMurzak.Unity.MCP.Runtime.Extensions;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    public partial class Tool_Screenshot
    {
        public const string ScreenshotIsolatedToolId = "screenshot-isolated";

        // Layer 31 is Unity's last user-defined layer; we reserve it transiently for
        // isolation rendering. The layer swap is restored in finally before any other
        // code observes the scene, so the choice is invisible to the user.
        private const int IsolationLayer = 31;
        private const int MinResolution = 1;
        private const int MaxResolution = 8192;
        private const float MinFieldOfView = 1f;
        private const float MaxFieldOfView = 179f;
        private const float MinPadding = 0.01f;
        private const float MaxPadding = 100f;
        private const float MaxNearClip = 1000f;
        private const float MaxFarClip = 1e6f;

        public enum CameraView
        {
            [Description("Camera faces the object's forward side (-Z). Standard front view.")]
            Front,
            [Description("Camera faces the object's rear side (+Z).")]
            Back,
            [Description("Camera faces the object's left side (-X).")]
            Left,
            [Description("Camera faces the object's right side (+X).")]
            Right,
            [Description("Camera looks down at the object from above (+Y).")]
            Top,
            [Description("Camera looks up at the object from below (-Y).")]
            Bottom,
            [Description("Produces a single 2x2 grid image with Front, Right, Back, and Top views. "
                       + "Each sub-view uses the specified resolution, so final image is resolution*2 x resolution*2.")]
            Composite
        }

        public enum BackgroundMode
        {
            [Description("Flat color defined by backgroundColor. Camera clearFlags = SolidColor.")]
            SolidColor,
            [Description("Uses the scene's current skybox material. Camera clearFlags = Skybox.")]
            Skybox,
            [Description("Alpha-zero background for compositing. RenderTexture format must support alpha (ARGB32).")]
            Transparent
        }

        public class IsolatedLightConfig
        {
            [JsonPropertyName("type")] public string? Type { get; set; }
            [JsonPropertyName("color")] public string? Color { get; set; }
            [JsonPropertyName("intensity")] public float? Intensity { get; set; }
            [JsonPropertyName("rotation")] public float[]? Rotation { get; set; }
            [JsonPropertyName("position")] public float[]? Position { get; set; }
            [JsonPropertyName("range")] public float? Range { get; set; }
            [JsonPropertyName("spotAngle")] public float? SpotAngle { get; set; }
            [JsonPropertyName("innerSpotAngle")] public float? InnerSpotAngle { get; set; }
            [JsonPropertyName("shadows")] public string? Shadows { get; set; }
            [JsonPropertyName("shadowStrength")] public float? ShadowStrength { get; set; }
            [JsonPropertyName("bounceIntensity")] public float? BounceIntensity { get; set; }
            [JsonPropertyName("colorTemperature")] public float? ColorTemperature { get; set; }
            [JsonPropertyName("cookieSize")] public float? CookieSize { get; set; }
            [JsonPropertyName("cullingMask")] public int? CullingMask { get; set; }
            [JsonPropertyName("renderMode")] public string? RenderMode { get; set; }
        }

        [McpPluginTool
        (
            ScreenshotIsolatedToolId,
            Title = "Screenshot / Isolated GameObject",
            ReadOnlyHint = true,
            IdempotentHint = true,
            Enabled = true
        )]
        [Description("Renders a screenshot of a target GameObject with configurable isolation, background, "
                   + "camera angle, and lighting. When isolated=true (default), only the target object is "
                   + "visible via layer-based culling and inactive children of the target are temporarily "
                   + "activated for the render (their OnEnable callbacks may fire — restored in finally, but "
                   + "side effects like audio/network/animation events are not undoable). When isolated=false, "
                   + "the existing scene state is rendered as-is without activating inactive objects. Supports "
                   + "custom multi-light setups via JSON. Returns a base64-encoded PNG.")]
        public ResponseCallTool ScreenshotIsolated
        (
            [Description("Reference to the target GameObject (by instanceId, path, or name).")]
            GameObjectRef? gameObjectRef,
            [Description("Include child GameObjects in the render. Default: true.")]
            bool? includeChildren = true,
            [Description("When true, renders only the target object using layer-based culling. "
                       + "When false, renders the full scene from the computed camera position. Default: true.")]
            bool? isolated = true,
            [Description("Background mode. Default: SolidColor.")]
            BackgroundMode? backgroundMode = BackgroundMode.SolidColor,
            [Description("Hex background color (e.g. '#404040'). Only used when backgroundMode is SolidColor.")]
            string? backgroundColor = "#404040",
            [Description("Camera angle relative to the target object's bounding box. Default: Front.")]
            CameraView? cameraView = CameraView.Front,
            [Description("Camera vertical field of view in degrees. Default: 60.")]
            float? fieldOfView = 60f,
            [Description("Camera near clip plane distance. Default: 0.01.")]
            float? nearClipPlane = 0.01f,
            [Description("Camera far clip plane distance. Default: 1000.")]
            float? farClipPlane = 1000f,
            [Description("Framing multiplier around the object. 1.0 = tight fit, 1.5 = 50% extra space. Default: 1.2.")]
            float? padding = 1.2f,
            [Description("JSON array of light configurations. Each object defines type, color, intensity, "
                       + "rotation, position, range, spotAngle, shadows, etc. "
                       + "When null, a default white directional light at rotation (50,-30,0) is used. "
                       + "Example: [{\"type\":\"Directional\",\"color\":\"#FFF4E5\",\"intensity\":1.2,\"rotation\":[45,-45,0]}]")]
            string? lights = null,
            [Description("Output image resolution in pixels (width = height). Default: 512.")]
            int? resolution = 512
        )
        {
            var resolvedResolution = resolution ?? 512;
            if (resolvedResolution < MinResolution || resolvedResolution > MaxResolution)
                return ResponseCallTool.Error(
                    $"Resolution must be between {MinResolution} and {MaxResolution} pixels. Got {resolvedResolution}.");

            if (gameObjectRef == null)
                return ResponseCallTool.Error("[Error] gameObjectRef is required.");

            var resolvedIncludeChildren = includeChildren ?? true;
            var resolvedIsolated = isolated ?? true;
            var resolvedBackgroundMode = backgroundMode ?? BackgroundMode.SolidColor;
            var resolvedBackgroundColor = backgroundColor ?? "#404040";
            var resolvedCameraView = cameraView ?? CameraView.Front;
            var resolvedFov = fieldOfView ?? 60f;
            var resolvedNear = nearClipPlane ?? 0.01f;
            var resolvedFar = farClipPlane ?? 1000f;
            var resolvedPadding = padding ?? 1.2f;

            if (!float.IsFinite(resolvedFov) || resolvedFov < MinFieldOfView || resolvedFov > MaxFieldOfView)
                return ResponseCallTool.Error(
                    $"fieldOfView must be finite and between {MinFieldOfView} and {MaxFieldOfView} degrees. Got {resolvedFov}.");

            if (!float.IsFinite(resolvedPadding) || resolvedPadding < MinPadding || resolvedPadding > MaxPadding)
                return ResponseCallTool.Error(
                    $"padding must be finite and between {MinPadding} and {MaxPadding}. Got {resolvedPadding}.");

            if (!float.IsFinite(resolvedNear) || resolvedNear <= 0f || resolvedNear > MaxNearClip)
                return ResponseCallTool.Error(
                    $"nearClipPlane must be finite and in (0, {MaxNearClip}]. Got {resolvedNear}.");

            if (!float.IsFinite(resolvedFar) || resolvedFar <= resolvedNear || resolvedFar > MaxFarClip)
                return ResponseCallTool.Error(
                    $"farClipPlane must be finite, > nearClipPlane ({resolvedNear}), and <= {MaxFarClip}. Got {resolvedFar}.");

            List<IsolatedLightConfig> lightConfigs;
            try
            {
                lightConfigs = ParseLights(lights);
            }
            catch (JsonException ex)
            {
                return ResponseCallTool.Error($"[Error] Failed to parse lights JSON: {ex.Message}");
            }

            if (!ColorUtility.TryParseHtmlString(resolvedBackgroundColor, out var clearColor))
                return ResponseCallTool.Error($"[Error] Invalid backgroundColor '{resolvedBackgroundColor}'. Expected '#RRGGBB', '#RRGGBBAA', or a Unity color name (e.g. 'red').");

            return MainThread.Instance.Run(() =>
            {
                var target = gameObjectRef.FindGameObject(out var findError);
                if (target == null)
                {
                    return ResponseCallTool.Error(string.IsNullOrEmpty(findError)
                        ? "[Error] GameObject not found for the given reference."
                        : $"[Error] GameObject not found: {findError}");
                }

                var renderers = CollectRenderers(target, resolvedIncludeChildren);
                if (renderers.Count == 0)
                    return ResponseCallTool.Error(resolvedIncludeChildren
                        ? "[Error] No Renderers found on target GameObject or its children. Cannot compute bounds."
                        : "[Error] No Renderers found on target GameObject (includeChildren=false). Set includeChildren=true if renderers live on child objects.");

                var bounds = ComputeBounds(renderers);

                var targetGameObjects = CollectAllGameObjects(target, resolvedIncludeChildren);

                var originalLayers = new Dictionary<GameObject, int>(targetGameObjects.Count);
                var originalActiveSelf = new Dictionary<GameObject, bool>(targetGameObjects.Count);
                var temporaryObjects = new List<GameObject>();
                RenderTexture? renderTexture = null;
                Texture2D? readbackTexture = null;
                var prevActiveRT = RenderTexture.active;

                try
                {
                    foreach (var go in targetGameObjects)
                    {
                        originalLayers[go] = go.layer;
                        originalActiveSelf[go] = go.activeSelf;
                        if (resolvedIsolated)
                        {
                            // Isolation requires the renderer's GameObject (and its hierarchy) to be active so
                            // its renderer participates in the cull. When isolated=false, leave activeSelf alone
                            // so we don't fire OnEnable on user scripts that the restore can't undo.
                            if (!go.activeSelf)
                                go.SetActive(true);
                            go.layer = IsolationLayer;
                        }
                    }

                    // ARGB32 is required for Transparent (alpha channel) and is acceptable for the
                    // other modes; alpha is simply ignored when clearFlags fills it opaque.
                    var rtFormat = RenderTextureFormat.ARGB32;

                    byte[] pngBytes;
                    if (resolvedCameraView == CameraView.Composite)
                    {
                        pngBytes = RenderComposite(
                            bounds,
                            resolvedResolution,
                            resolvedIsolated,
                            resolvedBackgroundMode,
                            clearColor,
                            resolvedFov,
                            resolvedNear,
                            resolvedFar,
                            resolvedPadding,
                            lightConfigs,
                            temporaryObjects,
                            rtFormat);
                    }
                    else
                    {
                        renderTexture = new RenderTexture(resolvedResolution, resolvedResolution, 24, rtFormat);
                        renderTexture.Create();

                        SetUpLights(lightConfigs, bounds, resolvedIsolated, temporaryObjects);

                        pngBytes = RenderSingleView(
                            resolvedCameraView,
                            bounds,
                            resolvedIsolated,
                            resolvedBackgroundMode,
                            clearColor,
                            resolvedFov,
                            resolvedNear,
                            resolvedFar,
                            resolvedPadding,
                            renderTexture,
                            temporaryObjects,
                            out readbackTexture);
                    }

                    return ResponseCallTool.Image(pngBytes, McpPlugin.Common.Consts.MimeType.ImagePng,
                        $"Isolated screenshot of '{target.name}' ({resolvedCameraView}, {resolvedResolution}x{resolvedResolution}, isolated={resolvedIsolated}, backgroundMode={resolvedBackgroundMode})");
                }
                finally
                {
                    foreach (var kvp in originalLayers)
                    {
                        if (kvp.Key != null)
                            kvp.Key.layer = kvp.Value;
                    }
                    foreach (var kvp in originalActiveSelf)
                    {
                        if (kvp.Key != null && kvp.Key.activeSelf != kvp.Value)
                            kvp.Key.SetActive(kvp.Value);
                    }
                    foreach (var go in temporaryObjects)
                    {
                        if (go != null)
                            UnityEngine.Object.DestroyImmediate(go);
                    }
                    RenderTexture.active = prevActiveRT;
                    if (readbackTexture != null)
                        UnityEngine.Object.DestroyImmediate(readbackTexture);
                    if (renderTexture != null)
                    {
                        renderTexture.Release();
                        UnityEngine.Object.DestroyImmediate(renderTexture);
                    }
                }
            });
        }

        private static List<IsolatedLightConfig> ParseLights(string? lights)
        {
            // Null / whitespace → caller did not specify lights → use default.
            // Empty array `[]` → caller explicitly disabled extra lights → return empty list.
            if (string.IsNullOrWhiteSpace(lights))
                return DefaultLights();

            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<IsolatedLightConfig>>(lights!);
            return parsed ?? DefaultLights();
        }

        private static List<IsolatedLightConfig> DefaultLights() => new List<IsolatedLightConfig>
        {
            new IsolatedLightConfig
            {
                Type = "Directional",
                Color = "#FFFFFF",
                Intensity = 1.0f,
                Rotation = new[] { 50f, -30f, 0f }
            }
        };

        private static List<Renderer> CollectRenderers(GameObject target, bool includeChildren)
        {
            var list = new List<Renderer>();
            if (includeChildren)
            {
                list.AddRange(target.GetComponentsInChildren<Renderer>(includeInactive: true));
            }
            else
            {
                var ownRenderers = target.GetComponents<Renderer>();
                if (ownRenderers != null)
                    list.AddRange(ownRenderers);
            }
            return list;
        }

        private static List<GameObject> CollectAllGameObjects(GameObject target, bool includeChildren)
        {
            var list = new List<GameObject> { target };
            if (!includeChildren)
                return list;

            foreach (Transform child in target.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (child != null && child.gameObject != target)
                    list.Add(child.gameObject);
            }
            return list;
        }

        private static Bounds ComputeBounds(List<Renderer> renderers)
        {
            var initialised = false;
            var bounds = new Bounds();
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (!initialised)
                {
                    bounds = r.bounds;
                    initialised = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (!initialised || bounds.size == Vector3.zero)
                bounds = new Bounds(bounds.center, Vector3.one * 0.1f);

            return bounds;
        }

        private static (Vector3 direction, Vector3 up) GetViewDirectionAndUp(CameraView view)
        {
            switch (view)
            {
                case CameraView.Front:  return (new Vector3(0, 0, -1), Vector3.up);
                case CameraView.Back:   return (new Vector3(0, 0, 1),  Vector3.up);
                case CameraView.Left:   return (new Vector3(-1, 0, 0), Vector3.up);
                case CameraView.Right:  return (new Vector3(1, 0, 0),  Vector3.up);
                case CameraView.Top:    return (new Vector3(0, 1, 0),  Vector3.forward);
                case CameraView.Bottom: return (new Vector3(0, -1, 0), Vector3.forward);
                default:                return (new Vector3(0, 0, -1), Vector3.up);
            }
        }

        private static Camera CreateTemporaryCamera(
            CameraView view,
            Bounds bounds,
            bool isolated,
            BackgroundMode backgroundMode,
            Color clearColor,
            float fov,
            float near,
            float far,
            float padding,
            RenderTexture target,
            List<GameObject> temporaryObjects)
        {
            var cameraGo = new GameObject("__TempIsolationCamera");
            cameraGo.hideFlags = HideFlags.HideAndDontSave;
            temporaryObjects.Add(cameraGo);

            var cam = cameraGo.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = near;
            cam.farClipPlane = far;
            cam.targetTexture = target;
            cam.allowHDR = false;
            cam.allowMSAA = false;

            switch (backgroundMode)
            {
                case BackgroundMode.SolidColor:
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = clearColor;
                    break;
                case BackgroundMode.Skybox:
                    cam.clearFlags = CameraClearFlags.Skybox;
                    break;
                case BackgroundMode.Transparent:
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(backgroundMode), backgroundMode, "Unsupported BackgroundMode.");
            }

            cam.cullingMask = isolated ? (1 << IsolationLayer) : ~0;

            var radius = bounds.extents.magnitude;
            if (radius < 0.0001f)
                radius = 0.05f;

            // Upfront validation in ScreenshotIsolated guarantees fov >= MinFieldOfView and padding >= MinPadding,
            // so no defensive clamp is needed here.
            var fovRad = fov * 0.5f * Mathf.Deg2Rad;
            var distance = (radius * padding) / Mathf.Sin(fovRad);

            var (dir, up) = GetViewDirectionAndUp(view);
            cameraGo.transform.position = bounds.center + dir * distance;
            cameraGo.transform.LookAt(bounds.center, up);

            return cam;
        }

        private static void SetUpLights(
            List<IsolatedLightConfig> configs,
            Bounds bounds,
            bool isolated,
            List<GameObject> temporaryObjects)
        {
            var index = 0;
            foreach (var cfg in configs)
            {
                var lightGo = new GameObject($"__TempIsolationLight_{index++}");
                lightGo.hideFlags = HideFlags.HideAndDontSave;
                temporaryObjects.Add(lightGo);

                var light = lightGo.AddComponent<Light>();
                ApplyLightConfig(light, lightGo.transform, cfg, bounds);

                if (cfg.CullingMask.HasValue)
                {
                    light.cullingMask = cfg.CullingMask.Value;
                }
                else
                {
                    light.cullingMask = isolated ? (1 << IsolationLayer) : ~0;
                }
            }
        }

        private static void ApplyLightConfig(Light light, Transform xform, IsolatedLightConfig cfg, Bounds bounds)
        {
            light.type = ParseLightType(cfg.Type);
            light.intensity = cfg.Intensity ?? 1.0f;

            var colorHex = cfg.Color ?? "#FFFFFF";
            light.color = ColorUtility.TryParseHtmlString(colorHex, out var c) ? c : Color.white;

            var rot = cfg.Rotation;
            xform.rotation = (rot != null && rot.Length >= 3)
                ? Quaternion.Euler(rot[0], rot[1], rot[2])
                : Quaternion.Euler(50f, -30f, 0f);

            if (cfg.Position != null && cfg.Position.Length >= 3)
            {
                xform.position = new Vector3(cfg.Position[0], cfg.Position[1], cfg.Position[2]);
            }
            else if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                xform.position = bounds.center - xform.forward * Mathf.Max(bounds.extents.magnitude * 2f, 1f);
            }

            if (cfg.Range.HasValue)
                light.range = cfg.Range.Value;
            if (cfg.SpotAngle.HasValue)
                light.spotAngle = cfg.SpotAngle.Value;
            if (cfg.InnerSpotAngle.HasValue)
                light.innerSpotAngle = cfg.InnerSpotAngle.Value;
            if (cfg.BounceIntensity.HasValue)
                light.bounceIntensity = cfg.BounceIntensity.Value;
            if (cfg.ColorTemperature.HasValue)
            {
                light.useColorTemperature = true;
                light.colorTemperature = cfg.ColorTemperature.Value;
            }
            if (cfg.CookieSize.HasValue)
            {
#if UNITY_6000_5_OR_NEWER
                // Unity 6.5+ deprecated the float cookieSize in favour of Vector2 cookieSize2D.
                // The IsolatedLightConfig wire shape stays a single float (uniform XY size) to
                // preserve backwards-compatible JSON; expand it to (x,y) at the API boundary.
                light.cookieSize2D = new Vector2(cfg.CookieSize.Value, cfg.CookieSize.Value);
#else
                light.cookieSize = cfg.CookieSize.Value;
#endif
            }

            light.shadows = ParseShadows(cfg.Shadows);
            if (cfg.ShadowStrength.HasValue)
                light.shadowStrength = Mathf.Clamp01(cfg.ShadowStrength.Value);

            light.renderMode = ParseRenderMode(cfg.RenderMode);
        }

        private static LightType ParseLightType(string? value)
        {
            if (string.IsNullOrEmpty(value)) return LightType.Directional;
            return Enum.TryParse<LightType>(value, ignoreCase: true, out var parsed) ? parsed : LightType.Directional;
        }

        private static LightShadows ParseShadows(string? value)
        {
            if (string.IsNullOrEmpty(value)) return LightShadows.None;
            return Enum.TryParse<LightShadows>(value, ignoreCase: true, out var parsed) ? parsed : LightShadows.None;
        }

        private static LightRenderMode ParseRenderMode(string? value)
        {
            if (string.IsNullOrEmpty(value)) return LightRenderMode.Auto;
            return Enum.TryParse<LightRenderMode>(value, ignoreCase: true, out var parsed) ? parsed : LightRenderMode.Auto;
        }

        private static byte[] RenderSingleView(
            CameraView view,
            Bounds bounds,
            bool isolated,
            BackgroundMode backgroundMode,
            Color clearColor,
            float fov,
            float near,
            float far,
            float padding,
            RenderTexture target,
            List<GameObject> temporaryObjects,
            out Texture2D readbackTexture)
        {
            var cam = CreateTemporaryCamera(view, bounds, isolated, backgroundMode, clearColor, fov, near, far, padding, target, temporaryObjects);
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = target;
            var format = backgroundMode == BackgroundMode.Transparent ? TextureFormat.ARGB32 : TextureFormat.RGB24;
            readbackTexture = new Texture2D(target.width, target.height, format, false);
            try
            {
                readbackTexture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                readbackTexture.Apply();
                return readbackTexture.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = prev;
            }
        }

        private static byte[] RenderComposite(
            Bounds bounds,
            int subResolution,
            bool isolated,
            BackgroundMode backgroundMode,
            Color clearColor,
            float fov,
            float near,
            float far,
            float padding,
            List<IsolatedLightConfig> lightConfigs,
            List<GameObject> temporaryObjects,
            RenderTextureFormat rtFormat)
        {
            var quadrantViews = new[] { CameraView.Front, CameraView.Right, CameraView.Back, CameraView.Top };
            var compositeSize = subResolution * 2;
            var format = backgroundMode == BackgroundMode.Transparent ? TextureFormat.ARGB32 : TextureFormat.RGB24;

            SetUpLights(lightConfigs, bounds, isolated, temporaryObjects);

            var compositeTex = new Texture2D(compositeSize, compositeSize, format, false);
            try
            {
                var clearPixels = new Color32[compositeSize * compositeSize];
                var fill = backgroundMode == BackgroundMode.Transparent ? new Color32(0, 0, 0, 0) : (Color32)clearColor;
                for (var i = 0; i < clearPixels.Length; i++) clearPixels[i] = fill;
                compositeTex.SetPixels32(clearPixels);

                for (var i = 0; i < quadrantViews.Length; i++)
                {
                    var quadrantView = quadrantViews[i];
                    RenderTexture? rt = null;
                    Texture2D? subTex = null;
                    Camera? cam = null;
                    try
                    {
                        rt = new RenderTexture(subResolution, subResolution, 24, rtFormat);
                        rt.Create();

                        // Per-quadrant temp camera is appended to the outer temporaryObjects so
                        // the caller's finally is the single source of truth for camera cleanup
                        // even if anything below throws.
                        cam = CreateTemporaryCamera(quadrantView, bounds, isolated, backgroundMode, clearColor, fov, near, far, padding, rt, temporaryObjects);
                        cam.Render();

                        var prev = RenderTexture.active;
                        RenderTexture.active = rt;
                        subTex = new Texture2D(subResolution, subResolution, format, false);
                        try
                        {
                            subTex.ReadPixels(new Rect(0, 0, subResolution, subResolution), 0, 0);
                            subTex.Apply();
                        }
                        finally
                        {
                            RenderTexture.active = prev;
                        }

                        var (dstX, dstY) = QuadrantOffset(i, subResolution);
                        var srcPixels = subTex.GetPixels32();
                        compositeTex.SetPixels32(dstX, dstY, subResolution, subResolution, srcPixels);
                    }
                    finally
                    {
                        // Detach the camera's targetTexture before releasing rt — Unity logs
                        // "Releasing render texture that is set as Camera.targetTexture!" otherwise.
                        // The camera GameObject itself is destroyed later in the outer finally
                        // (it lives in temporaryObjects).
                        if (cam != null)
                            cam.targetTexture = null;
                        if (subTex != null)
                            UnityEngine.Object.DestroyImmediate(subTex);
                        if (rt != null)
                        {
                            rt.Release();
                            UnityEngine.Object.DestroyImmediate(rt);
                        }
                    }
                }

                compositeTex.Apply();
                return compositeTex.EncodeToPNG();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(compositeTex);
            }
        }

        private static (int x, int y) QuadrantOffset(int index, int sub)
        {
            // Layout (Unity texture origin = bottom-left):
            //   top-left = Front,   top-right = Right
            //   bot-left = Back,    bot-right = Top
            switch (index)
            {
                case 0: return (0, sub);       // Front -> top-left
                case 1: return (sub, sub);     // Right -> top-right
                case 2: return (0, 0);         // Back  -> bottom-left
                case 3: return (sub, 0);       // Top   -> bottom-right
                default: throw new ArgumentOutOfRangeException(nameof(index), index, "Quadrant index must be 0..3.");
            }
        }
    }
}
