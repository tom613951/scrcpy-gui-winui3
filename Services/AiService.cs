using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ScrcpyGui.ViewModels
{
    public class UiChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}

namespace ScrcpyGui.Services
{

    public class RpaPosition
    {
        [JsonPropertyName("x")]
        public int X { get; set; }
        [JsonPropertyName("y")]
        public int Y { get; set; }
    }

    public class RpaAction
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("position")]
        public RpaPosition? Position { get; set; }

        [JsonPropertyName("target_position")]
        public RpaPosition? TargetPosition { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    public class RpaResponse
    {
        [JsonPropertyName("explanation")]
        public string Explanation { get; set; } = string.Empty;

        [JsonPropertyName("actions")]
        public List<RpaAction>? Actions { get; set; }
    }

    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public object? Content { get; set; } // Can be string or array for multimodal
    }

    public class AiService
    {
        private readonly HttpClient _httpClient;

        public AiService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(2); // Local models might be slow
        }

        public async Task<RpaResponse> AnalyzeScreenAndPlanAsync(string base64Image, string userPrompt, string baseUrl, string modelName, string apiKey)
        {
            var systemPrompt = @"You are a multimodal Android RPA agent.
You will be provided with a screenshot of the phone screen, and a user instruction.
You must output a JSON object describing the actions to take. 
Strictly wrap your output in a markdown JSON block ```json ... ```.

The JSON should match this schema:
{
  ""explanation"": ""Why you are taking these actions (in Chinese)"",
  ""actions"": [
    {
      ""action"": ""tap"" | ""swipe"" | ""input_text"" | ""keyevent"",
      ""position"": { ""x"": 123, ""y"": 456 },
      ""target_position"": { ""x"": 123, ""y"": 456 },
      ""text"": ""string to input""
    }
  ]
}";

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = new object[] {
                    new { type = "text", text = userPrompt },
                    new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64Image}" } }
                }}
            };

            var requestBody = new { model = modelName, messages = messages, max_tokens = 1000 };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/chat/completions");
            request.Content = content;
            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using (var document = JsonDocument.Parse(responseContent))
                {
                    var root = document.RootElement;
                    var textContent = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                    
                    var match = System.Text.RegularExpressions.Regex.Match(textContent, @"```json\s*(.*?)\s*```", System.Text.RegularExpressions.RegexOptions.Singleline);
                    if (match.Success)
                    {
                        var jsonStr = match.Groups[1].Value;
                        return JsonSerializer.Deserialize<RpaResponse>(jsonStr) ?? new RpaResponse { Explanation = "Failed to parse JSON" };
                    }
                    return new RpaResponse { Explanation = "No JSON block found. Model output: " + textContent };
                }
            }
            throw new Exception($"Error {response.StatusCode}: {responseContent}");
        }
    }
}
