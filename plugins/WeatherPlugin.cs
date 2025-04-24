using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

public class WeatherPlugin
{
    private readonly string _bingApiKey;
    private static readonly HttpClient _httpClient = new();

    public WeatherPlugin()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        _bingApiKey = config["BingSearch:ApiKey"]!;
        if (string.IsNullOrWhiteSpace(_bingApiKey))
        {
            throw new InvalidOperationException("BingSearch:ApiKey is missing or empty in appsettings.json. Please provide your Bing Search API key.");
        }
    }

    [KernelFunction]
    [Description("Get the current weather for a city using Bing Search")]
    public async Task<string> GetCurrentWeatherAsync(
        [Description("City name")] string city)
    {
        var query = $"current weather in {city}";
        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}");
        request.Headers.Add("Ocp-Apim-Subscription-Key", _bingApiKey);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("webPages", out var webPages) ||
            !webPages.TryGetProperty("value", out var results))
        {
            return "Weather information not found.";
        }
        foreach (var result in results.EnumerateArray())
        {
            if (result.TryGetProperty("snippet", out var snippetElem))
            {
                var snippet = snippetElem.GetString();
                if (!string.IsNullOrEmpty(snippet) &&
                    (snippet.Contains("weather", StringComparison.OrdinalIgnoreCase) ||
                     snippet.Contains("temperature", StringComparison.OrdinalIgnoreCase) ||
                     snippet.Contains("forecast", StringComparison.OrdinalIgnoreCase)))
                {
                    return snippet;
                }
            }
        }
        // fallback: return first snippet if available
        if (results.GetArrayLength() > 0 && results[0].TryGetProperty("snippet", out var firstSnippet))
        {
            return firstSnippet.GetString() ?? "Weather information not found.";
        }
        return "Weather information not found.";
    }
}
