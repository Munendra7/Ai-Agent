using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;
using HtmlAgilityPack; // Add NuGet package: HtmlAgilityPack

namespace SemanticKernel.AIAgentBackend.plugins.NativePlugin
{
    public class GoogleSearchPlugin
    {
        private readonly HttpClient _httpClient;
        private readonly Kernel _kernel;
        private readonly IConfiguration _configuration;

        // Configuration constants
        private const int MAX_CONTENT_LENGTH = 5000;
        private const int TIMEOUT_SECONDS = 10;

        public GoogleSearchPlugin([FromKeyedServices("LLMKernel")] Kernel kernel, IConfiguration configuration)
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(TIMEOUT_SECONDS)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

            _kernel = kernel;
            _configuration = configuration;
        }

        [KernelFunction("search"), Description("Performs advanced web search with content extraction and intelligent summarization")]
        public async Task<string> SearchAsync(
            [Description("Search query")] string query,
            [Description("Number of results to process (1-10)")] int numResults = 3,
            [Description("Include page content extraction")] bool extractContent = true)
        {
            try
            {
                // Validate inputs
                numResults = Math.Clamp(numResults, 1, 10);

                // Perform Google search
                var searchResults = await PerformGoogleSearchAsync(query, numResults);

                if (!searchResults.Any())
                {
                    return "No search results found for your query.";
                }

                // Extract content from pages if requested
                if (extractContent)
                {
                    await EnrichResultsWithContentAsync(searchResults);
                }

                // Generate comprehensive response
                return await GenerateIntelligentResponseAsync(query, searchResults);
            }
            catch (Exception ex)
            {
                return $"An error occurred while searching. Please try again." + ex.Message;
            }
        }

        [KernelFunction("search_with_filter"), Description("Search with specific filters like date range, site, or file type")]
        public async Task<string> SearchWithFilterAsync(
            [Description("Search query")] string query,
            [Description("Site to search within (e.g., 'reddit.com')")] string site = "",
            [Description("File type filter (e.g., 'pdf', 'doc')")] string fileType = "",
            [Description("Date range: 'd' (day), 'w' (week), 'm' (month), 'y' (year)")] string dateRange = "")
        {
            var filteredQuery = BuildFilteredQuery(query, site, fileType, dateRange);
            return await SearchAsync(filteredQuery, 3, true);
        }

        [KernelFunction("fact_check"), Description("Fact-check a claim by searching multiple sources")]
        public async Task<string> FactCheckAsync([Description("Claim to verify")] string claim)
        {
            var searchResults = await PerformGoogleSearchAsync($"fact check {claim}", 5);

            if (!searchResults.Any())
            {
                return "Unable to find relevant sources for fact-checking this claim.";
            }

            await EnrichResultsWithContentAsync(searchResults);

            var factCheckPrompt = @"
                Analyze the following search results to fact-check this claim: '{{$claim}}'
                
                Search Results:
                {{$results}}
                
                Provide a fact-check analysis that includes:
                1. Verdict: TRUE, FALSE, PARTIALLY TRUE, or UNVERIFIED
                2. Key evidence supporting or refuting the claim
                3. Reliability assessment of sources
                4. Any important context or nuance
                
                Keep the response concise but thorough.";

            var semanticFunction = _kernel.CreateFunctionFromPrompt(factCheckPrompt);
            var response = await _kernel.InvokeAsync(semanticFunction, new()
            {
                ["claim"] = claim,
                ["results"] = FormatSearchResults(searchResults)
            });

            return response.ToString();
        }

        private async Task<List<SearchResult>> PerformGoogleSearchAsync(string query, int numResults)
        {
            var apiKey = _configuration["GoogleSearch:ApiKey"]
                ?? throw new InvalidOperationException("Google API key not configured");
            var searchEngineId = _configuration["GoogleSearch:SearchEngineId"]
                ?? throw new InvalidOperationException("Search Engine ID not configured");

            var apiUrl = $"https://www.googleapis.com/customsearch/v1?" +
                        $"q={Uri.EscapeDataString(query)}" +
                        $"&key={apiKey}" +
                        $"&cx={searchEngineId}" +
                        $"&num={Math.Min(numResults, 10)}";

            var response = await _httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                return new List<SearchResult>();
            }

            var jsonResult = await response.Content.ReadAsStringAsync();
            return ParseSearchResults(jsonResult);
        }

        private List<SearchResult> ParseSearchResults(string jsonResult)
        {
            var results = new List<SearchResult>();

            try
            {
                using var doc = JsonDocument.Parse(jsonResult);
                var root = doc.RootElement;

                if (root.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        var result = new SearchResult
                        {
                            Title = item.GetProperty("title").GetString() ?? "",
                            Link = item.GetProperty("link").GetString() ?? "",
                            Snippet = item.GetProperty("snippet").GetString() ?? ""
                        };

                        // Extract additional metadata if available
                        if (item.TryGetProperty("pagemap", out var pagemap))
                        {
                            if (pagemap.TryGetProperty("metatags", out var metatags) &&
                                metatags.GetArrayLength() > 0)
                            {
                                var firstMetatag = metatags[0];

                                if (firstMetatag.TryGetProperty("og:description", out var desc))
                                    result.Description = desc.GetString();

                                if (firstMetatag.TryGetProperty("article:published_time", out var pubTime))
                                    result.PublishedDate = pubTime.GetString();
                            }
                        }

                        results.Add(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex + "Error parsing search results");
            }

            return results;
        }

        private async Task EnrichResultsWithContentAsync(List<SearchResult> results)
        {
            var tasks = results.Select(async result =>
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(result.Link)) // Ensure Link is not null or empty  
                    {
                        result.PageContent = await ExtractPageContentAsync(result.Link);
                    }
                    else
                    {
                        result.PageContent = result.Snippet; // Fallback to snippet  
                    }
                }
                catch (Exception)
                {
                    result.PageContent = result.Snippet; // Fallback to snippet  
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task<string> ExtractPageContentAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return "";

                var html = await response.Content.ReadAsStringAsync();

                // Use HtmlAgilityPack to parse HTML
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Remove script and style elements
                doc.DocumentNode.Descendants()
                    .Where(n => n.Name == "script" || n.Name == "style")
                    .ToList()
                    .ForEach(n => n.Remove());

                // Extract text from common content areas
                var contentSelectors = new[]
                {
                    "//article",
                    "//main",
                    "//div[@class='content']",
                    "//div[@id='content']",
                    "//div[contains(@class, 'post')]",
                    "//div[contains(@class, 'entry')]",
                    "//p"
                };

                var contentBuilder = new StringBuilder();

                foreach (var selector in contentSelectors)
                {
                    var nodes = doc.DocumentNode.SelectNodes(selector);
                    if (nodes != null)
                    {
                        foreach (var node in nodes.Take(5)) // Limit nodes per selector
                        {
                            var text = CleanText(node.InnerText);
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                contentBuilder.AppendLine(text);

                                if (contentBuilder.Length > MAX_CONTENT_LENGTH)
                                    break;
                            }
                        }
                    }

                    if (contentBuilder.Length > MAX_CONTENT_LENGTH)
                        break;
                }

                var content = contentBuilder.ToString();

                if (content.Length > MAX_CONTENT_LENGTH)
                {
                    content = content.Substring(0, MAX_CONTENT_LENGTH) + "...";
                }

                return content;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private string CleanText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            // Decode HTML entities
            text = System.Net.WebUtility.HtmlDecode(text);

            // Remove excessive whitespace
            text = Regex.Replace(text, @"\s+", " ");

            // Remove special characters that might break formatting
            text = Regex.Replace(text, @"[\r\n\t]+", " ");

            return text.Trim();
        }

        private async Task<string> GenerateIntelligentResponseAsync(string query, List<SearchResult> results)
        {
            var formattedResults = FormatSearchResults(results);

            var promptTemplate = @"
                You are a highly intelligent search assistant. Based on the user's query and the search results with extracted content, 
                provide a comprehensive, well-structured response.
                
                User Query: {{$query}}
                
                Search Results:
                {{$results}}
                
                Instructions:
                1. Synthesize information from multiple sources
                2. Identify key facts, trends, and insights
                3. Note any conflicting information between sources
                4. Provide a balanced, objective summary
                5. Include relevant details but stay concise
                6. Cite sources when making specific claims [Source: Title]
                7. If the results don't fully answer the query, acknowledge what's missing
                
                Format your response with:
                - A brief direct answer to the query (1-2 sentences)
                - Key findings organized by theme or importance
                - Any important caveats or conflicting information
                - Relevant sources for further reading
                
                Keep the total response under 2000 words unless the complexity demands more detail.";

            var semanticFunction = _kernel.CreateFunctionFromPrompt(promptTemplate);

            var response = await _kernel.InvokeAsync(semanticFunction, new()
            {
                ["query"] = query,
                ["results"] = formattedResults
            });

            return response.ToString();
        }

        private string FormatSearchResults(List<SearchResult> results)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                sb.AppendLine($"[{i + 1}] Title: {result.Title}");
                sb.AppendLine($"    URL: {result.Link}");
                sb.AppendLine($"    Snippet: {result.Snippet}");

                if (!string.IsNullOrWhiteSpace(result.PageContent))
                {
                    sb.AppendLine($"    Content: {result.PageContent}");
                }

                if (!string.IsNullOrWhiteSpace(result.PublishedDate))
                {
                    sb.AppendLine($"    Published: {result.PublishedDate}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        private string BuildFilteredQuery(string query, string site, string fileType, string dateRange)
        {
            var queryBuilder = new StringBuilder(query);

            if (!string.IsNullOrWhiteSpace(site))
            {
                queryBuilder.Append($" site:{site}");
            }

            if (!string.IsNullOrWhiteSpace(fileType))
            {
                queryBuilder.Append($" filetype:{fileType}");
            }

            if (!string.IsNullOrWhiteSpace(dateRange))
            {
                queryBuilder.Append($" after:{GetDateRangeString(dateRange)}");
            }

            return queryBuilder.ToString();
        }

        private string GetDateRangeString(string dateRange)
        {
            var date = DateTime.Now;

            return dateRange?.ToLower() switch
            {
                "d" => date.AddDays(-1).ToString("yyyy-MM-dd"),
                "w" => date.AddDays(-7).ToString("yyyy-MM-dd"),
                "m" => date.AddMonths(-1).ToString("yyyy-MM-dd"),
                "y" => date.AddYears(-1).ToString("yyyy-MM-dd"),
                _ => date.AddMonths(-1).ToString("yyyy-MM-dd")
            };
        }

        // Internal class for search results
        private class SearchResult
        {
            public string? Title { get; set; }
            public string? Link { get; set; }
            public string? Snippet { get; set; }
            public string? Description { get; set; }
            public string? PageContent { get; set; }
            public string? PublishedDate { get; set; }
        }
    }
}