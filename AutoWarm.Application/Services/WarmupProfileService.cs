using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs.WarmupProfiles;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;

namespace AutoWarm.Application.Services;

public class WarmupProfileService : IWarmupProfileService
{
    private readonly IMailAccountRepository _mailAccounts;
    private readonly IWarmupProfileRepository _profiles;
    private readonly IUnitOfWork _unitOfWork;

    public WarmupProfileService(
        IMailAccountRepository mailAccounts,
        IWarmupProfileRepository profiles,
        IUnitOfWork unitOfWork)
    {
        _mailAccounts = mailAccounts;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<WarmupProfileDto>> GetByMailAccountAsync(Guid userId, Guid mailAccountId, CancellationToken cancellationToken = default)
    {
        var account = await _mailAccounts.GetByIdAsync(mailAccountId, cancellationToken);
        if (account is null || account.UserId != userId)
        {
            throw new InvalidOperationException("Mail account not found.");
        }

        var profiles = await _profiles.GetByMailAccountAsync(mailAccountId, cancellationToken);
        return profiles.Select(MapToDto).ToArray();
    }

    public async Task<WarmupProfileDto> CreateAsync(Guid userId, CreateWarmupProfileRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _mailAccounts.GetByIdAsync(request.MailAccountId, cancellationToken);
        if (account is null || account.UserId != userId)
        {
            throw new InvalidOperationException("Mail account not found.");
        }

        var startLocal = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Local);
        var startUtc = startLocal.ToUniversalTime();

        var profile = new WarmupProfile
        {
            Id = Guid.NewGuid(),
            MailAccountId = request.MailAccountId,
            IsEnabled = request.IsEnabled,
            StartDate = startUtc,
            DailyMinEmails = request.DailyMinEmails,
            DailyMaxEmails = request.DailyMaxEmails,
            ReplyRate = request.ReplyRate,
            MaxDurationDays = request.MaxDurationDays,
            CurrentDay = 0,
            TimeWindowStart = request.TimeWindowStart,
            TimeWindowEnd = request.TimeWindowEnd,
            UseRandomization = request.UseRandomization
        };

        await _profiles.AddAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(profile);
    }

    public async Task<WarmupProfileDto> UpdateAsync(Guid userId, Guid id, UpdateWarmupProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _profiles.GetByIdAsync(id, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException("Warmup profile not found.");
        }

        var account = await _mailAccounts.GetByIdAsync(profile.MailAccountId, cancellationToken);
        if (account is null || account.UserId != userId)
        {
            throw new InvalidOperationException("Mail account not found.");
        }

        profile.IsEnabled = request.IsEnabled;
        profile.DailyMinEmails = request.DailyMinEmails;
        profile.DailyMaxEmails = request.DailyMaxEmails;
        profile.ReplyRate = request.ReplyRate;
        profile.MaxDurationDays = request.MaxDurationDays;
        profile.TimeWindowStart = request.TimeWindowStart;
        profile.TimeWindowEnd = request.TimeWindowEnd;
        profile.UseRandomization = request.UseRandomization;

        await _profiles.UpdateAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(profile);
    }

    public async Task DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken = default)
    {
        var profile = await _profiles.GetByIdAsync(id, cancellationToken);
        if (profile is null)
        {
            return;
        }

        var account = await _mailAccounts.GetByIdAsync(profile.MailAccountId, cancellationToken);
        if (account is null || account.UserId != userId)
        {
            throw new InvalidOperationException("Mail account not found.");
        }

        await _profiles.DeleteAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static WarmupProfileDto MapToDto(WarmupProfile profile)
    {
        return new WarmupProfileDto(
            profile.Id,
            profile.MailAccountId,
            profile.IsEnabled,
            profile.StartDate,
            profile.DailyMinEmails,
            profile.DailyMaxEmails,
            profile.ReplyRate,
            profile.MaxDurationDays,
            profile.CurrentDay,
            profile.TimeWindowStart,
            profile.TimeWindowEnd,
            profile.UseRandomization);
    }
}
