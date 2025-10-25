using System.Collections.Generic;
using System.Text.Json.Nodes;
using com.IvanMurzak.ReflectorNet.Utils;

namespace com.IvanMurzak.Unity.MCP.Common.Tests.Utils
{
    internal static class JsonObjectBuilderConstructExtensions
    {
        public static JsonObjectBuilder AddCompanyDefine(this JsonObjectBuilder builder)
        {
            // Company definition
            return builder
                .AddDefinition(
                    TypeUtils.GetSchemaTypeId<SampleData.Company>(),
                    new JsonObjectBuilder()
                        .SetTypeObject()
                        .AddSimpleProperty(nameof(SampleData.Company.Name), JsonSchema.String, required: false)
                        .AddRefProperty<SampleData.Address>(nameof(SampleData.Company.Headquarters), required: false)
                        .AddRefProperty<SampleData.Person[]>(nameof(SampleData.Company.Employees), required: false)
                        .AddRefProperty<Dictionary<string, SampleData.Person[]>>(nameof(SampleData.Company.Teams), required: false)
                        .AddRefProperty<Dictionary<string, Dictionary<string, SampleData.Person>>>(nameof(SampleData.Company.Directory), required: false)
                        .BuildJsonObject())

                // Address definition
                .AddDefinition(
                    TypeUtils.GetSchemaTypeId<SampleData.Address>(),
                    new JsonObjectBuilder()
                        .SetTypeObject()
                        .AddSimpleProperty(nameof(SampleData.Address.Street), JsonSchema.String, required: false)
                        .AddSimpleProperty(nameof(SampleData.Address.City), JsonSchema.String, required: false)
                        .AddSimpleProperty(nameof(SampleData.Address.Zip), JsonSchema.String, required: false)
                        .AddSimpleProperty(nameof(SampleData.Address.Country), JsonSchema.String, required: false)
                        .BuildJsonObject())

                // Person[] array definition
                .AddArrayDefinitionRef(
                    name: "com.IvanMurzak.Unity.MCP.Common.Tests.SampleData.Person_Array",
                    itemType: "com.IvanMurzak.Unity.MCP.Common.Tests.SampleData.Person")

                // Person definition
                .AddDefinition(
                    TypeUtils.GetSchemaTypeId<SampleData.Person>(),
                    new JsonObjectBuilder()
                        .SetTypeObject()
                        .AddSimpleProperty(nameof(SampleData.Person.FirstName), JsonSchema.String, required: false)
                        .AddSimpleProperty(nameof(SampleData.Person.LastName), JsonSchema.String, required: false)
                        .AddSimpleProperty(nameof(SampleData.Person.Age), JsonSchema.Integer, required: true)
                        .AddRefProperty<SampleData.Address>(nameof(SampleData.Person.Address), required: false)
                        .AddRefProperty<string[]>(nameof(SampleData.Person.Tags), required: false)
                        .AddRefProperty<Dictionary<string, int>>(nameof(SampleData.Person.Scores), required: false)
                        .AddRefProperty<int[]>(nameof(SampleData.Person.Numbers), required: false)
                        .AddRefProperty<string[][]>(nameof(SampleData.Person.JaggedAliases), required: false)
                        .AddRefProperty<int[]>(nameof(SampleData.Person.Matrix2x2), required: false)
                        .BuildJsonObject())

                // string[] array definition
                .AddArrayDefinition(
                    name: "System.String_Array",
                    itemType: JsonSchema.String)

                // Dictionary<string, int> definition
                .AddDefinition(
                    TypeUtils.GetSchemaTypeId<Dictionary<string, int>>(),
                    new JsonObject
                    {
                        [JsonSchema.Type] = JsonSchema.Object,
                        [JsonSchema.AdditionalProperties] = new JsonObject
                        {
                            [JsonSchema.Type] = JsonSchema.Integer
                        }
                    })

                // int[] array definition
                .AddArrayDefinition(
                    name: "System.Int32_Array",
                    itemType: JsonSchema.Integer)

                // string[][] jagged array definition
                .AddArrayDefinitionRef(
                    name: "System.String_Array_Array",
                    itemType: "System.String_Array")

                // Dictionary<string, Person[]> definition
                .AddDefinition(
                    TypeUtils.GetSchemaTypeId<Dictionary<string, SampleData.Person[]>>(),
                    new JsonObject
                    {
                        [JsonSchema.Type] = JsonSchema.Object,
                        [JsonSchema.AdditionalProperties] = new JsonObject
                        {
                            [JsonSchema.Type] = JsonSchema.Array,
                            [JsonSchema.Items] = new JsonObject
                            {
                                [JsonSchema.Ref] = JsonSchema.RefValue + TypeUtils.GetSchemaTypeId<SampleData.Person>()
                            }
                        }
                    })

                // Dictionary<string, Dictionary<string, Person>> definition
                .AddDefinition(
                    TypeUtils.GetSchemaTypeId<Dictionary<string, Dictionary<string, SampleData.Person>>>(),
                    new JsonObject
                    {
                        [JsonSchema.Type] = JsonSchema.Object,
                        [JsonSchema.AdditionalProperties] = new JsonObject
                        {
                            [JsonSchema.Type] = JsonSchema.Object,
                            [JsonSchema.AdditionalProperties] = new JsonObjectBuilder()
                                .SetTypeObject()
                                .AddSimpleProperty(nameof(SampleData.Person.FirstName), JsonSchema.String, required: false)
                                .AddSimpleProperty(nameof(SampleData.Person.LastName), JsonSchema.String, required: false)
                                .AddSimpleProperty(nameof(SampleData.Person.Age), JsonSchema.Integer, required: true)
                                .AddRefProperty<SampleData.Address>(nameof(SampleData.Person.Address), required: false)
                                .AddRefProperty<string[]>(nameof(SampleData.Person.Tags), required: false)
                                .AddRefProperty<Dictionary<string, int>>(nameof(SampleData.Person.Scores), required: false)
                                .AddRefProperty<int[]>(nameof(SampleData.Person.Numbers), required: false)
                                .AddRefProperty<string[][]>(nameof(SampleData.Person.JaggedAliases), required: false)
                                .AddRefProperty<int[]>(nameof(SampleData.Person.Matrix2x2), required: false)
                                .BuildJsonObject()
                        }
                    })

                // Dictionary<string, Person> definition
                .AddDefinition(
                    TypeUtils.GetSchemaTypeId<Dictionary<string, SampleData.Person>>(),
                    new JsonObject
                    {
                        [JsonSchema.Type] = JsonSchema.Object,
                        [JsonSchema.AdditionalProperties] = new JsonObjectBuilder()
                            .SetTypeObject()
                            .AddSimpleProperty(nameof(SampleData.Person.FirstName), JsonSchema.String, required: false)
                            .AddSimpleProperty(nameof(SampleData.Person.LastName), JsonSchema.String, required: false)
                            .AddSimpleProperty(nameof(SampleData.Person.Age), JsonSchema.Integer, required: true)
                            .AddRefProperty<SampleData.Address>(nameof(SampleData.Person.Address), required: false)
                            .AddRefProperty<string[]>(nameof(SampleData.Person.Tags), required: false)
                            .AddRefProperty<Dictionary<string, int>>(nameof(SampleData.Person.Scores), required: false)
                            .AddRefProperty<int[]>(nameof(SampleData.Person.Numbers), required: false)
                            .AddRefProperty<string[][]>(nameof(SampleData.Person.JaggedAliases), required: false)
                            .AddRefProperty<int[]>(nameof(SampleData.Person.Matrix2x2), required: false)
                            .BuildJsonObject()
                    });
        }
    }
}