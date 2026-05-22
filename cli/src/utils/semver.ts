const SEMVER_NUMERIC = /^\d+(\.\d+)*$/;

/** Returns true if the string is a valid numeric semver (e.g. "1.2.3"). */
export function isValidVersion(version: string): boolean {
  return SEMVER_NUMERIC.test(version);
}

/** Returns -1 if a < b, 0 if equal, 1 if a > b. */
function compareVersions(a: string, b: string): number {
  const pa = a.split('.').map(Number);
  const pb = b.split('.').map(Number);
  const len = Math.max(pa.length, pb.length);

  for (let i = 0; i < len; i++) {
    const na = pa[i] ?? 0;
    const nb = pb[i] ?? 0;
    if (na < nb) return -1;
    if (na > nb) return 1;
  }
  return 0;
}

/** Returns true if `latest` is strictly greater than `current`. */
export function isNewerVersion(current: string, latest: string): boolean {
  return compareVersions(current, latest) < 0;
}
