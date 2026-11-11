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

        [RqlDescription("Unity's sign-in was opened for the user (false: already signed in, or an Editor that cannot show it: " +
                        "headless, or opened without Unity Hub; next says what to do)")]
        public bool opened { get; internal set; }

        [RqlDescription("what happens next and who does it: the user signs in, never the agent")]
        public string next { get; internal set; } = "";
    }

    /// <summary>
    /// Runs work on the Editor's next update, after the current tool call answered. Not <c>EditorApplication.delayCall</c>:
    /// that one waits while the Editor is in the background (seen on Unity 6.6); <c>update</c> keeps ticking.
    /// </summary>
    internal static class AssetStoreEditorUpdate
    {
        /// <summary>Call on the main thread (RQL calls run there): <c>update</c> is a plain delegate field.</summary>
        public static void Next(Action action)
        {
            void Once()
            {
                EditorApplication.update -= Once;
                action();
            }
            EditorApplication.update += Once;
        }
    }

    /// <summary>
    /// Starts the Unity account sign-in of the Editor with public API only (<c>CloudProjectSettings.ShowLogin</c>, available
    /// from Unity 2022.3). The user completes it themselves: the agent only opens it and later reads <c>signedIn</c>.
    /// </summary>
    internal static class AssetStoreSignInFlow
    {
        /// <summary>
        /// Test seam: what opens Unity's sign-in (tests replace it so no sign-in UI ever opens). Deferred to the next Editor
        /// update, so a modal window it opens never holds the agent's call until the user answers it.
        /// </summary>
        internal static Action ShowLogin = () => AssetStoreEditorUpdate.Next(CloudProjectSettings.ShowLogin);

        /// <summary>Test seam: the Editor runs without a window (-batchmode), where no sign-in UI can appear.</summary>
        internal static Func<bool> IsBatchMode = () => Application.isBatchMode;

        /// <summary>
        /// Test seam: the Editor was opened through Unity Hub (<c>-useHub</c>, as the Hub and <c>unity-mcp-cli open</c> start
        /// it). Only such an Editor signs in: it takes the Hub's account, and its sign-in goes through the Hub. One opened
        /// directly stays signed out, and its sign-in only offers "Install Unity Hub", even with the Hub installed.
        /// </summary>
        internal static Func<bool> LaunchedViaHub = () =>
            Array.Exists(Environment.GetCommandLineArgs(), a => string.Equals(a, "-useHub", StringComparison.OrdinalIgnoreCase));

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
            if (!LaunchedViaHub())
            {
                // Its sign-in would only show "Install Unity Hub" (a modal dialog), whether the Hub is installed or not.
                return new AssetStoreSignIn
                {
                    next = "this Editor was opened directly, not through Unity Hub, so it cannot sign in (Unity would only offer " +
                           "\"Install Unity Hub\", even with the Hub installed). Ask the user to save their work, then reopen the " +
                           "project through the Hub: unity-mcp-cli close <project> and unity-mcp-cli open <project> (it starts the " +
                           "Editor through Unity Hub when the Hub is installed; otherwise install Unity Hub first), or open it from " +
                           "Unity Hub. An Editor opened so takes the account the Hub is signed in to; if still signed out, call SignIn() again"
                };
            }
            ShowLogin();
            return new AssetStoreSignIn
            {
                opened = true,
                next = "Unity's sign-in is opening through Unity Hub for the user: ask them to sign in to their Unity account there " +
                       "(the agent never types credentials nor answers its windows), " + CheckAfter
            };
        }
    }
}
