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

namespace com.IvanMurzak.Unity.MCP.Reflection.Converter
{
    public partial class UnityEngine_Color32_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.Color32> { }
    public partial class UnityEngine_Color_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.Color> { }

    public partial class UnityEngine_Matrix4x4_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.Matrix4x4> { }

    public partial class UnityEngine_Quaternion_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.Quaternion> { }

    public partial class UnityEngine_Vector2_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.Vector2> { }
    public partial class UnityEngine_Vector2Int_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.Vector2Int> { }
    public partial class UnityEngine_Vector3_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.Vector3> { }
    public partial class UnityEngine_Vector3Int_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.Vector3Int> { }
    public partial class UnityEngine_Vector4_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.Vector4> { }

    public partial class UnityEngine_Bounds_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.Bounds> { }
    public partial class UnityEngine_BoundsInt_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.BoundsInt> { }

    public partial class UnityEngine_Rect_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.Rect> { }
    public partial class UnityEngine_RectInt_ReflectionConverter : UnityStructReflectionConverter<UnityEngine.RectInt> { }
}
