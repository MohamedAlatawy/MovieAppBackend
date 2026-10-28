using System.Text.Json.Nodes;
using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using MovieApp.Api.Contracts;
using MovieApp.Api.Data;
namespace MovieApp.Api.Configuration;

public sealed class SwaggerExamples : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.MethodInfo.DeclaringType?.Name == "MoviesController" &&
            operation.Responses?.TryGetValue("200", out var response) == true && response.Content is not null)
        {
            var movies = DemoSeed.Movies().Select(MovieDto.From).ToArray();
            object example = context.MethodInfo.Name switch
            {
                "List" => new MoviePage(movies.Take(20).ToArray(), 1, 20, 31, 2, true),
                "Search" => new MoviePage(movies.Where(m => m.Title.Contains("Toy")).ToArray(), 1, 20, 2, 1, false),
                "Patch" => movies.Single(m => m.Id == 9001) with { Title = "Updated demo title", VoteAverage = 8.2 },
                _ => movies[0]
            };
            foreach (var content in response.Content.Values)
                content.Example = JsonSerializer.SerializeToNode(example, JsonSerializerOptions.Web);
        }
        if (context.MethodInfo.GetCustomAttributes(typeof(RequireAdminAttribute), true).Length > 0)
        {
            operation.Parameters ??= [];
            operation.Parameters.Add(new OpenApiParameter { Name = "X-Admin-Key", In = ParameterLocation.Header, Required = true, Description = "Development administrator key (32+ characters).", Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "password" } });
            if (operation.RequestBody?.Content?.TryGetValue("application/json", out var media) == true)
            {
                media.Schema = context.SchemaGenerator.GenerateSchema(typeof(MoviePatchExample), context.SchemaRepository);
                media.Example = JsonNode.Parse("""{"title":"Updated demo title","voteAverage":8.2,"posterUrl":null}""");
            }
        }
        if (context.MethodInfo.DeclaringType?.Name == "DevicesController" && operation.RequestBody?.Content?.TryGetValue("application/json", out var deviceMedia) == true)
            deviceMedia.Example = JsonNode.Parse("""{"token":"REPLACE_WITH_REAL_DEVICE_FCM_TOKEN"}""");
    }
}
