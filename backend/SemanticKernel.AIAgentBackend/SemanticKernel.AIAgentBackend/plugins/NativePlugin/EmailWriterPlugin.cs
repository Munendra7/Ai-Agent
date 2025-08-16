using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Text;

namespace SemanticKernel.AIAgentBackend.plugins.NativePlugin
{
    public class EmailWriterPlugin
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;

        public EmailWriterPlugin(HttpClient httpClient, IConfiguration configuration)
        {
            this.httpClient = httpClient;
            this.configuration = configuration;
        }

        [KernelFunction("SendEmail"), Description("Always take user consent before sending email, Prepares an email for sending. Make sure HTML is passed instead of markdown")]
        public async Task<string> SendEmailAsync(string to, string subject, string body)
        {
            var emailRequest = new { to,subject, emailBody = body };
            var response = await httpClient.PostAsync(configuration["AgentEmailUrl_PowerAutomate"]!,
                new StringContent(JsonConvert.SerializeObject(emailRequest), Encoding.UTF8, "application/json"));

            return response.IsSuccessStatusCode ? "Email sent successfully!" : "Failed to send email.";
        }
    }
}