using Azure;
using Microsoft.SemanticKernel;
using OpenAI.Chat;
using OpenAI;
using System.ComponentModel;
using SemanticKernel.AIAgentBackend.Repositories;
using Microsoft.OpenApi.Services;

namespace SemanticKernel.AIAgentBackend.plugins.NativePlugin
{
    public class WeatherPlugin
    {
        private readonly HttpClient _httpClient;
        private readonly Kernel _kernel;
        private readonly IConfiguration _configuration;

        public WeatherPlugin([FromKeyedServices("LLMKernel")] Kernel kernel, IConfiguration configuration)
        {
            _httpClient = new HttpClient();
            _kernel = kernel;
            _configuration = configuration;
        }

        [KernelFunction("GetWeather"), Description("Gets the weather information for a given city.")]
        public async Task<string> GetWeatherAsync([Description("Query of User")] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return "Please provide a valid query.";
            }

            string apikey = _configuration["WeatherAPI:ApiKey"]!;

            string basePath = AppContext.BaseDirectory;  // Get base directory
            string pluginPath = Path.Combine(basePath, "plugins", "WeatherExtractPlugin");

            var weatherExtractPlugin = _kernel.ImportPluginFromPromptDirectory(pluginPath, "WeatherExtractPlugin");

            var extractCityPlugin = weatherExtractPlugin["ExtractCity"];

            var arguments = new KernelArguments()
            {
                ["query"] = query
            };

            string city = (await _kernel.InvokeAsync(extractCityPlugin, arguments)).ToString();

            string url = $"http://api.weatherstack.com/current?access_key={apikey}&query={city}";

            try
            {
                var response = await _httpClient.GetFromJsonAsync<WeatherResponse>(url);
                if (response == null || response.Current == null || response.Location == null)
                {
                    return $"Could not retrieve weather data for {query}.";
                }

                var weatherInfo = response.Current;
                var weatherDescription = weatherInfo.WeatherDescriptions?.FirstOrDefault() ?? "No description available";
                var weatherIcon = weatherInfo.WeatherIcons?.FirstOrDefault() ?? "";

                var weatherResponse = $@"
                        🌍 Location: {response.Location.Name}, {response.Location.Country}
                        🌤️ Weather: {weatherDescription}
                        🌡️ Temperature: {weatherInfo.Temperature}°C (Feels like {weatherInfo.FeelsLike}°C)
                        💨 Wind: {weatherInfo.WindSpeed} km/h {weatherInfo.WindDir}
                        💧 Humidity: {weatherInfo.Humidity}%
                        🌞 UV Index: {weatherInfo.UvIndex}
                ";

                // Generate semantic response
                var semanticFunction = _kernel.CreateFunctionFromPrompt(
                    "Based on the user query {{$query}}, generate a concise and informative response not more than 50 words using the following search results:\n\n {{$searchResult}} \n\n Ensure the response is clear, engaging, and to the point. If no relevant answer is found, politely acknowledge it by stating that you don't know."
                );

                var finalResponse = await _kernel.InvokeAsync(semanticFunction, new() { ["searchResult"] = weatherResponse.ToString(), ["query"] = query });

                return finalResponse.ToString();
            }

            catch (Exception ex)
            {
                return $"Error retrieving weather data: {ex.Message}";
            }
        }
    }

    // Weather API Response Classes
    public class WeatherResponse
    {
        public WeatherRequest? Request { get; set; }
        public WeatherLocation? Location { get; set; }
        public WeatherCurrent? Current { get; set; }
    }

    public class WeatherRequest
    {
        public string? Type { get; set; }
        public string? Query { get; set; }
    }

    public class WeatherLocation
    {
        public string? Name { get; set; }
        public string? Country { get; set; }
        public string? Region { get; set; }
        public string? LocalTime { get; set; }
    }

    public class WeatherCurrent
    {
        public string? ObservationTime { get; set; }
        public int Temperature { get; set; }
        public int FeelsLike { get; set; }
        public int WindSpeed { get; set; }
        public int WindDegree { get; set; }
        public string? WindDir { get; set; }
        public int Pressure { get; set; }
        public int Humidity { get; set; }
        public int CloudCover { get; set; }
        public int UvIndex { get; set; }
        public int Visibility { get; set; }
        public string[]? WeatherDescriptions { get; set; }
        public string[]? WeatherIcons { get; set; }
    }
}
