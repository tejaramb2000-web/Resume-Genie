using System.Text;
using System.Text.Json;

namespace ResumeTailorApp.Services
{
    public class GroqService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public GroqService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
            _httpClient.Timeout = TimeSpan.FromMinutes(2);
        }

        public async Task<string> RewriteResumeAsync(string prompt)
        {
            var apiKey = _config["Groq:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("Groq API key is missing. Check appsettings.json or user secrets.");

           var body = new
{
    model = "llama-3.1-8b-instant", // ✅ WORKING MODEL
    messages = new[]
    {
        new { role = "user", content = prompt }
    },
    temperature = 0.4,
    max_tokens = 1500
};

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.groq.com/openai/v1/chat/completions");

            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json");

            try
            {
                var response = await _httpClient.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Groq failed. Status: {(int)response.StatusCode} {response.StatusCode}. Response: {json}");
                }

                using var doc = JsonDocument.Parse(json);

                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()?.Trim() ?? string.Empty;
            }
            catch (TaskCanceledException ex)
            {
                throw new Exception("Groq request timed out.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception("Could not connect to Groq API.", ex);
            }
        }
    }
}