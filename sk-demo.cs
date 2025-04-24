// Semantic Kernel C# example

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.Extensions.Configuration;

// Load configuration
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
var bingApiKey = config["BingSearch:ApiKey"];
if (string.IsNullOrWhiteSpace(bingApiKey))
{
    throw new InvalidOperationException("BingSearch:ApiKey is missing or empty in appsettings.json. Please provide your Bing Search API key.");
}

var openAIApiKey = config["OpenAI:ApiKey"];
var openAIEndpoint = config["OpenAI:Endpoint"];
var openAIDeployment = config["OpenAI:DeploymentName"];
if (string.IsNullOrWhiteSpace(openAIApiKey) || string.IsNullOrWhiteSpace(openAIEndpoint) || string.IsNullOrWhiteSpace(openAIDeployment))
{
    throw new InvalidOperationException("OpenAI:ApiKey, OpenAI:Endpoint, or OpenAI:DeploymentName is missing or empty in appsettings.json. Please provide your OpenAI settings.");
}

ChatHistory chatHistory = [];

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.AddAzureOpenAIChatCompletion(
    deploymentName: openAIDeployment,
    apiKey: openAIApiKey,
    endpoint: openAIEndpoint
);
kernelBuilder.Plugins.AddFromType<BookTravelPlugin>("BookTravel");
kernelBuilder.Plugins.AddFromType<WeatherPlugin>("Weather");

var kernel = kernelBuilder.Build();

var settings = new AzureOpenAIPromptExecutionSettings()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

while (true)
{
    Console.Write("You: ");
    var userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput) || userInput.ToLower() == "exit")
        break;
    chatHistory.AddUserMessage(userInput);

    var response = await chatCompletion.GetChatMessageContentAsync(chatHistory, settings, kernel);
    Console.WriteLine($"AI: {response.Content}");
    chatHistory.AddMessage(response!.Role, response!.Content!);
}