using Azure.AI.OpenAI;

namespace AltTextGenAi;

internal record AltText(string Content, UsageStats? Stats);
internal record UsageStats(int PromptTokens, int CompletionTokens, int TotalTokens, long? InferenceTimeMs = null)
{
    public static UsageStats FromUsage(CompletionsUsage usage)
        => new(usage.PromptTokens, usage.CompletionTokens, usage.TotalTokens);
}
internal record ContentPayload(byte[] Data, string ContentType)
{
    public Uri ToUri() => new($"data:{ContentType};base64,{Convert.ToBase64String(Data)}");
};
internal record GenerateRequest(
    string ImageUrl,
    string? SupportingText
);
internal record GenerateResponse(
    string AltText
);
internal record AzureOpenAIConfig(
    string Endpoint,
    string ChatCompletionVisionDeployment
);
internal record AppConfig(
    bool Enabled = false
);