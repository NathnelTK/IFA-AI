using System;
using System.Threading.Tasks;
using IFA.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IFA.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoiceController : BaseApiController
    {
        private readonly IVoxService _voxService;

        public VoiceController(IVoxService voxService)
        {
            _voxService = voxService;
        }

        public class VoiceIntentRequest
        {
            public string Transcript { get; set; } = string.Empty;
            public string Language { get; set; } = "en";
        }

        [HttpPost("intent")]
        public async Task<IActionResult> ProcessVoiceIntent([FromBody] VoiceIntentRequest request)
        {
            var result = await _voxService.RecognizeIntentAsync(request.Transcript, request.Language);
            return Ok(result);
        }
    }
}
