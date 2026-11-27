# Changelog

What each release changed for you, newest first. Each line is a commit's summary, linked to its full description and diff.

## 2.1.2 - 2026-11-27

### Features

- Require InternetData 3.3.0: an answer without the AS's type reads ([`89d8802`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/89d88029a7d577ced5e80673e4b7339e60bc88c6))

## 2.1.1 - 2026-11-26

### Features

- Require InternetData 3.2.0: a keyless answer without the bot, carrier, satellite and service flags reads ([`b4d41f7`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/b4d41f7439918b9c31050f5d6c1d49ebcc829801))

## 2.1.0 - 2026-11-25

### Features

- Require InternetData 3.1.0: a visitor's user agent, and AllowBots in the middleware core ([`c9a1a6d`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/c9a1a6d980a40cf4a210844d84c4312accf31c91))
- AllowBots lets a verified crawler through ([`2e5aa98`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/2e5aa98238284da5290b0ce3d1849ef2ec32c9af))

## 2.0.0 - 2026-11-24

### Breaking changes

- Require InternetData 3.0.0: a vocabulary value the API adds reads, and a list member matches any element ([`8d0badc`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/8d0badc4c6ae7899064fbc99c344cb9876bf4742))

## 1.0.6 - 2026-11-23

### Fixes

- Require InternetData 2.15.2: it says what a bot's provider and provider name hold for an address that is not verified ([`01c9a14`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/01c9a14af806ae1b0950ff1ac2970b4ab20f796a))

## 1.0.5 - 2026-11-23

### Fixes

- Require InternetData 2.15.1: it refuses an answer missing the AS's country or RPKI status ([`e0825ba`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/e0825ba4544ec4c97e65371f04f26577edfe3db9))

## 1.0.4 - 2026-11-22

### Features

- Require InternetData 2.15.0: it reads whether a bot uses an address, and whose ([`c6dc2c9`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/c6dc2c9d45a1b58f486c33cded0c8795214058cc))

## 1.0.3 - 2026-11-20

### Fixes

- Require InternetData 2.14.0: it reads a paid domain answer's new members ([`36b425a`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/36b425a881621c4ffe54363842c072a138ebbda2))

## 1.0.2 - 2026-11-20

### Fixes

- Require InternetData 2.13.0: a reduced answer is cached a minute at most ([`ff10a36`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/ff10a369d1474f3540f87a1d45fd70b373a06d12))

## 1.0.1 - 2026-11-14

### Fixes

- Require InternetData 2.9.1: a 403 can mean the key lacks the ip.lookup scope ([`927a335`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/927a33523aad32f3919e9628454608677997c340))

## 1.0.0 - 2026-11-14

### Features

- Attach the visitor's InternetData record to every request's `HttpContext`, read with `GetInternetData`: the location, the network and the company behind the address, and the anonymity and hosting flags your plan serves ([`2cbcbd5`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/2cbcbd553cef78c6d5436947dae7ec8813bc15fa))
- Refuse a request with a `BlockCondition` written in the shape of a result, by country, network type, anonymity or any other member, with 403 or an `OnBlocked` of your own ([`2cbcbd5`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/2cbcbd553cef78c6d5436947dae7ec8813bc15fa))
- Choose where the client address comes from: the connection's address by default, `IpSelectors.ForwardedFor`, `IpSelectors.Header` for a header your edge writes, or a function of your own ([`2cbcbd5`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/2cbcbd553cef78c6d5436947dae7ec8813bc15fa))
- Fail open on any lookup failure unless `FailClosed` is set, with a 2500 ms bound, no retry and a cache per middleware by default ([`2cbcbd5`](https://github.com/internetdata/sdk-dotnet-aspnetcore/commit/2cbcbd553cef78c6d5436947dae7ec8813bc15fa))
