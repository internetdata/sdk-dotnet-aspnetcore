# Changelog

What each release changed for you, newest first. Each line is a commit's summary, linked to its full description and diff.

## 1.0.0 - 2026-11-14

### Features

- Attach the visitor's InternetData record to every request's `HttpContext`, read with `GetInternetData`: the location, the network and the company behind the address, and the anonymity and hosting flags your plan serves ([`2cbcbd5`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/2cbcbd553cef78c6d5436947dae7ec8813bc15fa))
- Refuse a request with a `BlockCondition` written in the shape of a result, by country, network type, anonymity or any other member, with 403 or an `OnBlocked` of your own ([`2cbcbd5`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/2cbcbd553cef78c6d5436947dae7ec8813bc15fa))
- Choose where the client address comes from: the connection's address by default, `IpSelectors.ForwardedFor`, `IpSelectors.Header` for a header your edge writes, or a function of your own ([`2cbcbd5`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/2cbcbd553cef78c6d5436947dae7ec8813bc15fa))
- Fail open on any lookup failure unless `FailClosed` is set, with a 2500 ms bound, no retry and a cache per middleware by default ([`2cbcbd5`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/2cbcbd553cef78c6d5436947dae7ec8813bc15fa))
