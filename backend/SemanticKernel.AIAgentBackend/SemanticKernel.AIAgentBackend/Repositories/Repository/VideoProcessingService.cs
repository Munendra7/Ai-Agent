using Azure.Core;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using System.Drawing;
using System.Text;
using System.Text.Json;

namespace SemanticKernel.AIAgentBackend.Repositories.Repository
{
    public class VideoProcessingService : IVideoProcessingService
    {
        private readonly IBlobService _blobService;
        private readonly ILogger<VideoProcessingService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IDocumentsProcessFactory _documentsProcessFactory;
        private readonly IConfiguration _configuration;

        public VideoProcessingService(IBlobService blobService, ILogger<VideoProcessingService> logger, IDocumentsProcessFactory documentsProcessFactory, IConfiguration configuration)
        {
            _blobService = blobService;
            _logger = logger;
            _httpClient = new HttpClient();
            _documentsProcessFactory = documentsProcessFactory;
            _configuration = configuration;
        }

        public async Task<IEnumerable<string>> ProcessVideo(string fileName)
        {
            string FfmpegServiceUrl = _configuration["VideoToAudioService:EndPoint"]!;

            bool useBatch = bool.TryParse(_configuration["SpeechToTextService:UseBatchTranscription"], out var flag) && flag;

            string fileId = Guid.NewGuid().ToString("N");
            string videoFileName = $"{fileId}_{Path.GetFileName(fileName)}";
            string audioFileName = Path.GetFileNameWithoutExtension(videoFileName) + ".wav";

            try
            {
                // 1. Download video stream from blob
                var (videoStream, _) = await _blobService.DownloadFileAsync(
                    fileName, Constants.BlobStorageConstants.KnowledgeContainerName);

                _logger.LogInformation("📥 Streaming video from blob: {BlobFile}", fileName);

                // 2. Convert video to audio
                using var formData = new MultipartFormDataContent();
                var streamContent = new StreamContent(videoStream);
                formData.Add(streamContent, "file", videoFileName);

                var response = await _httpClient.PostAsync(FfmpegServiceUrl, formData);
                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    _logger.LogError("❌ FFmpeg failed: {Status} - {Error}", response.StatusCode, errorText);
                    throw new Exception($"FFmpeg service error: {errorText}");
                }

                _logger.LogInformation("🎧 Audio conversion succeeded for: {File}", videoFileName);

                await using var audioStream = await response.Content.ReadAsStreamAsync();

                // 3. Upload audio stream (optional)
                await _blobService.UploadFileAsync(
                    audioStream, audioFileName, Constants.BlobStorageConstants.ExtractedAudioContainerName);

                _logger.LogInformation("✅ Audio uploaded to blob: {AudioFile}", audioFileName);

                // 4. Transcribe audio stream directly using Azure Speech SDK
                //audioStream.Position = 0; // reset stream before reusing
                //var transcription = await TranscribeAudioStreamAsync(audioStream);

                // 4. Transcribe audio stream directly using Azure Speech SDK or Batch API
                string transcription;
                if (useBatch)
                {
                    var audioSas = _blobService.GenerateSasUri(audioFileName, Constants.BlobStorageConstants.ExtractedAudioContainerName);
                    string jobUrl = await SubmitBatchTranscriptionAsync(audioSas, audioFileName);
                    string resultUrl = await PollTranscriptionResultAsync(jobUrl);
                    transcription = await DownloadTranscriptTextAsync(resultUrl);
                }
                else
                {
                    audioStream.Position = 0;
                    //transcription = await TranscribeAudioStreamAsync(audioStream);

                    transcription = await TranscribeWithFastApiAsync(audioStream, audioFileName);
                }

                return _documentsProcessFactory.ChunkText(transcription, 1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Video processing failed for: {InputFile}", fileName);
                throw;
            }
        }

        private async Task<string> SubmitBatchTranscriptionAsync(string audioSasUrl, string fileName)
        {
            string key = _configuration["SpeechToTextService:SubscriptionKey"]!;
            string region = _configuration["SpeechToTextService:Region"]!;
            string endpoint = $"https://{region}.api.cognitive.microsoft.com/speechtotext/v3.0/transcriptions";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

            var requestBody = new
            {
                contentUrls = new[] { audioSasUrl },
                properties = new
                {
                    diarizationEnabled = true,
                    wordLevelTimestampsEnabled = true,
                    punctuationMode = "DictatedAndAutomatic",
                    profanityFilterMode = "Masked"
                },
                locale = "en-US",
                displayName = $"Transcription_{fileName}"
            };

            var response = await client.PostAsJsonAsync(endpoint, requestBody);
            if (!response.IsSuccessStatusCode)
                throw new Exception(await response.Content.ReadAsStringAsync());

            // Fix for CS8602: Ensure the Location header is not null before accessing it  
            if (response.Headers.Location == null)
                throw new Exception("The response does not contain a Location header.");

            return response.Headers.Location.ToString();
        }

        private async Task<string> PollTranscriptionResultAsync(string jobUrl)
        {
            string key = _configuration["SpeechToTextService:SubscriptionKey"]!;
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", key);

            while (true)
            {
                var result = await client.GetStringAsync(jobUrl);
                var json = JsonDocument.Parse(result).RootElement;
                string status = json.GetProperty("status").GetString()!;

                if (status == "Succeeded")
                {
                    return json.GetProperty("resultsUrls").GetProperty("transcriptionFiles").GetString()!;
                }
                if (status == "Failed") throw new Exception("Transcription job failed");

                await Task.Delay(5000);
            }
        }

        private async Task<string> DownloadTranscriptTextAsync(string resultsUrl)
        {
            using var client = new HttpClient();
            var json = await client.GetStringAsync(resultsUrl);
            var builder = new StringBuilder();

            using var doc = JsonDocument.Parse(json);
            foreach (var phrase in doc.RootElement.GetProperty("combinedRecognizedPhrases").EnumerateArray())
            {
                var speaker = phrase.TryGetProperty("speaker", out var spk) ? spk.GetRawText() : "Unknown";
                var text = phrase.GetProperty("display").GetString();
                builder.AppendLine($"Speaker {speaker}: {text}");
            }

            return builder.ToString();
        }

        private async Task<string> TranscribeWithFastApiAsync(Stream audioStream, string fileName)
        {
            string region = _configuration["SpeechToTextService:Region"]!;
            string subscriptionKey = _configuration["SpeechToTextService:SubscriptionKey"]!;
            string endpoint = $"https://{region}.api.cognitive.microsoft.com/speechtotext/transcriptions:transcribe?api-version=2024-11-15";

            using var content = new MultipartFormDataContent();

            var streamContent = new StreamContent(audioStream);
            streamContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            content.Add(streamContent, "audio", fileName);

            // Example definition JSON
            var definition = new
            {
                locales = new[] { "en-US", "hi-IN" },
                profanityFilterMode = "Masked",
                channels = new[] { 0 },
                // diarizationSettings = new { minSpeakers = 1, maxSpeakers = 4 } // optional
            };
            content.Add(new StringContent(JsonSerializer.Serialize(definition), Encoding.UTF8, "application/json"), "definition");

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            client.DefaultRequestHeaders.Accept.Add(new
                System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.PostAsync(endpoint, content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Fast API error: {Status} – {Error}", response.StatusCode, error);
                throw new Exception($"Fast Transcription API error: {error}");
            }

            var result = await response.Content.ReadAsStringAsync();
            // Response includes combinedPhrases and phrases per spec
            return result;
        }

        private async Task<string> TranscribeAudioStreamAsync(Stream audioStream)
        {
            string SubscriptionKey = _configuration["SpeechToTextService:SubscriptionKey"]!;
            string Region = _configuration["SpeechToTextService:Region"]!;

            var config = SpeechConfig.FromSubscription(SubscriptionKey, Region);

            var autoDetectLangConfig = AutoDetectSourceLanguageConfig.FromLanguages(
                new string[] { "en-US", "hi-IN", "es-ES" }
            );

            var audioFormat = AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1);
            var audioInput = AudioInputStream.CreatePullStream(new BinaryAudioStreamReader(audioStream), audioFormat);

            using var recognizer = new SpeechRecognizer(config, autoDetectLangConfig, AudioConfig.FromStreamInput(audioInput));
            var fullTranscript = new StringBuilder();
            var stopRecognition = new TaskCompletionSource<int>();

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    fullTranscript.AppendLine(e.Result.Text);
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                _logger.LogError("⛔ Transcription canceled: {0}", e.ErrorDetails);
                stopRecognition.TrySetResult(0);
            };

            recognizer.SessionStopped += (s, e) =>
            {
                _logger.LogInformation("✅ Transcription session ended.");
                stopRecognition.TrySetResult(0);
            };

            _logger.LogInformation("🎙️ Starting full transcription...");

            await recognizer.StartContinuousRecognitionAsync();
            await stopRecognition.Task;
            await recognizer.StopContinuousRecognitionAsync();

            return fullTranscript.ToString();
        }


        // Helper class to wrap Stream into pull audio stream for Azure SDK
        private class BinaryAudioStreamReader : PullAudioInputStreamCallback
        {
            private readonly Stream _stream;

            public BinaryAudioStreamReader(Stream stream)
            {
                _stream = stream;
            }

            public override int Read(byte[] dataBuffer, uint size)
            {
                return _stream.Read(dataBuffer, 0, (int)size);
            }

            public override void Close()
            {
                _stream.Close();
                base.Close();
            }
        }
    }
}