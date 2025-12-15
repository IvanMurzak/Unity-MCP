/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Kieran Hannigan (https://github.com/KaiStarkk)          │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Reflection.Convertor
{
    public partial class UnityEngine_Animator_ReflectionConvertor : UnityEngine_GenericComponent_ReflectionConvertor<Animator>
    {
        protected override IEnumerable<string> GetIgnoredProperties()
        {
            foreach (var property in base.GetIgnoredProperties())
                yield return property;

            // Properties that cause infinite recursion or contain complex object graphs
            yield return nameof(Animator.runtimeAnimatorController);
            yield return nameof(Animator.avatar);
            yield return nameof(Animator.playableGraph);
            yield return nameof(Animator.avatarRoot);
        }
    }
}
