using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace InternetData.AspNetCore.Tests;

/// <summary>
/// A transport that answers every lookup from one body and records what it was asked about, so
/// "never touched the network" is asserted rather than assumed.
/// </summary>
internal sealed class StubHandler : HttpMessageHandler
{
    private readonly string body;
    private readonly int status;
    private readonly List<string> asked = new();

    internal StubHandler(string body, int status = 200)
    {
        this.body = body;
        this.status = status;
    }

    /// <summary>Every address this handler was asked about, in arrival order.</summary>
    internal IReadOnlyList<string> Asked
    {
        get
        {
            lock (asked)
            {
                return asked.ToArray();
            }
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ip = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath.TrimStart('/'));
        lock (asked)
        {
            asked.Add(ip);
        }
        return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new StringContent(
                body.Replace("$IP", ip, StringComparison.Ordinal), Encoding.UTF8,
                "application/json"),
            RequestMessage = request,
        });
    }
}

internal static class Stub
{
    /// <summary>A client whose every answer is <paramref name="body"/>.</summary>
    internal static (InternetDataClient Client, StubHandler Handler) Serving(
        string body, int status = 200)
    {
        var handler = new StubHandler(body, status);
        var client = new InternetDataClient(new InternetDataClientOptions
        {
            CacheEnabled = false,
            Retries = 0,
            HttpClient = new HttpClient(handler),
        });
        return (client, handler);
    }

    /// <summary>
    /// The corpus's Unlimited answer for whichever address is asked about, with VPN IP's flag, the
    /// country and any other members a test names.
    /// </summary>
    /// <remarks>
    /// A served answer needs every member the spec requires, so a test's own members go over this
    /// one rather than standing alone. <paramref name="extra"/> is more members, written as they
    /// would follow another in the JSON object.
    /// </remarks>
    internal static string Lookup(bool? isVpn = null, string extra = "", string? country = null)
    {
        var answer = Unlimited();
        answer["ip"] = "$IP";
        if (isVpn is { } flag)
        {
            answer["is_vpn"] = flag;
        }
        if (country is not null)
        {
            answer["location"]!["country"] = country;
        }
        if (extra.Length > 0)
        {
            foreach (var (name, value) in JsonNode.Parse("{" + extra.TrimStart(',') + "}")!.AsObject())
            {
                answer[name] = value?.DeepClone();
            }
        }
        return answer.ToJsonString();
    }

    private static JsonObject Unlimited()
    {
        foreach (var c in Corpus.Data.GetProperty("lookup").EnumerateArray())
        {
            if (c.GetProperty("name").GetString() == "unlimited-answer")
            {
                return JsonNode.Parse(c.GetProperty("body").GetRawText())!.AsObject();
            }
        }
        throw new InvalidOperationException("the corpus has no unlimited-answer lookup case");
    }
}
