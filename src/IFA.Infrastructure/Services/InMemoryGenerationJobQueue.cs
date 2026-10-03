using System.Threading.Channels;
using IFA.Application.Common.Interfaces;

namespace IFA.Infrastructure.Services
{
    // In-memory only - lost on restart. Crash safety comes from the
    // worker's startup recovery scan (Step 5), not from this queue.
    public class InMemoryGenerationJobQueue : IGenerationJobQueue
    {
        private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

        public async Task EnqueueAsync(Guid jobId, CancellationToken ct = default) =>
            await _channel.Writer.WriteAsync(jobId, ct);

        public async Task<Guid> DequeueAsync(CancellationToken ct) =>
            await _channel.Reader.ReadAsync(ct);
    }
}