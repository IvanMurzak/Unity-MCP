# Object Reference Hierarchy

```text
ObjectRef (base — contains InstanceID)
├── AssetObjectRef (+ AssetPath, AssetGuid, AssetType)
│   └── GameObjectRef (+ Path, Name)
├── ComponentRef (+ Index, TypeName)
└── SceneRef (+ Path, BuildIndex)
```

Supporting types: `GameObjectData`, `ComponentData`, `SceneData`, `GameObjectMetadata`, plus `*Shallow` and `*List` variants. All use `[JsonPropertyName]` and implement `IsValid(out string? error)`.
