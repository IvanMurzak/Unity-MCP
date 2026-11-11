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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace com.IvanMurzak.Unity.MCP.Editor.Rql.AssetStore
{
    /// <summary>One asset inside a .unitypackage: its GUID, the project path it imports to and whether it carries a file.</summary>
    public sealed class UnityPackageEntry
    {
        public string guid { get; }
        public string path { get; }
        /// <summary>False for a folder (only a pathname and a .meta).</summary>
        public bool isFile { get; internal set; }
        public long sizeBytes { get; internal set; }

        public UnityPackageEntry(string guid, string path)
        {
            this.guid = guid;
            this.path = path;
        }
    }

    /// <summary>
    /// Reads the table of contents of a .unitypackage (a gzipped tar of <c>&lt;guid&gt;/pathname</c>, <c>&lt;guid&gt;/asset</c>,
    /// <c>&lt;guid&gt;/asset.meta</c> entries) without importing it. Only the small pathname entries are kept; asset bytes are
    /// streamed past. Results are cached per file (path, size, write time), since a large package takes seconds to scan.
    /// </summary>
    public static class UnityPackageArchive
    {
        const int BlockSize = 512;
        const int MaxPathnameBytes = 64 * 1024;

        static readonly Dictionary<string, (long length, DateTime written, List<UnityPackageEntry> entries)> _cache =
            new Dictionary<string, (long, DateTime, List<UnityPackageEntry>)>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Every asset of the package at <paramref name="packageFile"/>, sorted by path.</summary>
        public static List<UnityPackageEntry> ReadEntries(string packageFile)
        {
            var info = new FileInfo(packageFile);
            if (!info.Exists)
                throw new FileNotFoundException($"no .unitypackage at '{packageFile}'", packageFile);

            lock (_cache)
            {
                if (_cache.TryGetValue(info.FullName, out var cached) && cached.length == info.Length && cached.written == info.LastWriteTimeUtc)
                    return cached.entries;
            }

            List<UnityPackageEntry> entries;
            using (var file = File.OpenRead(info.FullName))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress))
                entries = ReadEntries(gzip);

            lock (_cache)
                _cache[info.FullName] = (info.Length, info.LastWriteTimeUtc, entries);
            return entries;
        }

        /// <summary>Parses an uncompressed tar stream of a .unitypackage (exposed for tests).</summary>
        public static List<UnityPackageEntry> ReadEntries(Stream tar)
        {
            var byGuid = new Dictionary<string, UnityPackageEntry>(StringComparer.Ordinal);
            var fileSizes = new Dictionary<string, long>(StringComparer.Ordinal);
            var header = new byte[BlockSize];
            string? longName = null;

            while (ReadFull(tar, header, BlockSize) == BlockSize)
            {
                if (IsZeroBlock(header))
                    break;

                var name = longName ?? HeaderName(header);
                longName = null;
                var size = ParseOctal(header, 124, 12);
                var type = (char)header[156];

                if (type == 'L' || type == 'x') // GNU long name / PAX header: the next entry's name
                {
                    var data = ReadData(tar, size, MaxPathnameBytes);
                    longName = type == 'L' ? Encoding.UTF8.GetString(data).TrimEnd('\0') : PaxPath(data);
                    continue;
                }

                var (guid, part) = Split(name);
                if (guid.Length == 0)
                {
                    Skip(tar, size);
                    continue;
                }

                if (part == "pathname")
                {
                    var path = FirstLine(Encoding.UTF8.GetString(ReadData(tar, size, MaxPathnameBytes)));
                    if (path.Length > 0)
                    {
                        var entry = new UnityPackageEntry(guid, path);
                        if (fileSizes.TryGetValue(guid, out var earlierSize)) { entry.isFile = true; entry.sizeBytes = earlierSize; }
                        byGuid[guid] = entry;
                    }
                    continue;
                }

                if (part == "asset")
                {
                    if (byGuid.TryGetValue(guid, out var entry)) { entry.isFile = true; entry.sizeBytes = size; }
                    else fileSizes[guid] = size; // the pathname may come after the asset
                }
                Skip(tar, size);
            }

            var list = new List<UnityPackageEntry>(byGuid.Values);
            list.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
            return list;
        }

        static (string guid, string part) Split(string name)
        {
            name = name.Replace('\\', '/');
            if (name.StartsWith("./", StringComparison.Ordinal)) name = name.Substring(2);
            var slash = name.IndexOf('/');
            if (slash <= 0) return ("", "");
            return (name.Substring(0, slash), name.Substring(slash + 1).TrimEnd('/'));
        }

        static string HeaderName(byte[] header)
        {
            var name = CString(header, 0, 100);
            // ustar: a prefix extends the name (magic "ustar" at 257)
            if (header[257] == (byte)'u' && header[258] == (byte)'s' && header[259] == (byte)'t' && header[260] == (byte)'a' && header[261] == (byte)'r')
            {
                var prefix = CString(header, 345, 155);
                if (prefix.Length > 0) name = prefix + "/" + name;
            }
            return name;
        }

        static string? PaxPath(byte[] data)
        {
            // records: "<len> key=value\n"
            foreach (var record in Encoding.UTF8.GetString(data).Split('\n'))
            {
                var space = record.IndexOf(' ');
                if (space < 0) continue;
                var kv = record.Substring(space + 1);
                if (kv.StartsWith("path=", StringComparison.Ordinal)) return kv.Substring(5);
            }
            return null;
        }

        static string FirstLine(string text)
        {
            var end = text.IndexOfAny(new[] { '\n', '\r', '\0' });
            return (end < 0 ? text : text.Substring(0, end)).Trim();
        }

        static string CString(byte[] buffer, int offset, int length)
        {
            var end = offset;
            while (end < offset + length && buffer[end] != 0) end++;
            return Encoding.UTF8.GetString(buffer, offset, end - offset);
        }

        static long ParseOctal(byte[] buffer, int offset, int length)
        {
            if ((buffer[offset] & 0x80) != 0) // base-256 (GNU) for sizes over 8 GB
            {
                long big = buffer[offset] & 0x7F;
                for (var i = offset + 1; i < offset + length; i++) big = (big << 8) | buffer[i];
                return big;
            }
            long value = 0;
            for (var i = offset; i < offset + length; i++)
            {
                var c = buffer[i];
                if (c == 0 || c == (byte)' ') { if (value > 0) break; continue; }
                if (c < (byte)'0' || c > (byte)'7') break;
                value = (value << 3) + (c - (byte)'0');
            }
            return value;
        }

        static bool IsZeroBlock(byte[] block)
        {
            foreach (var b in block) if (b != 0) return false;
            return true;
        }

        static byte[] ReadData(Stream tar, long size, int max)
        {
            if (size > max)
            {
                Skip(tar, size);
                return Array.Empty<byte>();
            }
            var data = new byte[size];
            if (ReadFull(tar, data, (int)size) != size) throw new InvalidDataException("the .unitypackage ends inside an entry");
            SkipPadding(tar, size);
            return data;
        }

        static readonly byte[] _skipBuffer = new byte[81920];

        /// <summary>Skips an entry's data and its padding to the next 512-byte block.</summary>
        static void Skip(Stream tar, long size) => Discard(tar, Padded(size));

        /// <summary>Skips the padding after <paramref name="size"/> bytes of data were read.</summary>
        static void SkipPadding(Stream tar, long size) => Discard(tar, Padded(size) - size);

        static void Discard(Stream tar, long count)
        {
            lock (_skipBuffer)
            {
                while (count > 0)
                {
                    var read = tar.Read(_skipBuffer, 0, (int)Math.Min(_skipBuffer.Length, count));
                    if (read <= 0) throw new InvalidDataException("the .unitypackage ends inside an entry");
                    count -= read;
                }
            }
        }

        static long Padded(long size) => (size + BlockSize - 1) / BlockSize * BlockSize;

        static int ReadFull(Stream stream, byte[] buffer, int count)
        {
            var total = 0;
            while (total < count)
            {
                var read = stream.Read(buffer, total, count - total);
                if (read <= 0) break;
                total += read;
            }
            return total;
        }
    }
}
