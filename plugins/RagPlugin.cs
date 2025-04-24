using Microsoft.SemanticKernel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;

public class RagPlugin
{
    private readonly string _searchEndpoint;
    private readonly string _searchApiKey;
    private readonly string _searchIndexName;
    private readonly string _openAiEndpoint;
    private readonly string _openAiApiKey;
    private readonly string _openAiDeployment;
    private readonly HttpClient _httpClient;

    public RagPlugin()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        _searchEndpoint = config["AzureCognitiveSearch:Endpoint"] ?? throw new InvalidOperationException("AzureCognitiveSearch:Endpoint is missing in appsettings.json");
        _searchApiKey = config["AzureCognitiveSearch:ApiKey"] ?? throw new InvalidOperationException("AzureCognitiveSearch:ApiKey is missing in appsettings.json");
        _searchIndexName = config["AzureCognitiveSearch:IndexName"] ?? throw new InvalidOperationException("AzureCognitiveSearch:IndexName is missing in appsettings.json");
        _openAiEndpoint = config["OpenAI:Endpoint"] ?? throw new InvalidOperationException("OpenAI:Endpoint is missing in appsettings.json");
        _openAiApiKey = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey is missing in appsettings.json");
        _openAiDeployment = config["OpenAI:DeploymentName"] ?? throw new InvalidOperationException("OpenAI:DeploymentName is missing in appsettings.json");
        _httpClient = new HttpClient();
    }

    [KernelFunction]
    public async Task<string> RetrieveAsync(string query)
    {
        var url = $"{_searchEndpoint}/indexes/{_searchIndexName}/docs/search?api-version=2023-07-01-preview";
        var payload = new
        {
            search = query,
            top = 3
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("api-key", _searchApiKey);
        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var results = new List<string>();
        foreach (var result in doc.RootElement.GetProperty("value").EnumerateArray())
        {
            if (result.TryGetProperty("content", out var contentProp))
            {
                var value = contentProp.GetString();
                if (!string.IsNullOrEmpty(value))
                    results.Add(value);
            }
        }
        return results.Count > 0 ? string.Join("\n---\n", results) : string.Empty;
    }

    [KernelFunction]
    public async Task<string> GenerateAsync(string question, string context)
    {
        var url = $"{_openAiEndpoint}/openai/deployments/{_openAiDeployment}/chat/completions?api-version=2024-02-15-preview";
        var messages = new[]
        {
            new { role = "system", content = "You are a helpful assistant that uses the provided context to answer questions." },
            new { role = "user", content = $"Context: {context}\n\nQuestion: {question}" }
        };
        var payload = new
        {
            messages = messages,
            max_tokens = 256,
            temperature = 0.2
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);
        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var answer = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return answer ?? string.Empty;
    }
}
