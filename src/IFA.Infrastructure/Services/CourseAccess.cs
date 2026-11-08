using System.Data;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace IFA.Infrastructure.Services;

public sealed class CourseAccessException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed record CourseAccessResult(Course Course, Guid? LearnerId, CourseEnrollment? Enrollment)
{
    public bool IsOwner => LearnerId.HasValue && Course.CreatorLearnerId == LearnerId;
    public bool HasContentAccess => IsOwner || (Course.IsPublished && Enrollment?.Status == "Active");

    public void RequireMetadata()
    {
        if (IsOwner) return;
        if (!Course.IsPublished)
            throw new CourseAccessException(403, "This course is an unpublished draft.");
        if (Course.Visibility is not ("Public" or "Unlisted") && Enrollment == null)
            throw new CourseAccessException(LearnerId.HasValue ? 403 : 401, "This private course requires an enrollment or an owner-issued invitation.");
    }

    public void RequireContent(bool learnerMutation = false)
    {
        if (!LearnerId.HasValue)
            throw new CourseAccessException(401, "Sign in to access this course.");
        if (learnerMutation && IsOwner)
            throw new CourseAccessException(403, "Owner preview does not record learner progress. Use a separate enrolled learner account.");
        if (!HasContentAccess)
            throw new CourseAccessException(403, Course.IsPublished
                ? "An Active enrollment is required to access this course content."
                : "This course is an unpublished draft.");
    }
}

public static class CourseAccess
{
    public static async Task<CourseAccessResult> GetAsync(
        IApplicationDbContext context, Guid courseId, Guid? learnerId, CancellationToken ct = default)
    {
        var course = await context.Courses.FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new CourseAccessException(404, "Course not found.");
        var enrollment = learnerId.HasValue
            ? await context.CourseEnrollments.FirstOrDefaultAsync(e => e.CourseId == courseId && e.LearnerId == learnerId, ct)
            : null;
        return new CourseAccessResult(course, learnerId, enrollment);
    }

    // Serializable writes protect capacity, attempt limits and first-action counters across requests/hosts.
    public static async Task<IDbContextTransaction?> BeginMutationAsync(IApplicationDbContext context, CancellationToken ct = default)
    {
        return context is DbContext db && db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct)
            : null;
    }

    public static async Task<(Course Course, CourseShareInvite? Invite)> ResolveShareAsync(
        IApplicationDbContext context, string shareCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(shareCode) || shareCode.Length > 128)
            throw new CourseAccessException(404, "Invalid or expired share code.");

        var invite = await context.CourseShareInvites.AsNoTracking().Include(i => i.Course)
            .FirstOrDefaultAsync(i => i.ShareCode == shareCode, ct);
        if (invite != null)
        {
            var invitedCourse = invite.Course;
            if (invitedCourse == null || !invitedCourse.IsPublished || invite.CreatedAt < DateTime.UtcNow.AddDays(-7) ||
                ((invitedCourse.Visibility == "Private" || invitedCourse.EnrollmentMode == "InviteOnly") &&
                 invitedCourse.CreatorLearnerId != invite.SenderLearnerId))
                throw new CourseAccessException(404, "Invalid or expired share code.");
            return (invitedCourse, invite);
        }

        var course = await context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.ShareCode == shareCode && c.IsPublished &&
            (c.Visibility == "Public" || c.Visibility == "Unlisted"), ct)
            ?? throw new CourseAccessException(404, "Invalid or expired share code.");
        return (course, null);
    }

    public static async Task<CourseEnrollment> EnrollAsync(
        IApplicationDbContext context, Guid courseId, Guid learnerId, Guid? inviteId = null, CancellationToken ct = default)
    {
        await using var transaction = await BeginMutationAsync(context, ct);
        if (learnerId == Guid.Empty || !await context.Learners.AnyAsync(l => l.Id == learnerId, ct))
            throw new CourseAccessException(401, "Sign in with a valid learner account.");

        var access = await GetAsync(context, courseId, learnerId, ct);
        var course = access.Course;
        if (access.IsOwner)
            throw new CourseAccessException(403, "Owners can preview their course without creating a learner enrollment.");
        if (!course.IsPublished)
            throw new CourseAccessException(409, "This course is not published for enrollment.");

        // Existing pending requests never become Active merely by following a different link.
        if (access.Enrollment != null)
        {
            if (access.Enrollment.Status == "Rejected")
                throw new CourseAccessException(403, "Your enrollment request was rejected. Contact the educator.");
            if (transaction != null) await transaction.CommitAsync(ct);
            return access.Enrollment;
        }

        CourseShareInvite? invite = null;
        if (inviteId.HasValue)
        {
            invite = await context.CourseShareInvites.FirstOrDefaultAsync(i => i.Id == inviteId && i.CourseId == courseId, ct);
            if (invite == null || invite.IsAccepted || invite.CreatedAt < DateTime.UtcNow.AddDays(-7))
                throw new CourseAccessException(400, "Invalid, used or expired invitation.");
            if (!string.IsNullOrWhiteSpace(invite.RecipientEmail))
            {
                var email = await context.Learners.Where(l => l.Id == learnerId).Select(l => l.Email).SingleAsync(ct);
                if (!string.Equals(email, invite.RecipientEmail.Trim(), StringComparison.OrdinalIgnoreCase))
                    throw new CourseAccessException(403, "This invitation is for a different learner.");
            }
        }

        var ownerInvite = invite != null && invite.SenderLearnerId == course.CreatorLearnerId;
        if (course.Visibility is not ("Public" or "Unlisted" or "Private") ||
            course.EnrollmentMode is not ("Open" or "ApprovalRequired" or "InviteOnly"))
            throw new CourseAccessException(409, "The course enrollment settings need educator review.");
        if ((course.Visibility == "Private" || course.EnrollmentMode == "InviteOnly") && !ownerInvite)
            throw new CourseAccessException(403, "An unused owner-issued invitation is required to enroll.");

        var now = DateTime.UtcNow;
        if (course.EnrollmentOpensAt > now)
            throw new CourseAccessException(409, "Enrollment has not opened yet.");
        if (course.EnrollmentClosesAt <= now)
            throw new CourseAccessException(409, "Enrollment is closed.");
        if (course.EnrollmentLimit.HasValue && await context.CourseEnrollments
                .CountAsync(e => e.CourseId == courseId && e.LearnerId != course.CreatorLearnerId && e.Status == "Active", ct) >= course.EnrollmentLimit.Value)
            throw new CourseAccessException(409, "This course has reached its enrollment limit.");
        if (course.IsPaid && course.PriceAmount <= 0)
            throw new CourseAccessException(409, "The educator must configure a valid course price before enrollment.");

        var enrollment = new CourseEnrollment
        {
            CourseId = courseId,
            LearnerId = learnerId,
            Status = course.IsPaid ? "PendingPayment" : course.RequiresApproval ? "PendingApproval" : "Active",
            EnrolledAt = now,
            LastAccessedAt = now
        };
        context.Add(enrollment);
        if (invite != null) invite.IsAccepted = true;
        await context.SaveChangesAsync(ct);
        if (transaction != null) await transaction.CommitAsync(ct);
        return enrollment;
    }
}
