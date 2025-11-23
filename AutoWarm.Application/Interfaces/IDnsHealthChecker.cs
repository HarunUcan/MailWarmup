using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs.MailAccounts;

namespace AutoWarm.Application.Interfaces;

public interface IDnsHealthChecker
{
    Task<DnsCheckDto> CheckAsync(string emailAddress, CancellationToken cancellationToken = default);
}
