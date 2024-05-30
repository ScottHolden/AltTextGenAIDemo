using AltTextGenAi;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.BindConfiguration<AzureOpenAIConfig>("aoai");
builder.Services.AddSingleton<AltTextGenerator>();
builder.Services.AddSingleton<TokenCredential, DefaultAzureCredential>();
builder.Services.AddSingleton<OpenAIClient>(
    sp =>
    {
        var config = sp.GetRequiredService<AzureOpenAIConfig>();
        var auth = sp.GetRequiredService<TokenCredential>();
        return new OpenAIClient(new Uri(config.Endpoint), auth);
    }
);

var app = builder.Build();
app.UseHttpsRedirection();

if (app.Configuration.GetValue<bool?>("appEnabled") ?? false)
{
    app.UseStaticFiles();
    app.UseDefaultFiles();
}

app.MapPost("api/generate", async (HttpContext context, [FromServices] AltTextGenerator generator, [FromServices] ILogger<Program> logger) =>
{
    // JSON body
    try
    {
        if (context.Request.HasJsonContentType())
        {
            var generateRequest = await context.Request.ReadFromJsonAsync<GenerateRequest>();
            if (generateRequest is null || string.IsNullOrWhiteSpace(generateRequest.ImageUrl))
            {
                return Results.BadRequest("Invalid request");
            }

            var altText = await generator.FromImageUrlAsync(generateRequest.ImageUrl, generateRequest.SupportingText ?? string.Empty);

            return Results.Ok(altText);
        }
        // Image data body
        else if (context.Request.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false)
        {
            // Kinda messy but fine for now
            using var ms = new MemoryStream();
            await context.Request.Body.CopyToAsync(ms);

            string supportingText = string.Empty;
            if (context.Request.Query.TryGetValue("supportingText", out var querySupportingText))
            {
                supportingText = querySupportingText.FirstOrDefault() ?? string.Empty;
            }

            var altText = await generator.FromImageAsync(ms.ToArray(), supportingText);

            return Results.Ok(altText);
        }
        else
        {
            logger.LogWarning("Invalid content type: {ContentType}", context.Request.ContentType);
            return Results.BadRequest("Invalid content type");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error while processing request: {Message}", ex.Message);
        return Results.BadRequest("Error while processing request, please check the logs for more information");
    }
});

app.Run();