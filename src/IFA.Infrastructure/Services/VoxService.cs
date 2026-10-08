using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;

namespace IFA.Infrastructure.Services
{
    public class VoxService : IVoxService
    {
        public Task<VoiceIntentResult> RecognizeIntentAsync(string audioBase64OrTranscript, string language = "en", CancellationToken cancellationToken = default)
        {
            var text = audioBase64OrTranscript?.Trim() ?? string.Empty;
            var lower = text.ToLowerInvariant();

            string intent = "EXPLAIN_TOPIC";
            string targetTopic = text;
            double confidence = 0.95;

            if (lower.Contains("start") || lower.Contains("create") || lower.Contains("learn"))
            {
                intent = "START_COURSE";
                targetTopic = Regex.Replace(text, "(?i)^(please|i want to|can you|start|create|learn about|learn)", "").Trim();
            }
            else if (lower.Contains("next") || lower.Contains("continue") || lower.Contains("proceed"))
            {
                intent = "NEXT_LESSON";
            }
            else if (lower.Contains("quiz") || lower.Contains("test") || lower.Contains("assessment"))
            {
                intent = "START_QUIZ";
            }
            else if (lower.Contains("skill") || lower.Contains("profile") || lower.Contains("gap"))
            {
                intent = "NAVIGATE_SKILL_PROFILE";
            }
            else if (lower.Contains("tutor") || lower.Contains("help") || lower.Contains("ask"))
            {
                intent = "OPEN_TUTOR";
                targetTopic = text;
            }

            return Task.FromResult(new VoiceIntentResult
            {
                RawTranscript = text,
                IntentName = intent,
                TargetTopic = targetTopic,
                Confidence = confidence,
                Language = language
            });
        }

        public Task<byte[]> SynthesizeSpeechAsync(string text, string language = "en", CancellationToken cancellationToken = default)
        {
            // IFA client uses browser Web Speech API for native real-time audio playback
            return Task.FromResult(Array.Empty<byte>());
        }
    }
}
