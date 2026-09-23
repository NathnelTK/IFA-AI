using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;

namespace IFA.Application.Voice.Commands
{
    public class ParseVoiceIntentCommand
    {
        public string AudioBase64OrTranscript { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
    }

    public class ParseVoiceIntentCommandHandler
    {
        private readonly IVoxService _voxService;

        public ParseVoiceIntentCommandHandler(IVoxService voxService)
        {
            _voxService = voxService;
        }

        public async Task<VoiceIntentResult> HandleAsync(
            ParseVoiceIntentCommand command,
            CancellationToken cancellationToken = default)
        {
            return await _voxService.RecognizeIntentAsync(
                command.AudioBase64OrTranscript,
                command.Language,
                cancellationToken);
        }
    }
}
