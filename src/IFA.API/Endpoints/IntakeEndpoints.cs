

using IFA.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Endpoints
{


    public record ChatMessageRequest(string Message);
    public static class IntakeEndpoints
    {
        public static void MapIntakeEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/learners/{learnerId:guid}/intake").WithTags("Intake");
            group.MapPost("/sessions", StartSession);
            group.MapPost("/sessions/{sessionId:guid}/messages", SendMessage);

            group.MapPost("/sessions/{sessionId:guid}/finalize", FinalizeSession);
        }

        public static async Task<IResult> StartSession(Guid learnerId, IApplicationDbContext db, CancellationToken ct)
        {
            var session = new ChatSession { LearnerId = learnerId };
            var opening = new ChatMessage
            {
                ChatSessionId = session.Id,
                Role = ChatRole.Assistant,
                Content = "Hi! What do you want to learn?",
                SequenceNumber = 0
            };

            db.Add(session);
            db.Add(opening);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/learners/{learnerId}/intake/sessions/{session.Id}", new { session.Id, opening.Content });
        }

        public static async Task<IResult> SendMessage(
            Guid learnerId, Guid sessionId, ChatMessageRequest request, IApplicationDbContext db, IUnderstandingAgentService agentService, CancellationToken ct
        )
        {
            var session = await db.ChatSessions.Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.LearnerId == learnerId, ct);
            if (session is null) return Results.NotFound();

            if (session.Status == ChatSessionStatus.Finalized) return Results.Conflict("Session already finalized.");
            // ask first while seeion.messages does not yet contain the new message.
            var reply = await agentService.ContinueConversationAsync(session, request.Message, ct);

            // the save both messages.
            var nextSeq = session.Messages.Count;
            db.Add(new ChatMessage { ChatSessionId = sessionId, Role = ChatRole.User, Content = request.Message, SequenceNumber = nextSeq });
            db.Add(new ChatMessage { ChatSessionId = sessionId, Role = ChatRole.Assistant, Content = reply, SequenceNumber = nextSeq + 1 });


            // var replyMsg = new ChatMessage { ChatSessionId = sessionId, Role = ChatRole.Assistant, Content = reply, SequenceNumber = nextSeq + 1 };
            // db.Add(replyMsg);

            await db.SaveChangesAsync(ct);

            return Results.Ok(new { reply });
        }

        public static async Task<IResult> FinalizeSession(
            Guid learnerId, Guid sessionId, IUnderstandingAgentService agentService, IApplicationDbContext db, CancellationToken ct
        )
        {
            var session = await db.ChatSessions.Include(s => s.Messages).FirstOrDefaultAsync(s => s.Id == sessionId && s.LearnerId == learnerId, ct);


            if (session is null) return Results.NotFound();

            var profile = await agentService.ExtractProfileAsync(session, ct);
            if (profile is null)
                return Results.UnprocessableEntity("Couldn't extract a clear enough profile yet - try adding a bit more detail to the conversation.");

            db.Add(profile);
            session.Status = ChatSessionStatus.Finalized;
            session.ResultingLearnerProfileId = profile.Id;
            await db.SaveChangesAsync(ct);

            return Results.Ok(profile);
        }
    }
}