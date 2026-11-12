using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using IFA.Application.Common.Interfaces;
using IFA.Domain.Entities;
using IFA.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IFA.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/site-management")]
    public sealed partial class SiteManagementController : BaseApiController
    {
        private readonly IApplicationDbContext _db;
        private readonly IPlatformAuthorizationService _authorization;
        private readonly FeatureOptions _features;
        private readonly TimeProvider _timeProvider;
        private readonly CustomDomainOptions _domainOptions;

        public SiteManagementController(
            IApplicationDbContext db,
            IPlatformAuthorizationService authorization,
            IOptions<FeatureOptions> features,
            IOptions<CustomDomainOptions> domainOptions,
            TimeProvider timeProvider)
        {
            _db = db;
            _authorization = authorization;
            _features = features.Value;
            _domainOptions = domainOptions.Value;
            _timeProvider = timeProvider;
        }

        public sealed class EducatorProfileRequest
        {
            [Required, StringLength(100)] public string Slug { get; set; } = string.Empty;
            [Required, StringLength(160)] public string DisplayName { get; set; } = string.Empty;
            [StringLength(300)] public string Headline { get; set; } = string.Empty;
            [StringLength(5000)] public string Bio { get; set; } = string.Empty;
            public Guid? PhotoAssetId { get; set; }
            [StringLength(20_000)] public string ExpertiseJson { get; set; } = "[]";
            [StringLength(20_000)] public string LinksJson { get; set; } = "[]";
            [StringLength(20_000)] public string ThemeJson { get; set; } = "{}";
        }

        public sealed class OrganizationSiteRequest
        {
            [Required, StringLength(100)] public string Slug { get; set; } = string.Empty;
            [Required, StringLength(160)] public string Name { get; set; } = string.Empty;
            public Guid? LogoAssetId { get; set; }
            [StringLength(20_000)] public string ThemeJson { get; set; } = "{}";
            [StringLength(20_000)] public string SeoJson { get; set; } = "{}";
        }

        public sealed class OrganizationPageRequest
        {
            [Required, StringLength(32)] public string PageType { get; set; } = string.Empty;
            public bool IsVisible { get; set; } = true;
            [Range(0, 100)] public int NavigationOrder { get; set; }
            [Required, StringLength(160)] public string Title { get; set; } = string.Empty;
            [Required, StringLength(50_000)] public string SchemaJson { get; set; } = "{}";
        }

        public sealed class DomainRequest
        {
            [Required, StringLength(253)] public string Host { get; set; } = string.Empty;
        }

        public sealed class ExternalWebsiteRequest
        {
            [Required, StringLength(2048)] public string Url { get; set; } = string.Empty;
        }

        [HttpPut("workspaces/{workspaceId:guid}/educator-profile")]
        public async Task<IActionResult> SaveEducatorProfile(
            Guid workspaceId,
            [FromBody] EducatorProfileRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.PublicPresence) return NotFound();
            var actorId = await RequireWorkspaceAsync(workspaceId, cancellationToken);
            if (!actorId.HasValue) return Forbid();
            var workspace = await _db.EducatorWorkspaces.SingleAsync(candidate => candidate.Id == workspaceId, cancellationToken);
            if (workspace.Type != EducatorWorkspaceType.Individual)
                return Conflict(new { message = "Individual educator profiles and organization sites use separate schemas." });
            var slug = NormalizeSlug(request.Slug);
            if (slug == null || !ValidJson(request.ExpertiseJson, JsonValueKind.Array)
                || !ValidJson(request.LinksJson, JsonValueKind.Array)
                || !ValidJson(request.ThemeJson, JsonValueKind.Object))
                return BadRequest(new { message = "The profile slug or structured fields are invalid." });
            if (await SlugUnavailableAsync("Educator", slug, workspaceId, cancellationToken))
                return Conflict(new { message = "This public slug is reserved." });

            var now = UtcNow();
            var profile = await _db.EducatorPublicProfiles.SingleOrDefaultAsync(candidate => candidate.WorkspaceId == workspaceId, cancellationToken);
            if (profile == null)
            {
                profile = new EducatorPublicProfile { WorkspaceId = workspaceId, CreatedAt = now };
                _db.Add(profile);
            }
            else if (!profile.Slug.Equals(slug, StringComparison.Ordinal))
            {
                _db.Add(new PublicSlugAlias
                {
                    TargetType = "Educator",
                    TargetId = profile.Id,
                    Slug = profile.Slug,
                    CreatedAt = now
                });
            }
            profile.Slug = slug;
            profile.DisplayName = request.DisplayName.Trim();
            profile.Headline = request.Headline.Trim();
            profile.Bio = request.Bio.Trim();
            profile.PhotoAssetId = request.PhotoAssetId;
            profile.ExpertiseJson = request.ExpertiseJson;
            profile.LinksJson = request.LinksJson;
            profile.ThemeJson = request.ThemeJson;
            profile.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(profile);
        }

        [HttpPost("workspaces/{workspaceId:guid}/educator-profile/publish")]
        public async Task<IActionResult> PublishEducatorProfile(Guid workspaceId, CancellationToken cancellationToken)
        {
            if (!_features.PublicPresence) return NotFound();
            if (!(await RequireWorkspaceAsync(workspaceId, cancellationToken)).HasValue) return Forbid();
            var profile = await _db.EducatorPublicProfiles.SingleOrDefaultAsync(candidate => candidate.WorkspaceId == workspaceId, cancellationToken);
            if (profile == null || string.IsNullOrWhiteSpace(profile.Bio))
                return Conflict(new { message = "Complete the educator profile before publishing." });
            profile.IsPublished = true;
            profile.PublishedAt = UtcNow();
            profile.UpdatedAt = UtcNow();
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpPut("workspaces/{workspaceId:guid}/organization-site")]
        public async Task<IActionResult> SaveOrganizationSite(
            Guid workspaceId,
            [FromBody] OrganizationSiteRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.PublicPresence) return NotFound();
            var actorId = await RequireWorkspaceAsync(workspaceId, cancellationToken);
            if (!actorId.HasValue) return Forbid();
            var workspace = await _db.EducatorWorkspaces.SingleAsync(candidate => candidate.Id == workspaceId, cancellationToken);
            if (workspace.Type != EducatorWorkspaceType.Organization)
                return Conflict(new { message = "Organization sites are available only to organization workspaces." });
            var slug = NormalizeSlug(request.Slug);
            if (slug == null || !ValidJson(request.ThemeJson, JsonValueKind.Object) || !ValidJson(request.SeoJson, JsonValueKind.Object))
                return BadRequest(new { message = "The organization site fields are invalid." });
            if (await SlugUnavailableAsync("Organization", slug, workspaceId, cancellationToken))
                return Conflict(new { message = "This public slug is reserved." });

            var now = UtcNow();
            var site = await _db.OrganizationSites.SingleOrDefaultAsync(candidate => candidate.WorkspaceId == workspaceId, cancellationToken);
            if (site == null)
            {
                site = new OrganizationSite { WorkspaceId = workspaceId, CreatedAt = now };
                _db.Add(site);
            }
            else if (!site.Slug.Equals(slug, StringComparison.Ordinal))
            {
                _db.Add(new PublicSlugAlias
                {
                    TargetType = "Organization",
                    TargetId = site.Id,
                    Slug = site.Slug,
                    CreatedAt = now
                });
            }
            site.Slug = slug;
            site.Name = request.Name.Trim();
            site.LogoAssetId = request.LogoAssetId;
            site.ThemeJson = request.ThemeJson;
            site.SeoJson = request.SeoJson;
            site.UpdatedAt = now;
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(site);
        }

        [HttpPut("workspaces/{workspaceId:guid}/organization-site/pages")]
        public async Task<IActionResult> SaveOrganizationPage(
            Guid workspaceId,
            [FromBody] OrganizationPageRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.PublicPresence) return NotFound();
            if (!(await RequireWorkspaceAsync(workspaceId, cancellationToken)).HasValue) return Forbid();
            if (!Enum.TryParse<OrganizationPageType>(request.PageType, true, out var pageType)
                || !ValidPageSchema(pageType, request.SchemaJson))
                return BadRequest(new { message = "The bounded organization page schema is invalid." });
            if (pageType is OrganizationPageType.Home or OrganizationPageType.Courses && !request.IsVisible)
                return BadRequest(new { message = "Home and Courses are required organization pages." });
            var site = await _db.OrganizationSites.SingleOrDefaultAsync(candidate => candidate.WorkspaceId == workspaceId, cancellationToken);
            if (site == null) return Conflict(new { message = "Create the organization site first." });
            var page = await _db.OrganizationSitePages.SingleOrDefaultAsync(candidate =>
                candidate.OrganizationSiteId == site.Id && candidate.PageType == pageType,
                cancellationToken);
            if (page == null)
            {
                page = new OrganizationSitePage { OrganizationSiteId = site.Id, PageType = pageType };
                _db.Add(page);
            }
            page.IsVisible = request.IsVisible;
            page.NavigationOrder = request.NavigationOrder;
            page.Title = request.Title.Trim();
            page.SchemaJson = request.SchemaJson;
            page.UpdatedAt = UtcNow();
            site.UpdatedAt = UtcNow();
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(page);
        }

        [HttpPost("workspaces/{workspaceId:guid}/organization-site/publish")]
        public async Task<IActionResult> PublishOrganizationSite(Guid workspaceId, CancellationToken cancellationToken)
        {
            if (!_features.PublicPresence) return NotFound();
            if (!(await RequireWorkspaceAsync(workspaceId, cancellationToken)).HasValue) return Forbid();
            var site = await _db.OrganizationSites.SingleOrDefaultAsync(candidate => candidate.WorkspaceId == workspaceId, cancellationToken);
            if (site == null) return NotFound();
            var required = await _db.OrganizationSitePages
                .Where(page => page.OrganizationSiteId == site.Id && page.IsVisible)
                .Select(page => page.PageType)
                .ToListAsync(cancellationToken);
            if (!required.Contains(OrganizationPageType.Home) || !required.Contains(OrganizationPageType.Courses))
                return Conflict(new { message = "Visible Home and Courses pages are required before publishing." });
            site.IsPublished = true;
            site.PublishedAt = UtcNow();
            site.UpdatedAt = UtcNow();
            await _db.SaveChangesAsync(cancellationToken);
            return NoContent();
        }

        [HttpPost("workspaces/{workspaceId:guid}/domains")]
        public async Task<IActionResult> AddDomain(
            Guid workspaceId,
            [FromBody] DomainRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.CustomDomains) return NotFound();
            if (!(await RequireWorkspaceAsync(workspaceId, cancellationToken)).HasValue) return Forbid();
            var host = NormalizeHost(request.Host);
            if (host == null || IsFirstPartyHost(host)) return BadRequest(new { message = "Use an exact custom hostname outside Witforge-owned domains." });
            if (await _db.CustomDomains.AnyAsync(domain => domain.Host == host, cancellationToken))
                return Conflict(new { message = "This hostname is already registered." });
            var now = UtcNow();
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            var domain = new CustomDomain
            {
                WorkspaceId = workspaceId,
                Host = host,
                Status = "PendingDns",
                VerificationToken = $"witforge-verification={token}",
                ExpectedCnameTarget = _domainOptions.CnameTarget,
                NextCheckAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Add(domain);
            await _db.SaveChangesAsync(cancellationToken);
            return Accepted(new
            {
                domain.Id,
                domain.Host,
                domain.Status,
                dns = new
                {
                    txtName = $"_witforge.{domain.Host}",
                    txtValue = domain.VerificationToken,
                    cnameName = domain.Host,
                    cnameValue = domain.ExpectedCnameTarget
                }
            });
        }

        [HttpPost("workspaces/{workspaceId:guid}/external-websites")]
        public async Task<IActionResult> AddExternalWebsite(
            Guid workspaceId,
            [FromBody] ExternalWebsiteRequest request,
            CancellationToken cancellationToken)
        {
            if (!_features.PublicPresence) return NotFound();
            if (!(await RequireWorkspaceAsync(workspaceId, cancellationToken)).HasValue) return Forbid();
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
                || uri.Scheme != "https"
                || !string.IsNullOrEmpty(uri.UserInfo)
                || NormalizeHost(uri.Host) is not { } host
                || IsFirstPartyHost(host))
                return BadRequest(new { message = "Use a public HTTPS website URL outside Witforge." });
            var now = UtcNow();
            var website = new VerifiedExternalWebsite
            {
                WorkspaceId = workspaceId,
                Url = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/'),
                Host = host,
                Status = "Pending",
                VerificationToken = $"witforge-site-verification={Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant()}",
                NextCheckAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Add(website);
            await _db.SaveChangesAsync(cancellationToken);
            return Accepted(new
            {
                website.Id,
                website.Url,
                website.Status,
                verificationUrl = $"{website.Url}/.well-known/witforge-verification.txt",
                expectedContent = website.VerificationToken
            });
        }

        private async Task<Guid?> RequireWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
        {
            var accountId = await GetCurrentLearnerIdAsync(_db);
            return await _authorization.CanManageWorkspaceAsync(accountId, workspaceId, cancellationToken) ? accountId : null;
        }

        private async Task<bool> SlugUnavailableAsync(
            string targetType,
            string slug,
            Guid workspaceId,
            CancellationToken cancellationToken)
        {
            if (ReservedSlugs.Contains(slug)) return true;
            if (await _db.PublicSlugAliases.AnyAsync(alias => alias.TargetType == targetType && alias.Slug == slug, cancellationToken)) return true;
            return targetType == "Educator"
                ? await _db.EducatorPublicProfiles.AnyAsync(profile => profile.Slug == slug && profile.WorkspaceId != workspaceId, cancellationToken)
                : await _db.OrganizationSites.AnyAsync(site => site.Slug == slug && site.WorkspaceId != workspaceId, cancellationToken);
        }

        private static bool ValidPageSchema(OrganizationPageType pageType, string value)
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                if (document.RootElement.ValueKind != JsonValueKind.Object
                    || !document.RootElement.TryGetProperty("sections", out var sections)
                    || sections.ValueKind != JsonValueKind.Array
                    || sections.GetArrayLength() > 20) return false;
                var allowed = pageType switch
                {
                    OrganizationPageType.Home => new[] { "hero", "richText", "courseGrid", "testimonials", "cta" },
                    OrganizationPageType.Courses => new[] { "hero", "courseGrid", "richText" },
                    OrganizationPageType.About => new[] { "hero", "richText", "stats" },
                    OrganizationPageType.Instructors => new[] { "hero", "instructorGrid", "richText" },
                    OrganizationPageType.Updates => new[] { "hero", "updates" },
                    OrganizationPageType.Testimonials => new[] { "hero", "testimonials" },
                    OrganizationPageType.Faq => new[] { "hero", "faq" },
                    OrganizationPageType.Contact => new[] { "hero", "contact", "richText" },
                    _ => Array.Empty<string>()
                };
                foreach (var section in sections.EnumerateArray())
                {
                    if (section.ValueKind != JsonValueKind.Object
                        || !section.TryGetProperty("type", out var type)
                        || !allowed.Contains(type.GetString(), StringComparer.Ordinal)) return false;
                    if (section.GetRawText().Length > 10_000) return false;
                }
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool ValidJson(string value, JsonValueKind kind)
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                return document.RootElement.ValueKind == kind;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static string? NormalizeSlug(string value)
        {
            var slug = SlugCharacters().Replace(value.Trim().ToLowerInvariant(), "-").Trim('-');
            return slug.Length is >= 3 and <= 100 ? slug : null;
        }

        internal static string? NormalizeHost(string value)
        {
            var host = value.Trim().TrimEnd('.');
            try { host = new IdnMapping().GetAscii(host).ToLowerInvariant(); }
            catch (ArgumentException) { return null; }
            return HostCharacters().IsMatch(host) && host.Length <= 253 && host.Contains('.') ? host : null;
        }

        private static bool IsFirstPartyHost(string host) =>
            host == "witforge.com" || host.EndsWith(".witforge.com", StringComparison.Ordinal);

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        private static readonly HashSet<string> ReservedSlugs = new(StringComparer.OrdinalIgnoreCase)
        {
            "app", "auth", "api", "admin", "studio", "marketplace", "courses", "settings", "support", "www"
        };

        [GeneratedRegex("[^a-z0-9-]+", RegexOptions.CultureInvariant)]
        private static partial Regex SlugCharacters();

        [GeneratedRegex("^(?=.{1,253}$)(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\\.)+[a-z]{2,63}$", RegexOptions.CultureInvariant)]
        private static partial Regex HostCharacters();
    }
}
