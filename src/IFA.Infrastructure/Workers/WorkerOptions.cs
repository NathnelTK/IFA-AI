namespace IFA.Infrastructure.Workers
{
    public sealed class WorkerOptions
    {
        public const string SectionName = "Workers";

        public GenerationWorkerOptions Generation { get; set; } = new();
        public AuthoringWorkerOptions Authoring { get; set; } = new();
        public AssetWorkerOptions Assets { get; set; } = new();
        public DomainWorkerOptions Domains { get; set; } = new();
        public CertificateWorkerOptions Certificates { get; set; } = new();
        public PaymentWorkerOptions Payments { get; set; } = new();
        public OutboxWorkerOptions Outbox { get; set; } = new();
        public MaintenanceWorkerOptions Maintenance { get; set; } = new();
    }

    public sealed class GenerationWorkerOptions
    {
        public bool Enabled { get; set; } = true;
        public int MaxInFlight { get; set; } = 3;
        public int PollSeconds { get; set; } = 5;
        public int LeaseSeconds { get; set; } = 120;
        public int HeartbeatSeconds { get; set; } = 30;
        public int MaxAttempts { get; set; } = 3;
    }

    public sealed class AuthoringWorkerOptions
    {
        public bool Enabled { get; set; } = true;
        public int MaxInFlight { get; set; } = 2;
        public int PollSeconds { get; set; } = 5;
        public int LeaseSeconds { get; set; } = 180;
        public int HeartbeatSeconds { get; set; } = 30;
    }

    public sealed class AssetWorkerOptions
    {
        public bool Enabled { get; set; } = true;
        public int MaxInFlight { get; set; } = 2;
        public int PollSeconds { get; set; } = 5;
        public int LeaseSeconds { get; set; } = 300;
        public int HeartbeatSeconds { get; set; } = 30;
    }

    public sealed class DomainWorkerOptions
    {
        public bool Enabled { get; set; } = true;
        public int PollSeconds { get; set; } = 30;
        public int LeaseSeconds { get; set; } = 120;
        public int MaxInFlight { get; set; } = 5;
    }

    public sealed class CertificateWorkerOptions
    {
        public bool Enabled { get; set; } = true;
        public int PollSeconds { get; set; } = 10;
        public int LeaseSeconds { get; set; } = 120;
        public int MaxInFlight { get; set; } = 2;
    }

    public sealed class PaymentWorkerOptions
    {
        public bool Enabled { get; set; } = true;
        public int PollSeconds { get; set; } = 30;
        public int LeaseSeconds { get; set; } = 60;
        public int MaxInFlight { get; set; } = 10;
    }

    public sealed class OutboxWorkerOptions
    {
        public bool Enabled { get; set; } = true;
        public int BatchSize { get; set; } = 20;
        public int PollSeconds { get; set; } = 5;
        public int LeaseSeconds { get; set; } = 60;
    }

    public sealed class MaintenanceWorkerOptions
    {
        public bool Enabled { get; set; } = true;
        public int IntervalMinutes { get; set; } = 60;
        public int ProcessedOutboxRetentionDays { get; set; } = 30;
    }

    public sealed class WorkerIdentity
    {
        public WorkerIdentity(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public static WorkerIdentity Create(string? configuredValue = null)
        {
            var value = string.IsNullOrWhiteSpace(configuredValue)
                ? $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}"
                : configuredValue.Trim();
            return new WorkerIdentity(value.Length <= 200 ? value : value[..200]);
        }
    }
}
