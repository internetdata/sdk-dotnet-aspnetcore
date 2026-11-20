# [<img src="https://s3.internetdata.io/internetdata-public/brand/mark.svg" alt="InternetData" height="28"/>](https://internetdata.io/) InternetData ASP.NET Core Middleware

[![NuGet](https://img.shields.io/nuget/v/InternetData.AspNetCore.svg)](https://www.nuget.org/packages/InternetData.AspNetCore)
[![license](https://img.shields.io/github/license/internetdata/sdk-dotnet-aspnetcore.svg)](LICENSE)

The official ASP.NET Core middleware for the [InternetData](https://internetdata.io) API.

It looks up the visitor behind each request, where they are, which network they come from and whether it's a VPN, a proxy, Tor or a hosting server, and puts the answer on `HttpContext`. Blocking is opt-in.

MVC, minimal APIs, Razor Pages and Blazor Server all sit on the same pipeline, so this covers all of them.

## Getting Started

```bash
dotnet add package InternetData.AspNetCore
```

Targets `net8.0`, so it loads on .NET 8 and every later runtime.

Without an API key a lookup is on Unlimited, the free plan: the country, the network and the company behind the address, Tor, relay, anycast, carrier and satellite space and the service flags, with no daily or monthly cap. Every lookup your app makes comes from your server's one address, though, so a busy site will see bursts refused. A paid plan answers every field, VPN, the proxy flags, hosting, CDN and the city among them; create a key in the [console](https://app.internetdata.io). A free account tries every field.

```csharp
using InternetData.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseInternetData(o => o.ApiKey = builder.Configuration["INTERNETDATA_API_KEY"]);

app.MapGet("/", (HttpContext context) =>
{
    var found = context.GetInternetData();
    return found?.Result is { } result ? $"Hello, visitor from {result.Location.CountryName}" : "Hello";
});

app.Run();
```

By default nothing is blocked. Every request carries a `Lookup` and your own code decides what it means: a currency to show, a sign-up to review, a page to keep from some countries.

## Blocking

Set a `BlockCondition` and a matching request is answered with `403` and never reaches your endpoints. `Condition` and `Bound` are in `InternetData.Middleware`:

```csharp
using InternetData.Middleware;

app.UseInternetData(o =>
{
    o.ApiKey = builder.Configuration["INTERNETDATA_API_KEY"];
    o.BlockCondition = new[]
    {
        new Condition { ["location"] = new Condition { ["country"] = new[] { "CU", "IR", "KP" } } },
    };
});
```

A condition is written in the shape of a result, keyed by the same names the API uses, and only the members you name are considered, so it can refuse on anything in the answer. The country works on every plan, and so do the first two of these:

```csharp
// the network's type
new Condition { ["asn"] = new Condition { ["type"] = "hosting" } }

// anycast and satellite space: a list is OR
new[] { new Condition { ["is_anycast"] = true }, new Condition { ["is_satellite"] = true } }

// traffic from servers, not people
new[] { new Condition { ["is_hosting"] = true }, new Condition { ["is_cdn"] = true } }

// anonymity
new[]
{
    new Condition { ["is_vpn"] = true },
    new Condition { ["is_tor"] = true },
    new Condition { ["is_resproxy"] = true },
}

// one provider
new Condition { ["is_vpn"] = true, ["vpn"] = new Condition { ["provider"] = "nordvpn" } }

// a numeric threshold
new Condition { ["resproxy"] = new Condition { ["hits"] = Bound.Gte(5) } }
```

Values are matched by equality, strings without regard to case. An array means any-of. `Bound.Gte`, `Gt`, `Lte` and `Lt` compare numbers and chain into a range (`Bound.Gte(5).AndLt(100)`); every bound you give must hold. Members set to `false` or `null` are ignored, so a condition states the signals you act on; one that constrains nothing would match every request, and is refused when the app starts rather than silently blocking all your traffic.

Replace the refusal with `OnBlocked`:

```csharp
o.OnBlocked = (context, lookup) =>
{
    context.Response.StatusCode = StatusCodes.Status451UnavailableForLegalReasons;
    return context.Response.WriteAsync("Not available in your country");
};
```

## Where the client address comes from

This is the setting that decides whether any of the above works, and it is the one thing only you can get right.

By default the middleware uses `HttpContext.Connection.RemoteIpAddress`. **That is the socket peer** unless your pipeline has `UseForwardedHeaders`. If your app sits behind nginx, a load balancer, or a CDN, every visitor arrives wearing your proxy's address, and that is the address that gets looked up: every visitor gets your proxy's country and network, and if it is a hosting address, a hosting rule blocks everyone. nginx on the same machine is `127.0.0.1` and a load balancer usually has a private address, which the middleware answers locally without a lookup, as an address with no country, and logs a warning.

Either give ASP.NET Core the topology and let it resolve the address for you:

```csharp
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor,
    KnownProxies = { IPAddress.Parse("10.0.0.1") },
});
app.UseInternetData(/* ... */);   // after it, so the connection address is the visitor's
```

…or name the header your edge writes:

```csharp
o.IpSelector = IpSelectors.Header("CF-Connecting-IP");   // or True-Client-IP
```

`IpSelectors.ForwardedFor()` reads the left-most `X-Forwarded-For` entry. Be aware that the left-most entry is whatever the caller sent, because proxies append to that header, so it's only trustworthy when an edge you control overwrites it. If you know how many proxies sit in front, count from the right instead: `IpSelectors.ForwardedFor(1)` is the address your nearest proxy saw.

Anything else, pass your own function. It receives the `HttpContext` and returns an address:

```csharp
o.IpSelector = context => context.Request.Headers["X-Real-IP"].ToString();
```

If the address resolves to a private one, the middleware says so once through `OnWarn`. That is expected on localhost and is the signal to fix your configuration anywhere else.

## When a lookup fails

The request is let through, and the reason is on `Lookup.Error`. Our outage should not become yours, so a network failure, a rate limit or a rejected key all fail open.

```csharp
var found = context.GetInternetData();
if (found?.Error is not null)
{
    logger.LogWarning(found.Error, "internetdata unavailable");
}
```

Set `FailClosed = true` to block instead. Private addresses are answered locally and never fail, so this will not lock you out in development.

## Cost and latency

Answers are cached per middleware for an hour, so a returning visitor costs nothing, and private addresses never leave the process. A cache miss is one request to our API, bounded at 2500 ms by default and not retried: on a request path, failing open quickly beats holding a visitor while we try again. Both are adjustable, and so is the cache, through a client you build yourself and pass as `Client`.

Mount it on the branch that matters rather than the whole app, or skip what you do not care about:

```csharp
o.Skip = context => context.Request.Path.StartsWithSegments("/healthz");
```

If you already hold an `InternetDataClient`, pass it as `Client` and the middleware will share it rather than building a second cache.

Beyond a few million distinct visitors a day, stop calling the API per request: look addresses up locally in [our databases](https://internetdata.io/databases) instead.

## Absent is not false

Unlimited, with a key or without one, leaves out `IsVpn`, `IsHosting`, `IsCdn` and the four proxy flags, the detail behind them, and the paid members inside `Location`, `Asn`, `Company` and `Abuse`, such as `Location.City`. A field your plan does not include is `null`, which means "not in your plan" rather than "checked, and no".

```csharp
found.Result.IsVpn == true   // when you only want the flag
```

A `BlockCondition` naming a member your plan does not serve, `location.city` on Unlimited included, can never match, so the middleware warns once instead of failing silently. Set `OnMissingField = MiddlewareOptions<HttpContext>.MissingField.Throw` to make it an error.

## Other Libraries

There are official InternetData client libraries available for many languages including PHP, Python, Go, Java, and Ruby. See our GitHub at https://github.com/internetdata for more.

## About InternetData

Geolocation, anonymity, ownership and network data for IP addresses, AS numbers and domains, as an API and as databases you download and query yourself.

[<img src="https://s3.internetdata.io/internetdata-public/brand/mark.svg" alt="InternetData" height="64"/>](https://internetdata.io/)

## License

This project is licensed under the [MIT License](LICENSE).
