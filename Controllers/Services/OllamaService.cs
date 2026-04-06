using System.Text;
using System.Text.Json;

namespace ResumeTailorApp.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;

        public OllamaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromMinutes(30);
        }

        public async Task<string> RewriteResumeAsync(string prompt)
        {
            var requestBody = new
            {
                model = "phi3",
                prompt,
                stream = false,
                options = new
                {
                    num_predict = 3000,
                    temperature = 0.4
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("http://localhost:11434/api/generate", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Ollama request failed. Status: {(int)response.StatusCode} {response.StatusCode}. Response: {errorBody}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseJson);

                if (doc.RootElement.TryGetProperty("response", out var responseElement))
                {
                    return responseElement.GetString()?.Trim() ?? string.Empty;
                }

                return string.Empty;
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception("Ollama request timed out. The model is taking too long or the prompt is too large.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Could not reach Ollama at http://localhost:11434.", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception("Ollama returned an invalid response format.", ex);
            }
        }
    }
}