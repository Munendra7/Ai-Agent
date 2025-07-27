using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using SemanticKernel.AIAgentBackend.Factories.Interface;
using SemanticKernel.AIAgentBackend.Repositories.Interface;
using System.Text;

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
                audioStream.Position = 0; // reset stream before reusing
                var transcription = await TranscribeAudioStreamAsync(audioStream);

                return _documentsProcessFactory.ChunkText(transcription, 1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Video processing failed for: {InputFile}", fileName);
                throw;
            }
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