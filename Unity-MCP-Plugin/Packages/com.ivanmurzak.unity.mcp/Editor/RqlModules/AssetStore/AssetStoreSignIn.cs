/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2026 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System;
using Rql;
using UnityEditor;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.AssetStore
{
    /// <summary>What <c>assetStore | call SignIn()</c> did, and what the user does next.</summary>
    [RqlDescription("the answer of SignIn(): whether the Editor is signed in, whether Unity's sign-in was opened, and the next step")]
    public sealed class AssetStoreSignIn
    {
        [RqlDescription("a Unity account was already signed in to the Editor when SignIn() was called")]
        public bool signedIn { get; internal set; }

        [RqlDescription("the signed-in account's user name (empty when signed out)")]
        public string userName { get; internal set; } = "";

        [RqlDescription("Unity's sign-in was opened for the user (false: already signed in, or a headless Editor that cannot show it)")]
        public bool opened { get; internal set; }

        [RqlDescription("what happens next and who does it: the user signs in, never the agent")]
        public string next { get; internal set; } = "";
    }

    /// <summary>
    /// Starts the Unity account sign-in of the Editor with public API only (<c>CloudProjectSettings.ShowLogin</c>, available
    /// from Unity 2022.3). The user completes it themselves: the agent only opens it and later reads <c>signedIn</c>.
    /// </summary>
    internal static class AssetStoreSignInFlow
    {
        /// <summary>
        /// Test seam: what opens Unity's sign-in (tests replace it so no sign-in UI ever opens). Deferred to the next Editor
        /// update: ShowLogin can show a modal dialog (Unity 6 signs in through Unity Hub and may offer "Install Unity Hub"),
        /// which would otherwise hold the agent's call until the user answers it.
        /// </summary>
        internal static Action ShowLogin = () => EditorApplication.delayCall += () => CloudProjectSettings.ShowLogin();

        /// <summary>Test seam: the Editor runs without a window (-batchmode), where no sign-in UI can appear.</summary>
        internal static Func<bool> IsBatchMode = () => Application.isBatchMode;

        /// <summary>Test seam: asks Unity for a fresh access token of the signed-in account.</summary>
        internal static Action RefreshAccessToken = () => CloudProjectSettings.RefreshAccessToken(_ => { });

        internal const string CheckAfter = "then assetStore | select signedIn userName shows the account (sign-in completes in the background)";

        public static AssetStoreSignIn Run(bool signedIn, string userName)
        {
            if (signedIn)
            {
                RefreshAccessToken();
                return new AssetStoreSignIn
                {
                    signedIn = true,
                    userName = userName,
                    next = $"already signed in as '{userName}': nothing to do (the access token is being refreshed). If the Asset Store " +
                           "still refuses the sign-in, the user signs out and in again via the Editor's Account menu (top right)"
                };
            }
            if (IsBatchMode())
            {
                return new AssetStoreSignIn
                {
                    next = "this Editor runs headless (batch mode) and cannot show Unity's sign-in: open the project in the Editor with a " +
                           "window (unity-mcp-cli open <project>) and call SignIn() there"
                };
            }
            ShowLogin();
            return new AssetStoreSignIn
            {
                opened = true,
                next = "Unity's sign-in is opening for the user (a recent Editor signs in through Unity Hub, or a dialog may ask " +
                       "about Unity Hub and block the Editor until the user answers it): ask the user to complete the sign-in to " +
                       "their Unity account themselves (the agent never types credentials nor answers that dialog), " + CheckAfter
            };
        }
    }
}
