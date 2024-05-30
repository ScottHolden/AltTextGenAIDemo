using Azure.AI.OpenAI;

namespace AltTextGenAi;

internal record AltText(string Content, UsageStats? Stats);
internal record UsageStats(int PromptTokens, int CompletionTokens, int TotalTokens, long? InferenceTimeMs = null, double? InferenceCost = null)
{
    public static UsageStats FromUsage(CompletionsUsage usage)
        => new(usage.PromptTokens, usage.CompletionTokens, usage.TotalTokens);
    public UsageStats WithOptionalPricing(double? promptTokenCostPer1k, double? completionTokenCostPer1k)
        =>  promptTokenCostPer1k.HasValue && promptTokenCostPer1k > 0 && completionTokenCostPer1k.HasValue && completionTokenCostPer1k > 0 ?
            this with
            {
                InferenceCost = (PromptTokens * promptTokenCostPer1k / 1000.0) + (CompletionTokens * completionTokenCostPer1k / 1000.0)
            } : this;
}
internal record ContentPayload(byte[] Data, string ContentType)
{
    public Uri ToUri() => new($"data:{ContentType};base64,{Convert.ToBase64String(Data)}");
    public static ContentPayload FromPng(byte[] data) => new(data, "image/jpeg");
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