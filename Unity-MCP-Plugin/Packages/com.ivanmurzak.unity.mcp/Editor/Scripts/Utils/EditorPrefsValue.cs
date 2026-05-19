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
using UnityEditor;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    // Editor-only state used to be stored via PlayerPrefsEx (PlayerPrefsBool / String / Int).
    // PlayerPrefs is the same store the player game uses for runtime settings, so when a user
    // clears their game's save data — including audio volume or other runtime preferences —
    // they also wipe editor-only state that has nothing to do with the game (e.g. the update
    // checker's "Don't show again" choice, foldout expanded state, etc.). See
    // https://github.com/IvanMurzak/Unity-MCP/issues/755.
    //
    // EditorPrefs is the correct backing store for editor-only state: it persists per Unity
    // installation (Windows registry / macOS plist / Linux config dir), is never touched by
    // PlayerPrefs.DeleteAll(), and is not even available at runtime. These thin wrappers
    // mirror the PlayerPrefsEx `Value` property API so call sites can swap the type name
    // with no other change. There is no `Save()` method — EditorPrefs persists immediately
    // (verifiable: re-reading right after a set always returns the written value), so no
    // explicit flush is required when migrating from PlayerPrefsEx.
    public sealed class EditorPrefsBool
    {
        readonly string _key;
        readonly bool _defaultValue;

        public EditorPrefsBool(string key, bool defaultValue = false)
        {
            _key = key;
            _defaultValue = defaultValue;
        }

        public string Key => _key;

        public bool Value
        {
            get => EditorPrefs.GetBool(_key, _defaultValue);
            set => EditorPrefs.SetBool(_key, value);
        }

        public void Delete() => EditorPrefs.DeleteKey(_key);
    }

    public sealed class EditorPrefsString
    {
        readonly string _key;
        readonly string _defaultValue;

        public EditorPrefsString(string key, string defaultValue = "")
        {
            _key = key;
            _defaultValue = defaultValue;
        }

        public string Key => _key;

        public string Value
        {
            get => EditorPrefs.GetString(_key, _defaultValue);
            set => EditorPrefs.SetString(_key, value);
        }

        public void Delete() => EditorPrefs.DeleteKey(_key);
    }

    public sealed class EditorPrefsInt
    {
        readonly string _key;
        readonly int _defaultValue;

        public EditorPrefsInt(string key, int defaultValue = 0)
        {
            _key = key;
            _defaultValue = defaultValue;
        }

        public string Key => _key;

        public int Value
        {
            get => EditorPrefs.GetInt(_key, _defaultValue);
            set => EditorPrefs.SetInt(_key, value);
        }

        public void Delete() => EditorPrefs.DeleteKey(_key);
    }
}
