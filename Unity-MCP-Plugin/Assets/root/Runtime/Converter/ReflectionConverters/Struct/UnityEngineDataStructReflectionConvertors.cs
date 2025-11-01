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

namespace com.IvanMurzak.Unity.MCP.Reflection.Convertor
{
    public partial class UnityEngine_Color32_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.Color32> { }
    public partial class UnityEngine_Color_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.Color> { }

    public partial class UnityEngine_Matrix4x4_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.Matrix4x4> { }

    public partial class UnityEngine_Quaternion_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.Quaternion> { }

    public partial class UnityEngine_Vector2_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.Vector2> { }
    public partial class UnityEngine_Vector2Int_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.Vector2Int> { }
    public partial class UnityEngine_Vector3_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.Vector3> { }
    public partial class UnityEngine_Vector3Int_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.Vector3Int> { }
    public partial class UnityEngine_Vector4_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.Vector4> { }

    public partial class UnityEngine_Bounds_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.Bounds> { }
    public partial class UnityEngine_BoundsInt_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.BoundsInt> { }

    public partial class UnityEngine_Rect_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.Rect> { }
    public partial class UnityEngine_RectInt_ReflectionConvertor : UnityStructReflectionConvertor<UnityEngine.RectInt> { }
}
