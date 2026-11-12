using System.Text.Json;
using IFA.Application.Common.Interfaces;

namespace IFA.Testing;

public sealed class FakeLlmGateway : ILlmGateway
{
    public Func<LlmCompletionRequest, CancellationToken, Task<LlmCompletionResult>> Handler { get; set; } =
        (request, _) => Task.FromResult(new LlmCompletionResult
        {
            Success = true,
            RawText = request.UserPrompt
        });

    public List<LlmCompletionRequest> Requests { get; } = [];

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        LlmRole role = LlmRole.General,
        CancellationToken ct = default)
    {
        var result = await CompleteAsync(new LlmCompletionRequest
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt
        }, ct);
        return result.RawText;
    }

    public async Task<T?> CompleteJsonAsync<T>(
        string systemPrompt,
        string userPrompt,
        LlmRole role = LlmRole.General,
        CancellationToken ct = default)
    {
        var raw = await CompleteAsync(systemPrompt, userPrompt, role, ct);
        return JsonSerializer.Deserialize<T>(raw, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    public Task<LlmCompletionResult> CompleteAsync(
        LlmCompletionRequest request,
        CancellationToken ct = default)
    {
        Requests.Add(request);
        return Handler(request, ct);
    }
}
