using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Entities;

namespace AutoWarm.Domain.Interfaces;

public interface IMailProvider
{
    Task<bool> ValidateCredentialsAsync(MailAccount account, CancellationToken cancellationToken = default);
    Task<string> SendEmailAsync(MailAccount account, string to, string subject, string body, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WarmupEmailLog>> FetchRecentEmailsAsync(MailAccount account, CancellationToken cancellationToken = default);
    Task MarkAsImportantAsync(MailAccount account, string messageId, CancellationToken cancellationToken = default);
    Task MoveToInboxAsync(MailAccount account, string messageId, CancellationToken cancellationToken = default);
}
