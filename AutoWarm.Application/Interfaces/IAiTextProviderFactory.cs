using AutoWarm.Domain.Interfaces;

namespace AutoWarm.Application.Interfaces;

public interface IAiTextProviderFactory
{
    IAiTextProvider Create();
}
