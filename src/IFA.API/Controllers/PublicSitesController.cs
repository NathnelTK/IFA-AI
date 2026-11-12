using System.Text.Json;
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
    [AllowAnonymous]
    [Route("api/public-sites")]
    public sealed class PublicSitesController : ControllerBase
    {
        private readonly IApplicationDbContext _db;
        private readonly FeatureOptions _features;

        public PublicSitesController(IApplicationDbContext db, IOptions<FeatureOptions> features)
        {
            _db = db;
            _features = features.Value;
        }

        [HttpGet("resolve")]
        public async Task<IActionResult> ResolveHost(
            [FromQuery] string host,
            [FromQuery] string path = "/",
            CancellationToken cancellationToken = default)
        {
            if (!_features.PublicPresence) return NotFound();
            var normalizedHost = SiteManagementController.NormalizeHost(host);
            if (normalizedHost == null) return NotFound();
            var domain = await _db.CustomDomains.SingleOrDefaultAsync(candidate =>
                candidate.Host == normalizedHost
                && candidate.Status == "Verified"
                && candidate.DisabledAt == null,
                cancellationToken);
            if (domain == null) return NotFound();
            var site = await _db.OrganizationSites.SingleOrDefaultAsync(candidate =>
                candidate.WorkspaceId == domain.WorkspaceId && candidate.IsPublished,
                cancellationToken);
            if (site == null) return NotFound();
            var pageType = PathToPage(path);
            if (!pageType.HasValue) return NotFound();
            return await OrganizationPageResultAsync(site, pageType.Value, cancellationToken);
        }

        [HttpGet("educators/{slug}")]
        public async Task<IActionResult> GetEducator(string slug, CancellationToken cancellationToken)
        {
            if (!_features.PublicPresence) return NotFound();
            var normalized = slug.Trim().ToLowerInvariant();
            var profile = await _db.EducatorPublicProfiles.SingleOrDefaultAsync(candidate =>
                candidate.Slug == normalized && candidate.IsPublished,
                cancellationToken);
            if (profile == null)
            {
                var alias = await _db.PublicSlugAliases.SingleOrDefaultAsync(candidate =>
                    candidate.TargetType == "Educator" && candidate.Slug == normalized && candidate.RetiredAt == null,
                    cancellationToken);
                if (alias == null) return NotFound();
                var current = await _db.EducatorPublicProfiles.SingleOrDefaultAsync(candidate => candidate.Id == alias.TargetId && candidate.IsPublished, cancellationToken);
                return current == null ? NotFound() : RedirectPermanent($"/e/{current.Slug}");
            }
            var courses = await PublicCoursesAsync(profile.WorkspaceId, cancellationToken);
            return Ok(new
            {
                kind = "Educator",
                profile.Id,
                profile.Slug,
                profile.DisplayName,
                profile.Headline,
                profile.Bio,
                profile.PhotoAssetId,
                expertise = Json(profile.ExpertiseJson),
                links = Json(profile.LinksJson),
                theme = Json(profile.ThemeJson),
                courses
            });
        }

        [HttpGet("organizations/{slug}/{page?}")]
        public async Task<IActionResult> GetOrganization(
            string slug,
            string? page,
            CancellationToken cancellationToken)
        {
            if (!_features.PublicPresence) return NotFound();
            var normalized = slug.Trim().ToLowerInvariant();
            var site = await _db.OrganizationSites.SingleOrDefaultAsync(candidate =>
                candidate.Slug == normalized && candidate.IsPublished,
                cancellationToken);
            if (site == null)
            {
                var alias = await _db.PublicSlugAliases.SingleOrDefaultAsync(candidate =>
                    candidate.TargetType == "Organization" && candidate.Slug == normalized && candidate.RetiredAt == null,
                    cancellationToken);
                if (alias == null) return NotFound();
                var current = await _db.OrganizationSites.SingleOrDefaultAsync(candidate => candidate.Id == alias.TargetId && candidate.IsPublished, cancellationToken);
                return current == null ? NotFound() : RedirectPermanent($"/org/{current.Slug}/{page ?? string.Empty}".TrimEnd('/'));
            }
            var pageType = PathToPage(page == null ? "/" : $"/{page}");
            return pageType.HasValue
                ? await OrganizationPageResultAsync(site, pageType.Value, cancellationToken)
                : NotFound();
        }

        [HttpGet("organizations/{slug}/external")]
        public async Task<IActionResult> RedirectToExternalWebsite(string slug, CancellationToken cancellationToken)
        {
            if (!_features.PublicPresence) return NotFound();
            var workspaceId = await _db.OrganizationSites
                .Where(site => site.Slug == slug.ToLower() && site.IsPublished)
                .Select(site => (Guid?)site.WorkspaceId)
                .SingleOrDefaultAsync(cancellationToken);
            if (!workspaceId.HasValue) return NotFound();
            var website = await _db.VerifiedExternalWebsites.SingleOrDefaultAsync(candidate =>
                candidate.WorkspaceId == workspaceId.Value && candidate.Status == "Verified",
                cancellationToken);
            return website == null ? NotFound() : Redirect(website.Url);
        }

        [HttpGet("organizations/{slug}/integration-links")]
        public async Task<IActionResult> GetIntegrationLinks(string slug, CancellationToken cancellationToken)
        {
            if (!_features.PublicPresence) return NotFound();
            var site = await _db.OrganizationSites.SingleOrDefaultAsync(candidate =>
                candidate.Slug == slug.ToLower() && candidate.IsPublished,
                cancellationToken);
            if (site == null) return NotFound();
            var courses = await PublicCoursesAsync(site.WorkspaceId, cancellationToken);
            return Ok(new
            {
                organizationUrl = $"/org/{site.Slug}",
                courses = courses.Select(course => new
                {
                    course.id,
                    course.title,
                    href = $"/courses/{course.id}",
                    html = $"<a href=\"/courses/{course.id}\">{System.Net.WebUtility.HtmlEncode(course.title)}</a>"
                })
            });
        }

        private async Task<IActionResult> OrganizationPageResultAsync(
            OrganizationSite site,
            OrganizationPageType pageType,
            CancellationToken cancellationToken)
        {
            var page = await _db.OrganizationSitePages.SingleOrDefaultAsync(candidate =>
                candidate.OrganizationSiteId == site.Id
                && candidate.PageType == pageType
                && candidate.IsVisible,
                cancellationToken);
            if (page == null) return NotFound();
            var navigation = await _db.OrganizationSitePages
                .Where(candidate => candidate.OrganizationSiteId == site.Id && candidate.IsVisible)
                .OrderBy(candidate => candidate.NavigationOrder)
                .Select(candidate => new { candidate.PageType, candidate.Title })
                .ToListAsync(cancellationToken);
            var courses = pageType is OrganizationPageType.Home or OrganizationPageType.Courses
                ? await PublicCoursesAsync(site.WorkspaceId, cancellationToken)
                : [];
            return Ok(new
            {
                kind = "Organization",
                site.Id,
                site.Slug,
                site.Name,
                site.LogoAssetId,
                theme = Json(site.ThemeJson),
                seo = Json(site.SeoJson),
                navigation,
                page = new
                {
                    page.PageType,
                    page.Title,
                    schema = Json(page.SchemaJson)
                },
                courses
            });
        }

        private async Task<List<PublicCourse>> PublicCoursesAsync(Guid workspaceId, CancellationToken cancellationToken) =>
            await _db.Courses
                .Where(course => course.WorkspaceId == workspaceId
                    && course.IsPublished
                    && course.IsPublic
                    && course.Visibility == "Public"
                    && course.ActiveReleaseId != null)
                .OrderByDescending(course => course.CreatedAt)
                .Select(course => new PublicCourse(
                    course.Id,
                    course.Title,
                    course.Description,
                    course.Category,
                    course.Level,
                    course.CoverImageUrl,
                    course.IsPaid,
                    course.PriceAmount,
                    course.Currency))
                .ToListAsync(cancellationToken);

        private static OrganizationPageType? PathToPage(string path) => path.Trim('/').ToLowerInvariant() switch
        {
            "" => OrganizationPageType.Home,
            "courses" => OrganizationPageType.Courses,
            "about" => OrganizationPageType.About,
            "instructors" => OrganizationPageType.Instructors,
            "updates" => OrganizationPageType.Updates,
            "testimonials" => OrganizationPageType.Testimonials,
            "faq" => OrganizationPageType.Faq,
            "contact" => OrganizationPageType.Contact,
            _ => null
        };

        private static JsonElement Json(string value)
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.Clone();
        }

        private sealed record PublicCourse(
            Guid id,
            string title,
            string description,
            string category,
            string level,
            string? coverImageUrl,
            bool isPaid,
            decimal priceAmount,
            string currency);
    }
}
