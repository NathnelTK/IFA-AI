using System.Threading;
using System.Threading.Tasks;
using IFA.Application.Voice.Commands;
using Microsoft.AspNetCore.Mvc;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoiceController : ControllerBase
    {
        private readonly ParseVoiceIntentCommandHandler _intentHandler;

        public VoiceController(ParseVoiceIntentCommandHandler intentHandler)
        {
            _intentHandler = intentHandler;
        }

        [HttpPost("intent")]
        public async Task<IActionResult> ParseIntent(
            [FromBody] ParseVoiceIntentCommand command,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.AudioBase64OrTranscript))
            {
                return BadRequest(new { error = "AudioBase64OrTranscript payload is required." });
            }

            var result = await _intentHandler.HandleAsync(command, cancellationToken);
            return Ok(result);
        }
    }
}
