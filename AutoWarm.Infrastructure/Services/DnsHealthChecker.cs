using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs.MailAccounts;
using AutoWarm.Application.Interfaces;
using DnsClient;

namespace AutoWarm.Infrastructure.Services;

public class DnsHealthChecker : IDnsHealthChecker
{
    private readonly LookupClient _lookupClient = new(new LookupClientOptions
    {
        Timeout = TimeSpan.FromSeconds(5),
        UseCache = true
    });

    public async Task<DnsCheckDto> CheckAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        var domain = emailAddress.Contains('@') ? emailAddress.Split('@')[1] : emailAddress;
        var spf = await CheckSpf(domain, cancellationToken);
        var dmarc = await CheckDmarc(domain, cancellationToken);
        var dkim = await CheckDkim(domain, cancellationToken);
        var mx = await CheckMx(domain, cancellationToken);
        var reverseDns = await CheckReverseDns(domain, cancellationToken);

        return new DnsCheckDto(Guid.Empty, emailAddress, spf, dkim, dmarc, mx, reverseDns);
    }

    private async Task<string> CheckSpf(string domain, CancellationToken ct)
    {
        try
        {
            var result = await _lookupClient.QueryAsync(domain, QueryType.TXT, cancellationToken: ct);
            var txts = result.AllRecords.OfType<DnsClient.Protocol.TxtRecord>().SelectMany(r => r.Text);
            return txts.Any(t => t.Contains("v=spf1", StringComparison.OrdinalIgnoreCase)) ? "PASS" : "FAIL";
        }
        catch
        {
            return "ERROR";
        }
    }

    private async Task<string> CheckDmarc(string domain, CancellationToken ct)
    {
        try
        {
            var queryName = "_dmarc." + domain;
            var result = await _lookupClient.QueryAsync(queryName, QueryType.TXT, cancellationToken: ct);
            var txts = result.AllRecords.OfType<DnsClient.Protocol.TxtRecord>().SelectMany(r => r.Text);
            if (!txts.Any()) return "WARNING (Not set)";
            return txts.Any(t => t.Contains("v=DMARC1", StringComparison.OrdinalIgnoreCase)) ? "PASS" : "WARNING";
        }
        catch
        {
            return "ERROR";
        }
    }

    private async Task<string> CheckDkim(string domain, CancellationToken ct)
    {
        // Without selector we try common 'default' selector as a heuristic.
        try
        {
            var selector = "default._domainkey." + domain;
            var result = await _lookupClient.QueryAsync(selector, QueryType.TXT, cancellationToken: ct);
            var txts = result.AllRecords.OfType<DnsClient.Protocol.TxtRecord>().SelectMany(r => r.Text);
            if (!txts.Any()) return "UNKNOWN (Selector needed)";
            return txts.Any(t => t.Contains("v=DKIM1", StringComparison.OrdinalIgnoreCase)) ? "PASS" : "WARNING";
        }
        catch
        {
            return "ERROR";
        }
    }

    private async Task<string> CheckMx(string domain, CancellationToken ct)
    {
        try
        {
            var result = await _lookupClient.QueryAsync(domain, QueryType.MX, cancellationToken: ct);
            return result.AllRecords.OfType<DnsClient.Protocol.MxRecord>().Any() ? "PASS" : "FAIL";
        }
        catch
        {
            return "ERROR";
        }
    }

    private async Task<string> CheckReverseDns(string domain, CancellationToken ct)
    {
        try
        {
            var mxResult = await _lookupClient.QueryAsync(domain, QueryType.MX, cancellationToken: ct);
            var mxHost = mxResult.AllRecords.OfType<DnsClient.Protocol.MxRecord>().OrderBy(r => r.Preference).FirstOrDefault()?.Exchange.Value;
            if (string.IsNullOrEmpty(mxHost)) return "UNKNOWN";

            var ipResult = await _lookupClient.QueryAsync(mxHost, QueryType.A, cancellationToken: ct);
            var ip = ipResult.AllRecords.OfType<DnsClient.Protocol.ARecord>().FirstOrDefault()?.Address;
            if (ip == null) return "UNKNOWN";

            var reverse = await Dns.GetHostEntryAsync(ip);
            return reverse.HostName?.Contains(domain, StringComparison.OrdinalIgnoreCase) == true ? "PASS" : "WARNING";
        }
        catch
        {
            return "ERROR";
        }
    }
}
