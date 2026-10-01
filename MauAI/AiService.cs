using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MauAI;

public class AiService
{
    private readonly HttpClient _httpClient = new();

    private const string ApiKey = ""; //тутай ключ від AgenRouter
    private const string ChatApiUrl = "https://agentrouter.org/v1/chat/completions";
    private const string TranscriptionApiUrl = "https://agentrouter.org/v1/audio/transcriptions";

    public async Task<string> AskAi(string prompt)
    {
        var requestPayload = new
        {
            model = "deepseek-v4-flash", //краще всього залишиту цю, інші моделі не працюють
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = prompt
                }
            }
        };

        var json = JsonSerializer.Serialize(requestPayload);

        using var request = new HttpRequestMessage(HttpMethod.Post, ChatApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        AddHeaders(request);

        using var response = await _httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"AgentRouter returned status {(int)response.StatusCode} ({response.StatusCode}):\n{responseJson}");
        }

        using var document = JsonDocument.Parse(responseJson);

        return document
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    public async Task<string> TranscribeAsync(Stream audioStream, string fileName)
    {
        using var content = new MultipartFormDataContent();

        var audioContent = new StreamContent(audioStream);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/m4a");

        content.Add(audioContent, "file", fileName);
        content.Add(new StringContent("whisper-1"), "model");

        using var request = new HttpRequestMessage(HttpMethod.Post, TranscriptionApiUrl)
        {
            Content = content
        };

        AddHeaders(request);

        using var response = await _httpClient.SendAsync(request);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Speech-to-text returned status {(int)response.StatusCode} ({response.StatusCode}):\n{responseJson}");
        }

        using var document = JsonDocument.Parse(responseJson);

        return document.RootElement
            .GetProperty("text")
            .GetString() ?? string.Empty;
    }

    private static void AddHeaders(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
        request.Headers.TryAddWithoutValidation("Originator", "mauai");
        request.Headers.TryAddWithoutValidation("User-Agent", "MauAI");
    }
}
