// Mirror of the core package's extensions.compatibility.json, checked against C#/installer mirrors.
export const UNITY_EXTENSION_COMPATIBILITY: Readonly<Record<string, Readonly<Record<string, string>>>> = {
  'com.ivanmurzak.unity.mcp.navigation': { 'com.unity.ai.navigation': '2.0.10' },
  'com.ivanmurzak.unity.mcp.probuilder': { 'com.unity.probuilder': '6.1.3' },
  'com.ivanmurzak.unity.mcp.tilemap': { 'com.unity.2d.tilemap.extras': '7.0.1' },
};

export function unityExtensionDependencies(packageId: string, unityVersion: string): Readonly<Record<string, string>> {
  const match = /^(\d+)\.(\d+)/.exec(unityVersion);
  if (!match || Number(match[1]) < 6000 || (Number(match[1]) === 6000 && Number(match[2]) < 6)) return {};
  return UNITY_EXTENSION_COMPATIBILITY[packageId] ?? {};
}
