using AutoWarm.Domain.Entities;
using AutoWarm.Domain.Interfaces;

namespace AutoWarm.Application.Interfaces;

public interface IMailProviderFactory
{
    IMailProvider Resolve(MailAccount account);
}
