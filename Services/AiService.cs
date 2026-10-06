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
      ""position"": { ""x"": 123, ""y"": 456 }, // coordinates normalized 0-1000 (top-left 0,0; bottom-right 1000,1000)
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

            var requestBody = new { model = modelName, messages = messages, max_tokens = 1500 };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpointUrl = NormalizeChatCompletionsUrl(baseUrl);
            var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl)
            {
                Content = content
            };

            if (!string.IsNullOrEmpty(apiKey))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            }

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(responseContent);
                var root = document.RootElement;
                var textContent = root.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                return ExtractRpaResponse(textContent);
            }
            throw new Exception($"Error {response.StatusCode}: {responseContent}");
        }

        private static string NormalizeChatCompletionsUrl(string baseUrl)
        {
            var trimmed = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                throw new ArgumentException("AI Base URL 不能为空，请在“系统设置”中配置。");
            }

            if (trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            if (trimmed.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return $"{trimmed}/chat/completions";
            }

            return $"{trimmed}/v1/chat/completions";
        }

        private static RpaResponse ExtractRpaResponse(string textContent)
        {
            if (string.IsNullOrWhiteSpace(textContent))
            {
                return new RpaResponse { Explanation = "AI 模型未返回任何内容。" };
            }

            // 1. Match ```json ... ```
            var jsonBlockMatch = System.Text.RegularExpressions.Regex.Match(
                textContent, @"```json\s*(.*?)\s*```", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (jsonBlockMatch.Success)
            {
                var parsed = TryDeserializeJson(jsonBlockMatch.Groups[1].Value);
                if (parsed != null) return parsed;
            }

            // 2. Match generic code block ``` ... ```
            var codeMatch = System.Text.RegularExpressions.Regex.Match(
                textContent, @"```\s*(\{.*?\})\s*```", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (codeMatch.Success)
            {
                var parsed = TryDeserializeJson(codeMatch.Groups[1].Value);
                if (parsed != null) return parsed;
            }

            // 3. Match raw JSON object
            var direct = TryDeserializeJson(textContent);
            if (direct != null) return direct;

            var firstBrace = textContent.IndexOf('{');
            var lastBrace = textContent.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                var candidate = textContent.Substring(firstBrace, lastBrace - firstBrace + 1);
                var parsed = TryDeserializeJson(candidate);
                if (parsed != null) return parsed;
            }

            return new RpaResponse { Explanation = textContent };
        }

        private static RpaResponse? TryDeserializeJson(string jsonStr)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<RpaResponse>(jsonStr, options);
            }
            catch
            {
                return null;
            }
        }
    }
}
