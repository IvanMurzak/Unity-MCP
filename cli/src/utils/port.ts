import { createHash } from 'crypto';

const MIN_PORT = 20000;
const MAX_PORT = 29999;
const PORT_RANGE = MAX_PORT - MIN_PORT + 1;
// Routing pin = first 4 bytes of the hash rendered as 8 lowercase hex chars.
const PIN_BYTES = 4;

// Characters where JS String.prototype.toLowerCase() diverges from .NET
// string.ToLowerInvariant(). ToLowerInvariant is the canonical origin of the
// ProjectIdentity derivation (see MCP-Plugin-dotnet ProjectIdentity.GoldenVectors.json),
// so the TS port must reproduce it byte-for-byte. Each entry maps a code point to the
// value ToLowerInvariant produces:
//   U+0130 LATIN CAPITAL LETTER I WITH DOT ABOVE - ToLowerInvariant leaves it unchanged
//   (no case fold), whereas toLowerCase() lowers it to U+0069 U+0307 (i + COMBINING DOT ABOVE).
const INVARIANT_LOWER_OVERRIDES: Record<string, string> = {
  'İ': 'İ',
};

/**
 * Lowercase a string the way .NET string.ToLowerInvariant() does: a simple,
 * culture-independent, per-code-point mapping (no context-sensitive rules such as the
 * Greek final-sigma or the Turkish-i special cases). We lower each code point on its own
 * and apply INVARIANT_LOWER_OVERRIDES for the few points where JS disagrees with .NET.
 */
function toLowerInvariant(value: string): string {
  let out = '';
  for (const ch of value) {
    out += INVARIANT_LOWER_OVERRIDES[ch] ?? ch.toLowerCase();
  }
  return out;
}

/**
 * Trim trailing directory separators ('/' and '\\') so '/a/b' and '/a/b/' are the same
 * project. Never trims below length 1 (matches ProjectIdentity.TrimTrailingSeparators).
 * Separators are NOT converted — 'C:\\a' and 'C:/a' remain distinct and hash differently.
 */
function trimTrailingSeparators(pathStr: string): string {
  let end = pathStr.length;
  while (end > 1 && (pathStr[end - 1] === '/' || pathStr[end - 1] === '\\')) {
    end--;
  }
  return end === pathStr.length ? pathStr : pathStr.slice(0, end);
}

/**
 * The exact string that is UTF-8/SHA-256 hashed: the project root with trailing directory
 * separators trimmed, then lowercased with the ToLowerInvariant-matching rules above.
 * Exposed so the golden-vector parity test can reproduce the pre-hash string.
 */
export function normalizeProjectRoot(projectRoot: string): string {
  return toLowerInvariant(trimTrailingSeparators(projectRoot));
}

function hashOf(projectRoot: string): Buffer {
  return createHash('sha256').update(normalizeProjectRoot(projectRoot), 'utf-8').digest();
}

/**
 * The routing pin: the first 4 bytes of the SHA-256 of the normalized project root as 8
 * lowercase hex characters. Byte-for-byte the C# ProjectIdentity.DerivePin.
 */
export function deriveProjectPin(dir: string): string {
  return hashOf(dir).subarray(0, PIN_BYTES).toString('hex');
}

/**
 * Generate a deterministic port from a directory path.
 * Ports the canonical C# ProjectIdentity derivation (DerivePort), which is itself byte-for-byte
 * the shipped Unity UnityMcpPlugin.GeneratePortFromDirectory() logic:
 * SHA256 of the normalized (trailing-separator-trimmed, ToLowerInvariant) directory → first
 * 4 bytes as a little-endian uint32 → modulo 10000 + 20000 (range 20000-29999).
 */
export function generatePortFromDirectory(dir: string): number {
  const hash = hashOf(dir);

  // Read first 4 bytes as little-endian int32, then treat as unsigned.
  const int32 = hash.readInt32LE(0);
  const uint32 = int32 >>> 0;

  return MIN_PORT + (uint32 % PORT_RANGE);
}
