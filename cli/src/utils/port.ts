import { createHash } from 'crypto';

const MIN_PORT = 50000;
const MAX_PORT = 59999;
const PORT_RANGE = MAX_PORT - MIN_PORT + 1;

/**
 * Generate a deterministic port from a directory path.
 * Ports the C# UnityMcpPlugin.GeneratePortFromDirectory() logic.
 * SHA256 hash of lowercased directory → first 4 bytes as uint32 → modulo 10000 + 50000.
 */
export function generatePortFromDirectory(dir: string): number {
  const hash = createHash('sha256')
    .update(dir.toLowerCase())
    .digest();

  // Read first 4 bytes as little-endian int32, then treat as unsigned
  const int32 = hash.readInt32LE(0);
  const uint32 = int32 >>> 0;

  return MIN_PORT + (uint32 % PORT_RANGE);
}
