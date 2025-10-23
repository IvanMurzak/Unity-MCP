using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.Utils
{
    internal class JsonObjectBuilder
    {
        public JsonObject? Result { get; private set; }

        public JsonObjectBuilder()
        {
            Result = new JsonObject();
        }

        public JsonObjectBuilder SetNull()
        {
            Result = null;
            return this;
        }

        public JsonObjectBuilder SetTypeObject()
        {
            if (Result == null)
                throw new InvalidOperationException("Cannot set type on a null schema.");

            Result[JsonSchema.Type] = JsonSchema.Object;
            return this;
        }

        public JsonObjectBuilder SetTypeArray()
        {
            if (Result == null)
                throw new InvalidOperationException("Cannot set type on a null schema.");

            Result[JsonSchema.Type] = JsonSchema.Array;
            return this;
        }

        public JsonObjectBuilder AddRequired(string name)
        {
            if (Result == null)
                throw new InvalidOperationException("Cannot add required property to a null schema.");

            if (Result[JsonSchema.Required] == null)
                Result[JsonSchema.Required] = new JsonArray();

            Result[JsonSchema.Required]!.AsArray().Add(name);

            return this;
        }

        public JsonObjectBuilder AddSimpleProperty(string name, string type, bool required = false, string? description = null)
        {
            if (Result == null)
                throw new InvalidOperationException("Cannot add property to a null schema.");

            if (Result[JsonSchema.Properties] == null)
                Result[JsonSchema.Properties] = new JsonObject();

            Result[JsonSchema.Properties]![name] = new JsonObject
            {
                [JsonSchema.Type] = type
            };

            if (required)
                AddRequired(name);

            if (description != null)
                Result[JsonSchema.Properties]![name]![JsonSchema.Description] = description;

            return this;
        }

        public JsonObjectBuilder AddJsonElementProperty(string name, JsonObject? value, bool required = false, string? description = null)
        {
            if (Result == null)
                throw new InvalidOperationException("Cannot add property to a null schema.");

            if (Result[JsonSchema.Properties] == null)
                Result[JsonSchema.Properties] = new JsonObject();

            Result[JsonSchema.Properties]![name] = value;

            if (required)
                AddRequired(name);

            if (description != null)
                Result[JsonSchema.Properties]![name]![JsonSchema.Description] = description;

            return this;
        }

        public JsonObjectBuilder AddArrayProperty(string name, string itemType, bool required = false, string? description = null)
        {
            if (Result == null)
                throw new InvalidOperationException("Cannot add property to a null schema.");

            if (Result[JsonSchema.Properties] == null)
                Result[JsonSchema.Properties] = new JsonObject();

            Result[JsonSchema.Properties]![name] = new JsonObject
            {
                [JsonSchema.Type] = JsonSchema.Array,
                [JsonSchema.Items] = new JsonObject
                {
                    [JsonSchema.Type] = itemType
                }
            };

            if (required)
                AddRequired(name);

            if (description != null)
                Result[JsonSchema.Properties]![name]![JsonSchema.Description] = description;

            return this;
        }

        public JsonObjectBuilder AddRefPropertyAndDefinition<T>(string name, bool required, JsonObject? definition, string? description = null) where T : notnull
        {
            return AddRefProperty(name, TypeUtils.GetSchemaTypeId<T>(), required, description)
                .AddDefinition(TypeUtils.GetSchemaTypeId<T>(), definition);
        }

        public JsonObjectBuilder AddRefProperty<T>(string name, bool required = false, string? description = null) where T : notnull
        {
            return AddRefProperty(name, TypeUtils.GetSchemaTypeId<T>(), required, description);
        }

        public JsonObjectBuilder AddRefProperty(string name, string typeId, bool required = false, string? description = null)
        {
            if (Result == null)
                throw new InvalidOperationException("Cannot add property to a null schema.");

            if (Result[JsonSchema.Properties] == null)
                Result[JsonSchema.Properties] = new JsonObject();

            Result[JsonSchema.Properties]![name] = new JsonObject
            {
                [JsonSchema.Ref] = JsonSchema.RefValue + typeId
            };

            if (description != null)
                Result[JsonSchema.Properties]![name]![JsonSchema.Description] = description;

            if (required)
                AddRequired(name);

            return this;
        }

        public JsonObjectBuilder AddDefinition(string name, JsonObject? definition)
        {
            if (Result == null)
                throw new InvalidOperationException("Cannot add definition to a null schema.");

            if (Result[JsonSchema.Defs] == null)
                Result[JsonSchema.Defs] = new JsonObject();

            Result[JsonSchema.Defs]![name] = definition;

            return this;
        }

        public JsonObjectBuilder AddArrayDefinition(string name, string itemType)
        {
            var arrayDefinition = new JsonObject
            {
                [JsonSchema.Type] = JsonSchema.Array,
                [JsonSchema.Items] = new JsonObject
                {
                    [JsonSchema.Type] = itemType
                }
            };

            return AddDefinition(name, arrayDefinition);
        }

        public JsonObjectBuilder AddArrayDefinitionRef(string name, string itemType)
        {
            var arrayDefinition = new JsonObject
            {
                [JsonSchema.Type] = JsonSchema.Array,
                [JsonSchema.Items] = new JsonObject
                {
                    [JsonSchema.Ref] = JsonSchema.RefValue + itemType
                }
            };

            return AddDefinition(name, arrayDefinition);
        }

        public JsonObject? BuildJsonObject()
        {
            return Result;
        }
        public JsonElement? BuildJsonElement()
        {
            return BuildJsonObject()?.ToJsonElement();
        }
    }
}