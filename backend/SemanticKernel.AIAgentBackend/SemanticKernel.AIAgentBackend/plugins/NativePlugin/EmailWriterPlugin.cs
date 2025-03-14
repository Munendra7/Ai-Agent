using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Net.Mail;
using System.Text;

namespace SemanticKernel.AIAgentBackend.plugins.NativePlugin
{
    public class EmailWriterPlugin
    {
        private readonly Kernel kernel;
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;

        public EmailWriterPlugin([FromKeyedServices("LLMKernel")] Kernel kernel, HttpClient httpClient, IConfiguration configuration)
        {
            this.kernel = kernel;
            this.httpClient = httpClient;
            this.configuration = configuration;
        }

        [KernelFunction("SendEmail"), Description("Prepares an email for sending. Always confirm the email body with the user before sending.")]
        public async Task<string> SendEmailAsync(string to, string body, bool confirmAndSend = false)
        {
            if (!confirmAndSend)
            {
                return $"Preview Email:\nTo: {to}\nBody: {body}\n\nPlease confirm before sending.";
            }

            var emailRequest = new { to, emailBody = body };
            var response = await httpClient.PostAsync(configuration["AgentEmailUrl_PowerAutomate"]!,
                new StringContent(JsonConvert.SerializeObject(emailRequest), Encoding.UTF8, "application/json"));

            return response.IsSuccessStatusCode ? "Email sent successfully!" : "Failed to send email.";
        }
    }
}