using System.Net;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using InternetData.Middleware;

using Xunit;

namespace InternetData.AspNetCore.Tests;

/// <summary>
/// <c>AllowBots</c> built the way the README builds it, from a PUBLIC peer, against a recording API
/// that answers the peer as a VPN with the verdict it names for the agent that arrived: the agent
/// it records is the request's own, so a middleware sending none, or another, fails.
/// </summary>
public class AllowBotsTests
{
    private const string PublicIp = "45.83.91.1";
    private const string Peer = "66.249.66.1";
    private const string Googlebot = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
    private const string Chrome = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/151.0.0.0 Safari/537.36";

    // The peer a request comes from, when it is not PublicIp, since a test server has no socket.
    private const string PeerHeader = "X-Test-Peer";

    private static string Crawler(int n) => $"Mozilla/5.0 (compatible; Examplebot/{n}.0)";

    [Theory]
    [InlineData("verified", HttpStatusCode.OK)]
    [InlineData("spoofed", HttpStatusCode.Forbidden)]
    [InlineData("not_bot", HttpStatusCode.Forbidden)]
    public async Task AVerifiedCrawlerPassesAConditionItOtherwiseMeets(string verdict, HttpStatusCode status)
    {
        await using var api = await AgentApi.StartAsync(verdict);
        await using var app = await App(api, allowBots: true);

        var answer = await Send(app, Googlebot);

        Assert.Equal(status, answer.StatusCode);
        Assert.Equal(new string?[] { Googlebot }, api.Agents);
        if (status == HttpStatusCode.OK)
        {
            Assert.Equal($"{PublicIp} verified", await answer.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task OffSendsNoAgentAndRefusesTheCrawler()
    {
        await using var api = await AgentApi.StartAsync("verified");
        await using var app = await App(api, allowBots: false);

        var answer = await Send(app, Googlebot);

        Assert.Equal(HttpStatusCode.Forbidden, answer.StatusCode);
        Assert.Equal(new string?[] { null }, api.Agents);
    }

    [Fact]
    public async Task AnAbsentHeaderSendsNone()
    {
        await using var api = await AgentApi.StartAsync("verified");
        await using var app = await App(api, allowBots: true);

        var answer = await Send(app, null);

        Assert.Equal(HttpStatusCode.Forbidden, answer.StatusCode);
        Assert.Equal(new string?[] { null }, api.Agents);
    }

    // A crawler's agent is sent with its lookup, and a browser's is not: the browser's request
    // arrives at the API with none, so a VPN's address is refused whatever the API would say.
    [Fact]
    public async Task AnAgentNamingNoCrawlerIsNotSent()
    {
        await using var api = await AgentApi.StartAsync("verified");
        await using var app = await App(api, allowBots: true);

        var answer = await Send(app, Chrome);

        Assert.Equal(HttpStatusCode.Forbidden, answer.StatusCode);
        Assert.Equal(new string?[] { null }, api.Agents);
    }

    // The API here vouches for any agent. Past the address's four the lookup carries none, so the
    // fifth arrives bare and is refused, a known agent is sent again, and another address has its
    // own four. The client's cache is off, or a known agent's repeat would be answered from it and
    // never reach the API.
    [Fact]
    public async Task OneAddressIsLookedUpWithAtMostFourCrawlerAgentsAndTheFifthIsRefused()
    {
        await using var api = await AgentApi.StartAsync("verified");
        await using var app = await App(api, allowBots: true, cache: false);

        for (var n = 1; n <= 4; n++)
        {
            Assert.Equal(HttpStatusCode.OK, (await Send(app, Crawler(n), Peer)).StatusCode);
        }
        var fifth = await Send(app, Crawler(5), Peer);
        Assert.Equal(HttpStatusCode.Forbidden, fifth.StatusCode);
        var known = await Send(app, Crawler(1), Peer);
        Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        Assert.Equal(
            new string?[] { Crawler(1), Crawler(2), Crawler(3), Crawler(4), null, Crawler(1) }, api.Agents);

        var other = await Send(app, Crawler(5), "66.249.66.2");
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
        Assert.Equal(Crawler(5), api.Agents[^1]);
    }

    private static async Task<HttpResponseMessage> Send(WebApplication app, string? agent, string? peer = null)
    {
        var client = app.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        if (peer is not null)
        {
            request.Headers.Add(PeerHeader, peer);
        }
        // The test client's own User-Agent, if it carries one, is no visitor's.
        client.DefaultRequestHeaders.UserAgent.Clear();
        request.Headers.Remove("User-Agent");
        if (agent is not null)
        {
            request.Headers.TryAddWithoutValidation("User-Agent", agent);
        }
        return await client.SendAsync(request);
    }

    // The README's setup, AllowBots set, refusing every VPN.
    private static async Task<WebApplication> App(AgentApi api, bool allowBots, bool cache = true)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        var app = builder.Build();
        app.Use((context, next) =>
        {
            context.Connection.RemoteIpAddress = IPAddress.Parse(
                context.Request.Headers.TryGetValue(PeerHeader, out var peer) ? peer.ToString() : PublicIp);
            return next();
        });
        app.UseInternetData(o =>
        {
            o.ApiKey = "id_example_key";
            o.BaseUrl = api.BaseUrl;
            if (!cache)
            {
                o.Client = new InternetDataClient(new InternetDataClientOptions
                {
                    ApiKey = o.ApiKey, BaseUrl = o.BaseUrl, CacheEnabled = false, Retries = 0,
                });
            }
            o.AllowBots = allowBots;
            o.BlockCondition = new[] { new Condition { ["is_vpn"] = true } };
        });
        app.MapGet("/", (HttpContext context) =>
        {
            var result = context.GetInternetData()?.Result;
            return $"{result?.Ip} {result?.UserAgent?.Verdict}";
        });
        await app.StartAsync();
        return app;
    }

    private sealed class AgentApi : IAsyncDisposable
    {
        private readonly WebApplication app;
        private readonly List<string?> agents = new();

        private AgentApi(WebApplication app) => this.app = app;

        internal string BaseUrl => app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();

        internal IReadOnlyList<string?> Agents
        {
            get
            {
                lock (agents)
                {
                    return agents.ToArray();
                }
            }
        }

        internal static async Task<AgentApi> StartAsync(string verdict)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var app = builder.Build();
            var api = new AgentApi(app);
            app.Run(async context =>
            {
                string? agent = context.Request.Query.TryGetValue("user_agent", out var sent) ? sent.ToString() : null;
                lock (api.agents)
                {
                    api.agents.Add(agent);
                }
                // The corpus's Unlimited answer for the peer, as a VPN, with the verdict a paid one carries.
                var ip = context.Request.Path.Value!.TrimStart('/');
                var answer = JsonNode.Parse(Stub.Lookup(isVpn: true).Replace("$IP", ip, StringComparison.Ordinal))!.AsObject();
                if (agent is not null)
                {
                    answer["user_agent"] = new JsonObject { ["agent"] = "Googlebot", ["kind"] = "bot", ["verdict"] = verdict };
                }
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(answer.ToJsonString());
            });
            await app.StartAsync();
            return api;
        }

        public ValueTask DisposeAsync() => app.DisposeAsync();
    }
}
