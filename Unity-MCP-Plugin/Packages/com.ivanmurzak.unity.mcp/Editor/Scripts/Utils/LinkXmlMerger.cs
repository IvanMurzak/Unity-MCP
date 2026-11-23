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
using System.Collections.Generic;
using System.Xml.Linq;

namespace com.IvanMurzak.Unity.MCP.Editor.Utils
{
    /// <summary>
    /// Merges the top-level directives of several link.xml documents into one linker document.
    /// Pure BCL so the engine-free test suite can compile it directly.
    /// </summary>
    internal static class LinkXmlMerger
    {
        public static XDocument Merge(IEnumerable<XDocument> xmlDocuments)
        {
            var mergedRoot = new XElement("linker");
            var seenElements = new HashSet<string>();

            foreach (var doc in xmlDocuments)
            {
                var elements = doc.Root?.Elements();
                if (elements == null)
                    continue;

                foreach (var element in elements)
                {
                    // Packages may declare different rules for the same assembly (other types, members,
                    // preserve levels or attributes). The linker accepts repeated <assembly> elements and
                    // applies all of them, so only exact duplicates are dropped.
                    if (seenElements.Add(element.ToString(SaveOptions.DisableFormatting)))
                        mergedRoot.Add(new XElement(element));
                }
            }

            return new XDocument(mergedRoot);
        }
    }
}
