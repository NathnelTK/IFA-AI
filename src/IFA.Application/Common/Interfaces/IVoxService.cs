using System.Threading;
using System.Threading.Tasks;

namespace IFA.Application.Common.Interfaces
{
    public class VoiceIntentResult
    {
        public string RawTranscript { get; set; } = string.Empty;
        public string IntentName { get; set; } = string.Empty; // e.g., "NAVIGATE_SKILL_PROFILE", "START_COURSE", "EXPLAIN_TOPIC"
        public string TargetTopic { get; set; } = string.Empty;
        public double Confidence { get; set; } = 0.95;
        public string Language { get; set; } = "en"; // "en", "am" (Amharic)
    }

    public interface IVoxService
    {
        Task<VoiceIntentResult> RecognizeIntentAsync(string audioBase64OrTranscript, string language = "en", CancellationToken cancellationToken = default);
        Task<byte[]> SynthesizeSpeechAsync(string text, string language = "en", CancellationToken cancellationToken = default);
    }
}
