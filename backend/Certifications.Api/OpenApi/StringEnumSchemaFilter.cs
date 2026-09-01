using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Certifications.Api.OpenApi;

internal sealed class StringEnumSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        var nullableEnumType = Nullable.GetUnderlyingType(context.Type);
        var enumType = nullableEnumType ?? context.Type;
        if (!enumType.IsEnum || schema is not OpenApiSchema concreteSchema)
        {
            return;
        }

        concreteSchema.Type = nullableEnumType is null
            ? JsonSchemaType.String
            : JsonSchemaType.String | JsonSchemaType.Null;
        concreteSchema.Format = null;
        concreteSchema.Enum = Enum.GetNames(enumType)
            .Select(name => JsonValue.Create(name))
            .ToList<JsonNode>();
    }
}
