using IFA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IFA.Infrastructure.Data.Configurations
{
    public sealed class AccountRoleGrantConfiguration : IEntityTypeConfiguration<AccountRoleGrant>
    {
        public void Configure(EntityTypeBuilder<AccountRoleGrant> builder)
        {
            builder.ToTable("AccountRoleGrants");
            builder.HasKey(role => role.Id);
            builder.Property(role => role.Role).HasConversion<string>().HasMaxLength(32);
            builder.Property(role => role.Reason).HasMaxLength(500);
            builder.HasIndex(role => new { role.AccountId, role.Role }).IsUnique();
            builder.HasIndex(role => new { role.Role, role.IsActive });
            builder.HasOne(role => role.Account)
                .WithMany(account => account.RoleGrants)
                .HasForeignKey(role => role.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
    {
        public void Configure(EntityTypeBuilder<AuthSession> builder)
        {
            builder.ToTable("AuthSessions");
            builder.HasKey(session => session.Id);
            builder.Property(session => session.TokenHash).IsRequired().HasMaxLength(32);
            builder.Property(session => session.CsrfTokenHash).IsRequired().HasMaxLength(32);
            builder.Property(session => session.HostScope).IsRequired().HasMaxLength(253);
            builder.Property(session => session.SiteScope).HasConversion<string>().HasMaxLength(16);
            builder.Property(session => session.UserAgentHash).HasMaxLength(64);
            builder.Property(session => session.IpAddressHash).HasMaxLength(64);
            builder.Property(session => session.RevocationReason).HasMaxLength(200);
            builder.HasIndex(session => session.TokenHash).IsUnique();
            builder.HasIndex(session => new { session.AccountId, session.RevokedAt, session.ExpiresAt });
            builder.HasOne(session => session.Account)
                .WithMany()
                .HasForeignKey(session => session.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<AuthSession>()
                .WithMany()
                .HasForeignKey(session => session.ParentSessionId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    public sealed class LegacyTokenExchangeConfiguration : IEntityTypeConfiguration<LegacyTokenExchange>
    {
        public void Configure(EntityTypeBuilder<LegacyTokenExchange> builder)
        {
            builder.ToTable("LegacyTokenExchanges");
            builder.HasKey(exchange => exchange.Id);
            builder.Property(exchange => exchange.TokenHash).IsRequired().HasMaxLength(32);
            builder.HasIndex(exchange => exchange.TokenHash).IsUnique();
            builder.HasIndex(exchange => exchange.AccountId);
            builder.HasOne<Learner>()
                .WithMany()
                .HasForeignKey(exchange => exchange.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<AuthSession>()
                .WithMany()
                .HasForeignKey(exchange => exchange.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class AccountSecurityTokenConfiguration : IEntityTypeConfiguration<AccountSecurityToken>
    {
        public void Configure(EntityTypeBuilder<AccountSecurityToken> builder)
        {
            builder.ToTable("AccountSecurityTokens");
            builder.HasKey(token => token.Id);
            builder.Property(token => token.Purpose).HasConversion<string>().HasMaxLength(32);
            builder.Property(token => token.TokenHash).IsRequired().HasMaxLength(32);
            builder.HasIndex(token => token.TokenHash).IsUnique();
            builder.HasIndex(token => new { token.AccountId, token.Purpose, token.ConsumedAt });
            builder.HasOne<Learner>()
                .WithMany()
                .HasForeignKey(token => token.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class AccountMfaCredentialConfiguration : IEntityTypeConfiguration<AccountMfaCredential>
    {
        public void Configure(EntityTypeBuilder<AccountMfaCredential> builder)
        {
            builder.ToTable("AccountMfaCredentials");
            builder.HasKey(credential => credential.Id);
            builder.Property(credential => credential.ProtectedSecret).IsRequired().HasMaxLength(1000);
            builder.HasIndex(credential => credential.AccountId).IsUnique();
            builder.HasOne<Learner>()
                .WithMany()
                .HasForeignKey(credential => credential.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class AccountRecoveryCodeConfiguration : IEntityTypeConfiguration<AccountRecoveryCode>
    {
        public void Configure(EntityTypeBuilder<AccountRecoveryCode> builder)
        {
            builder.ToTable("AccountRecoveryCodes");
            builder.HasKey(code => code.Id);
            builder.Property(code => code.CodeHash).IsRequired().HasMaxLength(32);
            builder.HasIndex(code => code.CodeHash).IsUnique();
            builder.HasIndex(code => new { code.AccountId, code.UsedAt });
            builder.HasOne<Learner>()
                .WithMany()
                .HasForeignKey(code => code.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class OAuthAuthorizationCodeConfiguration : IEntityTypeConfiguration<OAuthAuthorizationCode>
    {
        public void Configure(EntityTypeBuilder<OAuthAuthorizationCode> builder)
        {
            builder.ToTable("OAuthAuthorizationCodes");
            builder.HasKey(code => code.Id);
            builder.Property(code => code.CodeHash).IsRequired().HasMaxLength(32);
            builder.Property(code => code.ClientId).IsRequired().HasMaxLength(100);
            builder.Property(code => code.RedirectUri).IsRequired().HasMaxLength(1000);
            builder.Property(code => code.CodeChallenge).IsRequired().HasMaxLength(128);
            builder.Property(code => code.CodeChallengeMethod).IsRequired().HasMaxLength(16);
            builder.Property(code => code.StateHash).IsRequired().HasMaxLength(128);
            builder.Property(code => code.TargetSiteScope).HasConversion<string>().HasMaxLength(16);
            builder.Property(code => code.TargetHost).IsRequired().HasMaxLength(253);
            builder.HasIndex(code => code.CodeHash).IsUnique();
            builder.HasIndex(code => new { code.AccountId, code.ConsumedAt, code.ExpiresAt });
            builder.HasOne<Learner>()
                .WithMany()
                .HasForeignKey(code => code.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<AuthSession>()
                .WithMany()
                .HasForeignKey(code => code.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class InternalRequestReplayConfiguration : IEntityTypeConfiguration<InternalRequestReplay>
    {
        public void Configure(EntityTypeBuilder<InternalRequestReplay> builder)
        {
            builder.ToTable("InternalRequestReplays");
            builder.HasKey(replay => replay.Id);
            builder.Property(replay => replay.NonceHash).IsRequired().HasMaxLength(32);
            builder.HasIndex(replay => replay.NonceHash).IsUnique();
            builder.HasIndex(replay => replay.ExpiresAt);
        }
    }

    public sealed class MfaLoginChallengeConfiguration : IEntityTypeConfiguration<MfaLoginChallenge>
    {
        public void Configure(EntityTypeBuilder<MfaLoginChallenge> builder)
        {
            builder.ToTable("MfaLoginChallenges");
            builder.HasKey(challenge => challenge.Id);
            builder.Property(challenge => challenge.TokenHash).IsRequired().HasMaxLength(32);
            builder.Property(challenge => challenge.HostScope).IsRequired().HasMaxLength(253);
            builder.Property(challenge => challenge.SiteScope).HasConversion<string>().HasMaxLength(16);
            builder.HasIndex(challenge => challenge.TokenHash).IsUnique();
            builder.HasIndex(challenge => new { challenge.AccountId, challenge.ConsumedAt, challenge.ExpiresAt });
            builder.HasOne<Learner>()
                .WithMany()
                .HasForeignKey(challenge => challenge.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class EducatorWorkspaceConfiguration : IEntityTypeConfiguration<EducatorWorkspace>
    {
        public void Configure(EntityTypeBuilder<EducatorWorkspace> builder)
        {
            builder.ToTable("EducatorWorkspaces");
            builder.HasKey(workspace => workspace.Id);
            builder.Property(workspace => workspace.Type).HasConversion<string>().HasMaxLength(16);
            builder.Property(workspace => workspace.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(workspace => workspace.Name).IsRequired().HasMaxLength(160);
            builder.Property(workspace => workspace.Slug).IsRequired().HasMaxLength(100);
            builder.Property(workspace => workspace.SuspensionReason).HasMaxLength(500);
            builder.HasIndex(workspace => workspace.Slug).IsUnique();
            builder.HasIndex(workspace => new { workspace.Type, workspace.Status });
        }
    }

    public sealed class WorkspaceMembershipConfiguration : IEntityTypeConfiguration<WorkspaceMembership>
    {
        public void Configure(EntityTypeBuilder<WorkspaceMembership> builder)
        {
            builder.ToTable("WorkspaceMemberships");
            builder.HasKey(membership => membership.Id);
            builder.Property(membership => membership.Role).HasConversion<string>().HasMaxLength(16);
            builder.Property(membership => membership.State).HasConversion<string>().HasMaxLength(16);
            builder.HasIndex(membership => new { membership.WorkspaceId, membership.AccountId }).IsUnique();
            builder.HasIndex(membership => new { membership.AccountId, membership.State });
            builder.HasOne<EducatorWorkspace>()
                .WithMany()
                .HasForeignKey(membership => membership.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Learner>()
                .WithMany()
                .HasForeignKey(membership => membership.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class WorkspaceInstructorProfileConfiguration : IEntityTypeConfiguration<WorkspaceInstructorProfile>
    {
        public void Configure(EntityTypeBuilder<WorkspaceInstructorProfile> builder)
        {
            builder.ToTable("WorkspaceInstructorProfiles");
            builder.HasKey(profile => profile.Id);
            builder.Property(profile => profile.DisplayName).IsRequired().HasMaxLength(160);
            builder.Property(profile => profile.Title).HasMaxLength(200);
            builder.Property(profile => profile.Bio).HasMaxLength(4000);
            builder.Property(profile => profile.PhotoUrl).HasMaxLength(2048);
            builder.Property(profile => profile.ExpertiseJson).HasColumnType("jsonb");
            builder.Property(profile => profile.LinksJson).HasColumnType("jsonb");
            builder.HasIndex(profile => profile.MembershipId).IsUnique();
            builder.HasOne<WorkspaceMembership>()
                .WithMany()
                .HasForeignKey(profile => profile.MembershipId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class WorkspaceInvitationConfiguration : IEntityTypeConfiguration<WorkspaceInvitation>
    {
        public void Configure(EntityTypeBuilder<WorkspaceInvitation> builder)
        {
            builder.ToTable("WorkspaceInvitations");
            builder.HasKey(invitation => invitation.Id);
            builder.Property(invitation => invitation.Email).IsRequired().HasMaxLength(254);
            builder.Property(invitation => invitation.Role).HasConversion<string>().HasMaxLength(16);
            builder.Property(invitation => invitation.TokenHash).IsRequired().HasMaxLength(32);
            builder.HasIndex(invitation => invitation.TokenHash).IsUnique();
            builder.HasIndex(invitation => new { invitation.WorkspaceId, invitation.Email, invitation.AcceptedAt, invitation.RevokedAt });
            builder.HasOne<EducatorWorkspace>()
                .WithMany()
                .HasForeignKey(invitation => invitation.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class CourseInstructorAssignmentConfiguration : IEntityTypeConfiguration<CourseInstructorAssignment>
    {
        public void Configure(EntityTypeBuilder<CourseInstructorAssignment> builder)
        {
            builder.ToTable("CourseInstructorAssignments");
            builder.HasKey(assignment => assignment.Id);
            builder.Property(assignment => assignment.AssignmentRole).IsRequired().HasMaxLength(32);
            builder.HasIndex(assignment => new { assignment.CourseId, assignment.MembershipId }).IsUnique();
            builder.HasIndex(assignment => new { assignment.CourseId, assignment.IsActive, assignment.OrderIndex });
            builder.HasOne<Course>()
                .WithMany()
                .HasForeignKey(assignment => assignment.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<WorkspaceMembership>()
                .WithMany()
                .HasForeignKey(assignment => assignment.MembershipId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class EducatorApplicationConfiguration : IEntityTypeConfiguration<EducatorApplication>
    {
        public void Configure(EntityTypeBuilder<EducatorApplication> builder)
        {
            builder.ToTable("EducatorApplications");
            builder.HasKey(application => application.Id);
            builder.Property(application => application.Status).IsRequired().HasMaxLength(32);
            builder.Property(application => application.EvidenceJson).HasColumnType("jsonb");
            builder.Property(application => application.DecisionReason).HasMaxLength(1000);
            builder.HasIndex(application => new { application.ApplicantAccountId, application.Status });
            builder.HasIndex(application => new { application.WorkspaceId, application.Status });
            builder.HasOne<EducatorWorkspace>()
                .WithMany()
                .HasForeignKey(application => application.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne<Learner>()
                .WithMany()
                .HasForeignKey(application => application.ApplicantAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class VerificationClaimConfiguration : IEntityTypeConfiguration<VerificationClaim>
    {
        public void Configure(EntityTypeBuilder<VerificationClaim> builder)
        {
            builder.ToTable("VerificationClaims");
            builder.HasKey(claim => claim.Id);
            builder.Property(claim => claim.ClaimType).IsRequired().HasMaxLength(64);
            builder.Property(claim => claim.ClaimValue).IsRequired().HasMaxLength(500);
            builder.Property(claim => claim.State).IsRequired().HasMaxLength(32);
            builder.Property(claim => claim.EvidenceHash).HasMaxLength(32);
            builder.HasIndex(claim => new { claim.WorkspaceId, claim.ClaimType, claim.ClaimValue }).IsUnique();
            builder.HasIndex(claim => new { claim.State, claim.ExpiresAt });
            builder.HasOne<EducatorWorkspace>()
                .WithMany()
                .HasForeignKey(claim => claim.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class WorkspaceEntitlementConfiguration : IEntityTypeConfiguration<WorkspaceEntitlement>
    {
        public void Configure(EntityTypeBuilder<WorkspaceEntitlement> builder)
        {
            builder.ToTable("WorkspaceEntitlements");
            builder.HasKey(entitlement => entitlement.Id);
            builder.Property(entitlement => entitlement.Key).IsRequired().HasMaxLength(100);
            builder.HasIndex(entitlement => new { entitlement.WorkspaceId, entitlement.Key }).IsUnique();
            builder.HasOne<EducatorWorkspace>()
                .WithMany()
                .HasForeignKey(entitlement => entitlement.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public sealed class PublicSlugAliasConfiguration : IEntityTypeConfiguration<PublicSlugAlias>
    {
        public void Configure(EntityTypeBuilder<PublicSlugAlias> builder)
        {
            builder.ToTable("PublicSlugAliases");
            builder.HasKey(alias => alias.Id);
            builder.Property(alias => alias.TargetType).IsRequired().HasMaxLength(32);
            builder.Property(alias => alias.Slug).IsRequired().HasMaxLength(100);
            builder.HasIndex(alias => new { alias.TargetType, alias.Slug }).IsUnique();
            builder.HasIndex(alias => new { alias.TargetType, alias.TargetId });
        }
    }
}
