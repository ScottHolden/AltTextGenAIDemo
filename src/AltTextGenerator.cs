using System.Diagnostics;
using Azure.AI.OpenAI;

namespace AltTextGenAi;

internal class AltTextGenerator(
    OpenAIClient _openAIClient,
    AzureOpenAIConfig _config,
    HttpClient _httpClient,
    ILogger<AltTextGenerator> _logger
)
{
    private static readonly string Prompt = """
        You are an alt text generator for images. Given an image create a description of the image for a user who cannot see it.
        You may also be provided with some supporting text to help you generate the alt text, if something is mentioned in the text, focus only on it in the alt text.
        Keep all responses to a single sentence in length.
        """;

    // Should include {0} where the supporting text is placed
    private static readonly string SupportingPromptFormat = """
        The image is related to: "{0}". Only mention this in the alt text, no other objects unrelated to this.
        """;


    public async Task<AltText> FromImageUrlAsync(string imageUrl, string supportingText)
    {
        _logger.LogInformation("Getting image data from {ImageUrl}", imageUrl);

        var imageData = await _httpClient.GetByteArrayAsync(imageUrl);

        _logger.LogInformation("Downloaded {ImageSize} bytes", imageData.Length);

        return await FromImageAsync(imageData, supportingText);
    }
    public async Task<AltText> FromImageAsync(ReadOnlyMemory<byte> imageData, string supportingText)
    {
        var resizedImageData = ImageResize.ResizeToJpeg(imageData, 256, 256);
        return await FromImageAsync(resizedImageData, supportingText);
    }

    public async Task<AltText> FromImageAsync(ContentPayload data, string supportingText)
    {
        _logger.LogInformation("Image is {Size} bytes", data.Data.Length);

        var supportingPrompt = string.IsNullOrWhiteSpace(supportingText) ? "" : string.Format(SupportingPromptFormat, supportingText);

        _logger.LogInformation("Supporting Prompt: {SupportingPrompt}", supportingPrompt);

        var options = new ChatCompletionsOptions(
            _config.ChatCompletionVisionDeployment,
            [
                new ChatRequestSystemMessage(Prompt),
                new ChatRequestUserMessage(
                    new ChatMessageImageContentItem(data.ToUri()),
                    new ChatMessageTextContentItem(supportingPrompt)
                )
            ]
        )
        {
            MaxTokens = 200
        };
        var sw = Stopwatch.StartNew();
        var result = await _openAIClient.GetChatCompletionsAsync(options);
        sw.Stop();
        var response = result.Value.Choices[0].Message.Content;
        var usage = UsageStats.FromUsage(result.Value.Usage) with
        {
            InferenceTimeMs = sw.ElapsedMilliseconds
        };

        _logger.LogInformation("Azure OpenAI Usage for {Deployment}: {Usage}", _config.ChatCompletionVisionDeployment, usage);

        return new AltText(response, usage);
    }
}
