---
name: gameobject-converter-allowsetvalue
description: jsonPatch GameObject-ref-by-instanceID needs AllowSetValue=true on UnityEngine_GameObject_ReflectionConverter, not just TreatJsonObjectAsAtomicValue
metadata:
  type: project
---

For the jsonPatch (Reflector.TryPatch) surface to resolve a GameObject reference supplied as `{"<field>":{"instanceID":"..."}}`, overriding `TreatJsonObjectAsAtomicValue(Type) => true` on `UnityEngine_GameObject_ReflectionConverter` is necessary but **NOT sufficient**. That converter has `AllowSetValue => false` by default; ReflectorNet's atomic merge-patch path gates the `SetValue` call on `AllowSetValue`, so with it false the GameObject field is silently left unmodified and the patch reports Success while assigning nothing.

**Fix:** also set `AllowSetValue => true` on `UnityEngine_GameObject_ReflectionConverter`. Its overridden `SetValue` resolves via `ToGameObjectRef().FindGameObject()` (a reference lookup, not a deep structural deserialize), so this does not reintroduce the heavy GameObject serialization the converter deliberately avoids.

**Why this matters:** The `UnityEngine_Object_ReflectionConverter<T>` (used for asset-refs like Material and component-refs like Rigidbody) already has `AllowSetValue => true`, so those object-ref behaviors work with just the TreatJsonObjectAsAtomicValue override. Only the GameObject converter — which does NOT inherit the Object converter (it extends `UnityGenericReflectionConverter<GameObject>`) — has the `AllowSetValue=false` gap. Issue #791's one-line plan and the dispatch handoff both missed this; it was found empirically (asset/component-ref tests passed, gameobject-ref failed off-by-an-id until AllowSetValue was flipped). Landed in PR #792.
