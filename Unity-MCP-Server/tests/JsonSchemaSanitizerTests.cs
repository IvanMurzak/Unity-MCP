/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable

using System.Text.Json;
using System.Text.Json.Nodes;
using com.IvanMurzak.Unity.MCP.Server.Utils;

namespace com.IvanMurzak.Unity.MCP.Server.Tests;

public class JsonSchemaSanitizerTests
{
    // ───────────────────────────────────────────────────────────────
    //  SanitizeTypeName
    // ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("System.String", "System.String")]
    [InlineData("System.Int32", "System.Int32")]
    [InlineData("com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef",
                "com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef")]
    public void SanitizeTypeName_SafeNames_Unchanged(string input, string expected)
    {
        Assert.Equal(expected, JsonSchemaSanitizer.SanitizeTypeName(input));
    }

    [Theory]
    [InlineData("System.String[]", "System.String_Array")]
    [InlineData("System.Byte[][]", "System.Byte_Array_Array")]
    public void SanitizeTypeName_Arrays(string input, string expected)
    {
        Assert.Equal(expected, JsonSchemaSanitizer.SanitizeTypeName(input));
    }

    [Theory]
    [InlineData("System.Collections.Generic.List<System.String>",
                "System.Collections.Generic.List_Of_System.String")]
    [InlineData("System.Collections.Generic.Dictionary<System.String, System.Int32>",
                "System.Collections.Generic.Dictionary_Of_System.String_And_System.Int32")]
    [InlineData("System.Collections.Generic.Dictionary<System.String,System.Int32>",
                "System.Collections.Generic.Dictionary_Of_System.String_And_System.Int32")]
    public void SanitizeTypeName_Generics(string input, string expected)
    {
        Assert.Equal(expected, JsonSchemaSanitizer.SanitizeTypeName(input));
    }

    [Theory]
    [InlineData("Namespace.Outer+Inner", "Namespace.Outer.Inner")]
    [InlineData("A.B+C+D", "A.B.C.D")]
    public void SanitizeTypeName_NestedTypes(string input, string expected)
    {
        Assert.Equal(expected, JsonSchemaSanitizer.SanitizeTypeName(input));
    }

    [Theory]
    [InlineData("List<String[]>", "List_Of_String_Array")]
    [InlineData("Outer+Inner<System.String[], System.Int32>",
                "Outer.Inner_Of_System.String_Array_And_System.Int32")]
    public void SanitizeTypeName_Combined(string input, string expected)
    {
        Assert.Equal(expected, JsonSchemaSanitizer.SanitizeTypeName(input));
    }

    [Fact]
    public void SanitizeTypeName_Idempotent()
    {
        var original = "System.Collections.Generic.List<System.String[]>";
        var first    = JsonSchemaSanitizer.SanitizeTypeName(original);
        var second   = JsonSchemaSanitizer.SanitizeTypeName(first);
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void SanitizeTypeName_EmptyOrNull(string? input, string? expected)
    {
        Assert.Equal(expected, JsonSchemaSanitizer.SanitizeTypeName(input!));
    }

    [Fact]
    public void SanitizeTypeName_ResultContainsOnlySafeChars()
    {
        var inputs = new[]
        {
            "System.String[]",
            "List<Dictionary<string, int>>",
            "A+B<C[], D+E>",
            "Namespace.Type`2",
        };

        foreach (var input in inputs)
        {
            var result = JsonSchemaSanitizer.SanitizeTypeName(input);
            Assert.Matches(@"^[a-zA-Z0-9._\-]*$", result);
        }
    }

    // ───────────────────────────────────────────────────────────────
    //  SanitizeSchema — full schema rewriting
    // ───────────────────────────────────────────────────────────────

    [Fact]
    public void SanitizeSchema_RewritesDefsAndRefs()
    {
        var schema = JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "items": {
                    "$ref": "#/$defs/System.Collections.Generic.List<System.String>"
                }
            },
            "$defs": {
                "System.Collections.Generic.List<System.String>": {
                    "type": "array",
                    "items": { "type": "string" }
                }
            }
        }
        """);

        var sanitized = JsonSchemaSanitizer.SanitizeSchema(schema);
        var root = JsonNode.Parse(sanitized.GetRawText())!.AsObject();

        // $defs key must be sanitized.
        var defs = root["$defs"]!.AsObject();
        Assert.False(defs.ContainsKey("System.Collections.Generic.List<System.String>"));
        Assert.True(defs.ContainsKey("System.Collections.Generic.List_Of_System.String"));

        // $ref must match the new key.
        var refVal = root["properties"]!["items"]!["$ref"]!.GetValue<string>();
        Assert.Equal("#/$defs/System.Collections.Generic.List_Of_System.String", refVal);
    }

    [Fact]
    public void SanitizeSchema_NestedRefsAreRewritten()
    {
        var schema = JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "data": {
                    "$ref": "#/$defs/Outer+Inner"
                }
            },
            "$defs": {
                "Outer+Inner": {
                    "type": "object",
                    "properties": {
                        "list": {
                            "type": "array",
                            "items": {
                                "$ref": "#/$defs/System.String[]"
                            }
                        }
                    }
                },
                "System.String[]": {
                    "type": "string"
                }
            }
        }
        """);

        var sanitized = JsonSchemaSanitizer.SanitizeSchema(schema);
        var root = JsonNode.Parse(sanitized.GetRawText())!.AsObject();
        var defs = root["$defs"]!.AsObject();

        Assert.True(defs.ContainsKey("Outer.Inner"));
        Assert.True(defs.ContainsKey("System.String_Array"));
        Assert.False(defs.ContainsKey("Outer+Inner"));
        Assert.False(defs.ContainsKey("System.String[]"));

        // Nested $ref inside $defs values must also be rewritten.
        var innerItems = defs["Outer.Inner"]!["properties"]!["list"]!["items"]!["$ref"]!.GetValue<string>();
        Assert.Equal("#/$defs/System.String_Array", innerItems);
    }

    [Fact]
    public void SanitizeSchema_AlreadySafeSchema_Unchanged()
    {
        var json = """
        {
            "type": "object",
            "properties": {
                "x": { "$ref": "#/$defs/Foo.Bar" }
            },
            "$defs": {
                "Foo.Bar": { "type": "string" }
            }
        }
        """;
        var schema    = JsonSerializer.Deserialize<JsonElement>(json);
        var sanitized = JsonSchemaSanitizer.SanitizeSchema(schema);

        // Should be identical — no rewriting needed.
        var origNode = JsonNode.Parse(schema.GetRawText())!;
        var saniNode = JsonNode.Parse(sanitized.GetRawText())!;
        Assert.Equal(origNode.ToJsonString(), saniNode.ToJsonString());
    }

    [Fact]
    public void SanitizeSchema_NoDefsSection_Unchanged()
    {
        var json = """{ "type": "object", "properties": { "x": { "type": "string" } } }""";
        var schema    = JsonSerializer.Deserialize<JsonElement>(json);
        var sanitized = JsonSchemaSanitizer.SanitizeSchema(schema);

        Assert.Equal(
            JsonNode.Parse(schema.GetRawText())!.ToJsonString(),
            JsonNode.Parse(sanitized.GetRawText())!.ToJsonString());
    }

    [Fact]
    public void SanitizeSchema_NonObjectElement_ReturnedAsIs()
    {
        var schema = JsonSerializer.Deserialize<JsonElement>("42");
        var result = JsonSchemaSanitizer.SanitizeSchema(schema);
        Assert.Equal(JsonValueKind.Number, result.ValueKind);
    }

    [Fact]
    public void SanitizeSchema_CollisionHandling()
    {
        // Two different original names that would sanitize to the same thing.
        var schema = JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "a": { "$ref": "#/$defs/X<Y>" },
                "b": { "$ref": "#/$defs/X_Of_Y" }
            },
            "$defs": {
                "X<Y>": { "type": "string" },
                "X_Of_Y": { "type": "integer" }
            }
        }
        """);

        var sanitized = JsonSchemaSanitizer.SanitizeSchema(schema);
        var root = JsonNode.Parse(sanitized.GetRawText())!.AsObject();
        var defs = root["$defs"]!.AsObject();

        // Both keys must exist and be distinct.
        Assert.Equal(2, defs.Count);

        // The originally-safe key "X_Of_Y" keeps its name.
        Assert.True(defs.ContainsKey("X_Of_Y"));

        // The colliding key gets a suffix.
        Assert.True(defs.ContainsKey("X_Of_Y_2"));

        // Refs point to the correct keys.
        var refA = root["properties"]!["a"]!["$ref"]!.GetValue<string>();
        var refB = root["properties"]!["b"]!["$ref"]!.GetValue<string>();
        Assert.Equal("#/$defs/X_Of_Y_2", refA);
        Assert.Equal("#/$defs/X_Of_Y", refB);
    }

    [Fact]
    public void SanitizeSchema_GptHostileTypes_AllSanitized()
    {
        // Every C# type pattern that breaks GPT clients.
        var schema = JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "a": { "$ref": "#/$defs/System.String[]" },
                "b": { "$ref": "#/$defs/System.Collections.Generic.List<System.String>" },
                "c": { "$ref": "#/$defs/System.Collections.Generic.Dictionary<System.String, System.Int32>" },
                "d": { "$ref": "#/$defs/Namespace.Outer+Inner" },
                "e": { "$ref": "#/$defs/List<String[]>" },
                "f": { "$ref": "#/$defs/System.Nullable`1<System.Int32>" }
            },
            "$defs": {
                "System.String[]":                              { "type": "array",   "items": { "type": "string" } },
                "System.Collections.Generic.List<System.String>": { "type": "array", "items": { "type": "string" } },
                "System.Collections.Generic.Dictionary<System.String, System.Int32>": { "type": "object" },
                "Namespace.Outer+Inner":                        { "type": "object" },
                "List<String[]>":                               { "type": "array" },
                "System.Nullable`1<System.Int32>":              { "type": "integer" }
            }
        }
        """);

        var sanitized = JsonSchemaSanitizer.SanitizeSchema(schema);
        var root = JsonNode.Parse(sanitized.GetRawText())!.AsObject();
        var defs = root["$defs"]!.AsObject();

        // No raw C# syntax characters in any key.
        foreach (var key in defs.Select(kv => kv.Key))
        {
            Assert.DoesNotContain("[", key);
            Assert.DoesNotContain("]", key);
            Assert.DoesNotContain("<", key);
            Assert.DoesNotContain(">", key);
            Assert.DoesNotContain("+", key);
            Assert.DoesNotContain(",", key);
            Assert.DoesNotContain("`", key);
            Assert.Matches(@"^[a-zA-Z0-9._\-]+$", key);
        }

        // Every $ref must point to an existing $defs key.
        var props = root["properties"]!.AsObject();
        foreach (var prop in props)
        {
            var refVal = prop.Value!["$ref"]!.GetValue<string>();
            Assert.StartsWith("#/$defs/", refVal);
            var refKey = refVal.Substring("#/$defs/".Length);
            Assert.True(defs.ContainsKey(refKey),
                $"$ref points to '{refKey}' which is missing from $defs");
        }
    }

    [Fact]
    public void SanitizeSchema_RealWorldUnitySchema()
    {
        // Based on actual schema from the Unity MCP Plugin.
        var schema = JsonSerializer.Deserialize<JsonElement>("""
        {
            "type": "object",
            "properties": {
                "assetRef": {
                    "$ref": "#/$defs/com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef",
                    "description": "Reference to asset"
                },
                "content": {
                    "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
                }
            },
            "$defs": {
                "com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef": {
                    "type": "object",
                    "properties": {
                        "AssetPath": { "type": "string" }
                    }
                },
                "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
                    "type": "array",
                    "items": {
                        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
                    }
                },
                "com.IvanMurzak.ReflectorNet.Model.SerializedMember": {
                    "type": "object",
                    "properties": {
                        "fields": {
                            "type": "array",
                            "items": {
                                "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
                            }
                        }
                    }
                }
            },
            "required": ["assetRef", "content"]
        }
        """);

        var sanitized = JsonSchemaSanitizer.SanitizeSchema(schema);
        var root = JsonNode.Parse(sanitized.GetRawText())!.AsObject();

        // These names are already safe — should be unchanged.
        var defs = root["$defs"]!.AsObject();
        Assert.True(defs.ContainsKey("com.IvanMurzak.Unity.MCP.Runtime.Data.AssetObjectRef"));
        Assert.True(defs.ContainsKey("com.IvanMurzak.ReflectorNet.Model.SerializedMember"));
        Assert.True(defs.ContainsKey("com.IvanMurzak.ReflectorNet.Model.SerializedMemberList"));

        // Self-referencing $ref must still work.
        var fieldsRef = defs["com.IvanMurzak.ReflectorNet.Model.SerializedMember"]!
            ["properties"]!["fields"]!["items"]!["$ref"]!.GetValue<string>();
        Assert.Equal("#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember", fieldsRef);
    }
}
