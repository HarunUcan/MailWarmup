using System;

namespace AutoWarm.Application.DTOs.MailAccounts;

public record DnsCheckDto(
    Guid MailAccountId,
    string EmailAddress,
    string Spf,
    string Dkim,
    string Dmarc,
    string Mx,
    string ReverseDns);
