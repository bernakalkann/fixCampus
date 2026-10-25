using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace FixCampus.Web.OpenApi;

public sealed class BearerSecurityDocumentTransformer(IApiDescriptionGroupCollectionProvider descriptions)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" };

        foreach (var description in descriptions.ApiDescriptionGroups.Items.SelectMany(group => group.Items))
        {
            var metadata = description.ActionDescriptor.EndpointMetadata;
            if (!metadata.OfType<IAuthorizeData>().Any() || metadata.OfType<IAllowAnonymous>().Any()) continue;
            var path = "/" + description.RelativePath?.Split('?')[0];
            if (!document.Paths.TryGetValue(path, out var pathItem) || pathItem.Operations is null) continue;
            var operation = pathItem.Operations.FirstOrDefault(pair =>
                string.Equals(pair.Key.ToString(), description.HttpMethod, StringComparison.OrdinalIgnoreCase)).Value;
            if (operation is null) continue;
            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
        }
        return Task.CompletedTask;
    }
}
