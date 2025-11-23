using System;
using AutoWarm.Domain.Enums;

namespace AutoWarm.Application.DTOs.MailAccounts;

public record MailAccountDto(
    Guid Id,
    string DisplayName,
    string EmailAddress,
    MailProviderType ProviderType,
    MailAccountStatus Status,
    DateTime CreatedAt,
    DateTime? LastHealthCheckAt);
