using IFA.Application.Common.Interfaces;
using IFA.Application.Courses.Services;
using IFA.Application.Skills.Services;
using IFA.Infrastructure.AI;
using IFA.Infrastructure.Configuration;
using IFA.Infrastructure.Data;
using IFA.Infrastructure.Services;
using IFA.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace IFA.Infrastructure
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Registers the persistence layer and the infrastructure service
        /// implementations used by the application layer.
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString, IConfiguration configuration)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException(
                    "A database connection string is required to configure the Witforge infrastructure layer.",
                    nameof(connectionString));
            }

            // Database Configuration - PostgreSQL First, SQLite Fallback
            var dbProvider = configuration["DB_PROVIDER"]?.ToLowerInvariant();
            bool useSqlite = dbProvider == "sqlite" 
                || connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase) 
                || connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase);

            if (useSqlite)
            {
                var sqliteConn = connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
                    ? connectionString
                    : "Data Source=ifa.db";

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite(sqliteConn, sqlite =>
                        sqlite.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            }
            else
            {
                // Default to PostgreSQL for production
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(connectionString, npgsql =>
                        npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
            }

            services.AddScoped<IApplicationDbContext>(provider =>
                provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IPlatformAuthorizationService, PlatformAuthorizationService>();
            services.AddScoped<IAuthoringWorkspaceService, AuthoringWorkspaceService>();
            services.AddScoped<ICourseReleaseService, CourseReleaseService>();
            services.AddScoped<IReleaseProgressService, ReleaseProgressService>();
            services.AddSingleton(TimeProvider.System);
            services.AddOptions<FeatureOptions>()
                .Bind(configuration.GetSection(FeatureOptions.SectionName))
                .Validate(options => !options.Workspaces || options.OpaqueSessions,
                    "Features:Workspaces requires Features:OpaqueSessions.")
                .Validate(options => !options.UnifiedAuthoring || options.Workspaces,
                    "Features:UnifiedAuthoring requires Features:Workspaces.")
                .Validate(options => !options.ImmutableReleases || options.UnifiedAuthoring,
                    "Features:ImmutableReleases requires Features:UnifiedAuthoring.")
                .Validate(options => !options.CustomDomains || options.PublicPresence,
                    "Features:CustomDomains requires Features:PublicPresence.")
                .Validate(options => !options.Certificates || (options.ImmutableReleases && options.AssetUploads),
                    "Features:Certificates requires Features:ImmutableReleases and Features:AssetUploads.")
                .Validate(options => !options.PublicPresence || options.Workspaces,
                    "Features:PublicPresence requires Features:Workspaces.")
                .Validate(options => !options.Social || (options.Workspaces && options.ImmutableReleases),
                    "Features:Social requires Features:Workspaces and Features:ImmutableReleases.")
                .Validate(options => !options.Offers || options.ImmutableReleases,
                    "Features:Offers requires Features:ImmutableReleases.")
                .ValidateOnStart();
            services.AddOptions<AuthOptions>()
                .Bind(configuration.GetSection(AuthOptions.SectionName))
                .PostConfigure(options =>
                {
                    options.BffSigningSecret = configuration["BFF_INTERNAL_SECRET"] ?? options.BffSigningSecret;
                    options.ContextPepper = configuration["AUTH_CONTEXT_PEPPER"] ?? options.ContextPepper;
                    options.DataProtectionKey = configuration["AUTH_DATA_PROTECTION_KEY"] ?? options.DataProtectionKey;
                })
                .Validate(options => options.SessionHours is >= 1 and <= 168,
                    "Auth:SessionHours must be between 1 and 168.")
                .Validate(options => !configuration.GetValue<bool>("Features:OpaqueSessions")
                    || (options.BffSigningSecret.Length >= 32
                        && options.ContextPepper.Length >= 16
                        && IsBase64Key(options.DataProtectionKey)),
                    "Opaque sessions require BFF_INTERNAL_SECRET, AUTH_CONTEXT_PEPPER, and a base64 256-bit AUTH_DATA_PROTECTION_KEY.")
                .ValidateOnStart();
            services.AddOptions<EmailOptions>()
                .Bind(configuration.GetSection(EmailOptions.SectionName))
                .PostConfigure(options =>
                {
                    options.ApiKey = configuration["RESEND_API_KEY"] ?? options.ApiKey;
                    options.From = configuration["EMAIL_FROM"] ?? options.From;
                    options.AuthBaseUrl = configuration["AUTH_PUBLIC_URL"] ?? options.AuthBaseUrl;
                })
                .Validate(options => Uri.TryCreate(options.AuthBaseUrl, UriKind.Absolute, out var uri)
                    && uri.Scheme is "http" or "https",
                    "Email:AuthBaseUrl must be an absolute HTTP(S) URL.")
                .Validate(options => !configuration.GetValue<bool>("Features:OpaqueSessions")
                    || ((configuration["DOTNET_ENVIRONMENT"] ?? configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production")
                            .Equals("Development", StringComparison.OrdinalIgnoreCase)
                        && options.DeliveryMode.Equals("Development", StringComparison.OrdinalIgnoreCase))
                    || (options.DeliveryMode.Equals("Resend", StringComparison.OrdinalIgnoreCase)
                        && options.ApiKey.Length >= 16
                        && options.From.Contains('@')),
                    "Opaque sessions require a configured transactional email provider.")
                .ValidateOnStart();
            services.AddOptions<ObjectStorageOptions>()
                .Bind(configuration.GetSection(ObjectStorageOptions.SectionName))
                .PostConfigure(options =>
                {
                    options.ServiceUrl = configuration["R2_SERVICE_URL"] ?? options.ServiceUrl;
                    options.AccessKeyId = configuration["R2_ACCESS_KEY_ID"] ?? options.AccessKeyId;
                    options.SecretAccessKey = configuration["R2_SECRET_ACCESS_KEY"] ?? options.SecretAccessKey;
                    options.Bucket = configuration["R2_BUCKET"] ?? options.Bucket;
                    options.PublicBaseUrl = configuration["R2_PUBLIC_BASE_URL"] ?? options.PublicBaseUrl;
                })
                .Validate(options => !configuration.GetValue<bool>("Features:AssetUploads")
                    || (Uri.TryCreate(options.ServiceUrl, UriKind.Absolute, out _)
                        && options.AccessKeyId.Length > 8
                        && options.SecretAccessKey.Length > 16
                        && options.Bucket.Length > 2),
                    "Asset uploads require the R2 endpoint, credentials, and bucket.")
                .ValidateOnStart();
            services.AddOptions<MalwareScannerOptions>()
                .Bind(configuration.GetSection(MalwareScannerOptions.SectionName))
                .PostConfigure(options =>
                {
                    options.Host = configuration["CLAMAV_HOST"] ?? options.Host;
                    options.Port = int.TryParse(configuration["CLAMAV_PORT"], out var port) ? port : options.Port;
                })
                .Validate(options => !configuration.GetValue<bool>("Features:AssetUploads")
                    || (!string.IsNullOrWhiteSpace(options.Host) && options.Port is > 0 and <= 65535),
                    "Asset uploads require a private ClamAV host and port.")
                .ValidateOnStart();
            services.AddOptions<OcrOptions>()
                .Bind(configuration.GetSection(OcrOptions.SectionName))
                .PostConfigure(options =>
                {
                    options.Endpoint = configuration["OCR_SERVICE_URL"] ?? options.Endpoint;
                    options.ApiKey = configuration["OCR_API_KEY"] ?? options.ApiKey;
                });
            services.AddOptions<CustomDomainOptions>()
                .Bind(configuration.GetSection(CustomDomainOptions.SectionName))
                .PostConfigure(options =>
                {
                    options.VercelToken = configuration["VERCEL_API_TOKEN"] ?? options.VercelToken;
                    options.VercelProjectId = configuration["VERCEL_PROJECT_ID"] ?? options.VercelProjectId;
                    options.VercelTeamId = configuration["VERCEL_TEAM_ID"] ?? options.VercelTeamId;
                    options.CnameTarget = configuration["CUSTOM_DOMAIN_CNAME_TARGET"] ?? options.CnameTarget;
                })
                .Validate(options => !configuration.GetValue<bool>("Features:CustomDomains")
                    || (options.VercelToken.Length > 16
                        && options.VercelProjectId.Length > 2
                        && !string.IsNullOrWhiteSpace(options.CnameTarget)),
                    "Custom domains require Vercel API credentials, project ID, and CNAME target.")
                .ValidateOnStart();
            services.AddOptions<CertificateOptions>()
                .Bind(configuration.GetSection(CertificateOptions.SectionName))
                .PostConfigure(options =>
                    options.PublicBaseUrl = configuration["PUBLIC_BASE_URL"] ?? options.PublicBaseUrl)
                .Validate(options => Uri.TryCreate(options.PublicBaseUrl, UriKind.Absolute, out var uri)
                    && uri.Scheme == Uri.UriSchemeHttps,
                    "Certificates:PublicBaseUrl must be an HTTPS URL.")
                .ValidateOnStart();
            services.AddOptions<PaymentSecurityOptions>()
                .Configure(options =>
                {
                    options.SecretKey = configuration["CHAPA_SECRET_KEY"] ?? string.Empty;
                    options.WebhookSecret = configuration["CHAPA_WEBHOOK_SECRET"] ?? string.Empty;
                    options.CallbackUrl = configuration["CHAPA_CALLBACK_URL"] ?? string.Empty;
                    options.ReturnOrigins = configuration["PAYMENT_RETURN_ORIGINS"] ?? string.Empty;
                })
                .Validate(options => !configuration.GetValue<bool>("Features:Offers")
                    || (options.SecretKey.Length > 16
                        && options.WebhookSecret.Length > 16
                        && Uri.TryCreate(options.CallbackUrl, UriKind.Absolute, out var callback)
                        && callback.Scheme == Uri.UriSchemeHttps
                        && options.ReturnOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries).Length > 0),
                    "Offers require Chapa credentials, a webhook secret, an HTTPS callback, and payment return origins.")
                .ValidateOnStart();
            services.AddScoped<IOpaqueSessionService, OpaqueSessionService>();
            services.AddScoped<IHostPolicyService, HostPolicyService>();
            services.AddScoped<IAccountTokenService, AccountTokenService>();
            services.AddSingleton<IApplicationSecretProtector, ApplicationSecretProtector>();
            services.AddHttpClient<ITransactionalEmailService, TransactionalEmailService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
            });
            services.AddSingleton<IObjectStorageService, R2ObjectStorageService>();
            services.AddSingleton<IMalwareScanner, ClamAvMalwareScanner>();
            services.AddSingleton<IAssetContentExtractor, AssetContentExtractor>();
            services.AddHttpClient<IImageOcrService, ImageOcrService>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<OcrOptions>>().Value;
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });
            services.AddHttpClient<IDomainDnsVerifier, DomainDnsVerifier>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });
            services.AddHttpClient<IExternalWebsiteVerifier, ExternalWebsiteVerifier>()
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
                {
                    AllowAutoRedirect = false,
                    ConnectCallback = PublicNetworkConnect.ConnectAsync
                })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5));
            services.AddHttpClient<ICustomDomainProvisioner, VercelCustomDomainProvisioner>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
            });
            services.AddSingleton<ICertificateRenderer, CertificatePdfRenderer>();

            // HTTP Client for external APIs
            services.AddHttpClient();

            // Domain Services
            services.AddScoped<SkillProfileService>();
            services.AddScoped<ICourseSharingService, CourseSharingService>();
            services.AddScoped<IResearchService, ResearchService>();
            services.AddScoped<IUnderstandingAgentService, UnderstandingAgentService>();

            // LLM Providers & Gateway
            services.AddHttpClient<GeminiLlmProvider>(client => client.Timeout = TimeSpan.FromSeconds(30));
            services.AddHttpClient<GroqLlmProvider>(client => client.Timeout = TimeSpan.FromSeconds(30));
            services.AddScoped<ILlmGateway, LlmGateway>();

            // Multi-provider academic research (OpenAlex, Semantic Scholar,
            // arXiv, Tavily when configured, Wikimedia).
            services.AddHttpClient<IAcademicResearchService, AcademicResearchService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
            });

            // Educator Studio: source ingestion, analysis and blueprinting.
            services.AddScoped<ISourceIngestionService, SourceIngestionService>();
            services.AddScoped<ISourceAnalysisService, SourceAnalysisService>();
            services.AddScoped<IBlueprintService, BlueprintService>();

            // Named HTTP client for source ingestion (URL fetch / PDF download).
            services.AddHttpClient("SourceIngestion", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Witforge/1.0 (educational source extraction)");
            }).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseProxy = false,
                UseCookies = false,
                ConnectTimeout = TimeSpan.FromSeconds(5),
                MaxResponseHeadersLength = 32,
                PooledConnectionLifetime = TimeSpan.FromMinutes(2),
                ConnectCallback = SourceIngestionService.ConnectToPublicAddressAsync
            });

            // Payments (Chapa)
            services.AddHttpClient<IChapaPaymentService, ChapaPaymentService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
            });

            // YouTube Resource Service
            services.AddScoped<IYouTubeResourceService, YouTubeResourceService>();

            // Image/diagram research (Wikimedia, no API key required)
            services.AddHttpClient<IImageResourceService, WikimediaImageService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(20);
            });

            // Ollama LLM Provider with HTTP Client
            services.AddHttpClient<OllamaLlmProvider>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });

            // Additional Services
            // Registered as both the interface and the concrete type: several
            // controllers (Courses, Modules) depend on the concrete
            // CourseGenerationService for JIT module generation.
            services.AddScoped<CourseGenerationService>();
            services.AddScoped<ICourseGenerationService>(provider =>
                provider.GetRequiredService<CourseGenerationService>());
            services.AddScoped<IIntakeService, IntakeService>();
            services.AddScoped<IAiTutorService, AiTutorService>();
            services.AddScoped<IAdaptiveEngine, AdaptiveEngine>();
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<IActivityService, ActivityService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IVoxService, VoxService>();

            return services;
        }

        private static bool IsBase64Key(string value)
        {
            try
            {
                return Convert.FromBase64String(value).Length == 32;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static IServiceCollection AddWorkerInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<WorkerOptions>()
                .Bind(configuration.GetSection(WorkerOptions.SectionName))
                .Validate(options => options.Generation.MaxInFlight is >= 1 and <= 32,
                    "Workers:Generation:MaxInFlight must be between 1 and 32.")
                .Validate(options => options.Generation.LeaseSeconds > options.Generation.HeartbeatSeconds,
                    "The generation lease must be longer than its heartbeat interval.")
                .Validate(options => options.Generation.MaxAttempts is >= 1 and <= 20,
                    "Workers:Generation:MaxAttempts must be between 1 and 20.")
                .Validate(options => options.Authoring.MaxInFlight is >= 1 and <= 16,
                    "Workers:Authoring:MaxInFlight must be between 1 and 16.")
                .Validate(options => options.Authoring.LeaseSeconds > options.Authoring.HeartbeatSeconds,
                    "The authoring lease must be longer than its heartbeat interval.")
                .Validate(options => options.Assets.MaxInFlight is >= 1 and <= 8,
                    "Workers:Assets:MaxInFlight must be between 1 and 8.")
                .Validate(options => options.Assets.LeaseSeconds > options.Assets.HeartbeatSeconds,
                    "The asset lease must be longer than its heartbeat interval.")
                .Validate(options => options.Domains.MaxInFlight is >= 1 and <= 20,
                    "Workers:Domains:MaxInFlight must be between 1 and 20.")
                .Validate(options => options.Certificates.MaxInFlight is >= 1 and <= 8,
                    "Workers:Certificates:MaxInFlight must be between 1 and 8.")
                .Validate(options => options.Payments.MaxInFlight is >= 1 and <= 50,
                    "Workers:Payments:MaxInFlight must be between 1 and 50.")
                .Validate(options => options.Outbox.BatchSize is >= 1 and <= 200,
                    "Workers:Outbox:BatchSize must be between 1 and 200.")
                .Validate(options => options.Maintenance.IntervalMinutes is >= 5 and <= 1440,
                    "Workers:Maintenance:IntervalMinutes must be between 5 and 1440.")
                .ValidateOnStart();

            services.AddSingleton(_ => WorkerIdentity.Create(configuration["WORKER_INSTANCE_ID"]));
            services.AddHostedService<ModuleGenerationBackgroundService>();
            services.AddHostedService<AuthoringJobBackgroundService>();
            services.AddHostedService<AssetProcessingBackgroundService>();
            services.AddHostedService<DomainVerificationBackgroundService>();
            services.AddHostedService<CertificateRenderBackgroundService>();
            services.AddHostedService<PaymentReconciliationBackgroundService>();
            services.AddHostedService<OutboxDispatcherBackgroundService>();
            services.AddHostedService<OperationalMaintenanceBackgroundService>();
            return services;
        }
    }
}
