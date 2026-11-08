using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MediLink.OpenApi;

/// <summary>
/// Declara en el documento OpenAPI que existe un esquema de seguridad "ApiKey"
/// (cabecera X-API-Key). Esto crea el botón "Authorize" en Swagger UI.
/// </summary>
internal sealed class ApiKeySecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = "MediLink API",
            Version = "v1",
            Description = "API del sistema hospitalario MediLink: pacientes, médicos, consultas, " +
                          "recetas electrónicas y reposición de insumos con la farmacia."
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["ApiKey"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            Name = "X-API-Key",
            In = ParameterLocation.Header,
            Description = "Clave de acceso enviada en la cabecera X-API-Key."
        };

        return Task.CompletedTask;
    }
}

/// <summary>
/// Marca con candado solo los endpoints que ApiKeyMiddleware realmente protege.
/// Las reglas deben coincidir con las de Middleware/ApiKeyMiddleware.cs.
/// </summary>
internal sealed class ApiKeyOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var path = "/" + (context.Description.RelativePath ?? string.Empty).ToLowerInvariant();

        bool protegido =
            (path.Contains("/api/consultas/") && path.Contains("/recetas")) ||
            path.Contains("/api/farmacia/recetas") ||
            path.Contains("/api/reposiciones");

        if (!protegido)
            return Task.CompletedTask;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("ApiKey", context.Document)] = []
        });

        return Task.CompletedTask;
    }
}