

using IFA.Application.Common.Interfaces;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace IFA.API.Endpoints
{


    public record ChatMessageRequest(string Message);
    public static class IntakeEndpoints
    {
        public static void MapIntakeEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/learners/{learnerId:guid}/intake").WithTags("Intake").RequireAuthorization();
            group.MapPost("/sessions", StartSession);
            group.MapPost("/sessions/{sessionId:guid}/messages", SendMessage)
                .RequireRateLimiting("generation");

            group.MapPost("/sessions/{sessionId:guid}/finalize", FinalizeSession)
                .RequireRateLimiting("generation");
        }

        public static async Task<IResult> StartSession(Guid learnerId, ClaimsPrincipal user, IApplicationDbContext db, CancellationToken ct)
        {
            if (!IsCurrentLearner(learnerId, user)) return Results.Json(new { message = "You can only access your own intake sessions." }, statusCode: 403);
            if (!await db.Learners.AnyAsync(l => l.Id == learnerId, ct)) return Results.Unauthorized();
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
            Guid learnerId, Guid sessionId, ChatMessageRequest request, ClaimsPrincipal user, IApplicationDbContext db, IUnderstandingAgentService agentService, CancellationToken ct
        )
        {
            if (!IsCurrentLearner(learnerId, user)) return Results.Json(new { message = "You can only access your own intake sessions." }, statusCode: 403);
            if (string.IsNullOrWhiteSpace(request.Message) || request.Message.Length > 12000)
                return Results.BadRequest(new { message = "A message of 1-12000 characters is required." });
            var session = await db.ChatSessions.Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.LearnerId == learnerId, ct);
            if (session is null) return Results.NotFound();

            if (session.Status == ChatSessionStatus.Finalized) return Results.Conflict("Session already finalized.");
            var reply = await agentService.ContinueConversationAsync(session, request.Message, ct);

            var nextSeq = session.Messages.Count;
            db.Add(new ChatMessage { ChatSessionId = sessionId, Role = ChatRole.User, Content = request.Message, SequenceNumber = nextSeq });
            db.Add(new ChatMessage { ChatSessionId = sessionId, Role = ChatRole.Assistant, Content = reply, SequenceNumber = nextSeq + 1 });
            await db.SaveChangesAsync(ct);

            return Results.Ok(new { reply });
        }

        public static async Task<IResult> FinalizeSession(
            Guid learnerId, Guid sessionId, ClaimsPrincipal user, IUnderstandingAgentService agentService, IApplicationDbContext db, CancellationToken ct
        )
        {
            if (!IsCurrentLearner(learnerId, user)) return Results.Json(new { message = "You can only access your own intake sessions." }, statusCode: 403);
            var session = await db.ChatSessions.Include(s => s.Messages).FirstOrDefaultAsync(s => s.Id == sessionId && s.LearnerId == learnerId, ct);


            if (session is null) return Results.NotFound();
            if (session.Status == ChatSessionStatus.Finalized) return Results.Conflict(new { message = "Session already finalized." });

            var extracted = await agentService.ExtractProfileAsync(session, ct);
            if (extracted is null)
                return Results.UnprocessableEntity("Couldn't extract a clear enough profile yet - try adding a bit more detail to the conversation.");
            if (string.IsNullOrWhiteSpace(extracted.LearningGoal) || extracted.LearningGoal.Length > 500 ||
                (extracted.Subject?.Length ?? 0) > 200 || (extracted.CurrentLevel?.Length ?? 0) > 50 || (extracted.TargetOutcome?.Length ?? 0) > 500 ||
                (extracted.PreferredLanguage?.Length ?? 0) > 20 || (extracted.LearningStyle?.Length ?? 0) > 50 || extracted.WeeklyStudyHours is < 0 or > 168)
                return Results.UnprocessableEntity("The extracted profile exceeded supported field limits. Refine the conversation and try again.");

            extracted.Subject ??= string.Empty;
            extracted.CurrentLevel ??= "Not set";
            extracted.TargetOutcome ??= string.Empty;
            extracted.PreferredLanguage ??= "en";
            extracted.LearningStyle ??= string.Empty;
            extracted.Constraints ??= string.Empty;
            extracted.PreferredYouTubeChannelsJson ??= "[]";
            extracted.KnownStrengthsJson ??= "[]";
            extracted.KnownWeaknessesJson ??= "[]";
            extracted.Requirements ??= string.Empty;

            var profile = await db.LearnerProfiles.FirstOrDefaultAsync(p => p.LearnerId == learnerId, ct);
            if (profile == null)
            {
                profile = extracted;
                profile.LearnerId = learnerId;
                db.Add(profile);
            }
            else
            {
                profile.LearningGoal = extracted.LearningGoal;
                profile.Subject = extracted.Subject ?? string.Empty;
                profile.CurrentLevel = extracted.CurrentLevel ?? "Not set";
                profile.TargetOutcome = extracted.TargetOutcome ?? string.Empty;
                profile.WeeklyStudyHours = extracted.WeeklyStudyHours;
                profile.PreferredLanguage = extracted.PreferredLanguage ?? "en";
                profile.LearningStyle = extracted.LearningStyle ?? string.Empty;
                profile.Constraints = extracted.Constraints ?? string.Empty;
                profile.PreferredYouTubeChannelsJson = extracted.PreferredYouTubeChannelsJson ?? "[]";
                profile.KnownStrengthsJson = extracted.KnownStrengthsJson ?? "[]";
                profile.KnownWeaknessesJson = extracted.KnownWeaknessesJson ?? "[]";
                profile.Requirements = extracted.Requirements ?? string.Empty;
                profile.UpdatedAt = DateTime.UtcNow;
            }
            session.Status = ChatSessionStatus.Finalized;
            session.ResultingLearnerProfileId = profile.Id;
            await db.SaveChangesAsync(ct);

            return Results.Ok(profile);
        }

        private static bool IsCurrentLearner(Guid learnerId, ClaimsPrincipal user) =>
            user.Identity?.IsAuthenticated == true && Guid.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) && id == learnerId;
    }
}
