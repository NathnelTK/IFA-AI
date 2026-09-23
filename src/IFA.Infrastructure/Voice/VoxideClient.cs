using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;

namespace IFA.Infrastructure.Voice
{
    /// <summary>
    /// Voxide Voice Engine integration.
    ///
    /// =========================================================================
    /// ASSIGNED TO VOICE & BACKEND TEAM
    /// =========================================================================
    /// RESPONSIBILITY:
    /// Provide low-latency speech-to-text, intent extraction, and text-to-speech
    /// audio synthesis using the Voxide engine (supporting English and Amharic).
    ///
    /// STEP-BY-STEP IMPLEMENTATION INSTRUCTIONS:
    /// 1. Configure "Voxide:Endpoint" and "Voxide:ApiKey" in appsettings.json.
    /// 2. RecognizeIntentAsync:
    ///    - If audioBase64OrTranscript is audio bytes (base64 wav/ogg):
    ///      Post to {Voxide:Endpoint}/v1/stt/transcribe to get the transcript text.
    ///    - Pass the transcribed text into intent classification logic:
    ///      * "learn X" / "study X" -> START_COURSE (TargetTopic = X)
    ///      * "skills" / "profile" -> NAVIGATE_SKILL_PROFILE
    ///      * "explain X" / "what is X" -> EXPLAIN_TOPIC (TargetTopic = X)
    ///      * "next" / "continue" -> NEXT_MODULE
    /// 3. SynthesizeSpeechAsync:
    ///    - Post text to {Voxide:Endpoint}/v1/tts/synthesize with voice and language.
    ///    - Return raw PCM/WAV byte array for the client audio player.
    /// 4. Resilient Fallback:
    ///    - When Voxide is offline or unconfigured, parse textual transcripts
    ///      heuristically so local voice testing and demo runs always succeed.
    /// =========================================================================
    /// </summary>
    public class VoxideClient : IVoxService
    {
        public Task<VoiceIntentResult> RecognizeIntentAsync(
            string audioBase64OrTranscript,
            string language = "en",
            CancellationToken cancellationToken = default)
        {
            // TODO (Voice Team): Wire live Voxide STT endpoint here.
            // Heuristic fallback for demo and transcript testing:
            var input = audioBase64OrTranscript.Trim();
            var lower = input.ToLower();

            var result = new VoiceIntentResult
            {
                RawTranscript = input,
                Language = language,
                Confidence = 0.95
            };

            if (lower.Contains("skill") || lower.Contains("profile"))
            {
                result.IntentName = "NAVIGATE_SKILL_PROFILE";
                result.TargetTopic = "Skills";
            }
            else if (lower.Contains("explain") || lower.Contains("what is"))
            {
                result.IntentName = "EXPLAIN_TOPIC";
                result.TargetTopic = Regex.Replace(input, "(?i)explain|what is", "").Trim();
            }
            else if (lower.Contains("next") || lower.Contains("continue"))
            {
                result.IntentName = "NEXT_MODULE";
                result.TargetTopic = "Next";
            }
            else
            {
                result.IntentName = "START_COURSE";
                var match = Regex.Match(input, @"(?i)(?:learn|study|course on|explore)\s+([a-zA-Z0-9#+.\s]+)");
                result.TargetTopic = match.Success ? match.Groups[1].Value.Trim() : input;
            }

            return Task.FromResult(result);
        }

        public Task<byte[]> SynthesizeSpeechAsync(
            string text,
            string language = "en",
            CancellationToken cancellationToken = default)
        {
            // TODO (Voice Team): Wire live Voxide TTS streaming endpoint.
            // Return empty audio payload as safe fallback for now.
            return Task.FromResult(Array.Empty<byte>());
        }
    }
}
