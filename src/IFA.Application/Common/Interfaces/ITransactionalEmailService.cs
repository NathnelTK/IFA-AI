namespace IFA.Application.Common.Interfaces
{
    public sealed record TransactionalEmail(
        string To,
        string Subject,
        string TextBody,
        string HtmlBody,
        string IdempotencyKey);

    public interface ITransactionalEmailService
    {
        Task SendAsync(TransactionalEmail email, CancellationToken cancellationToken = default);
    }
}
