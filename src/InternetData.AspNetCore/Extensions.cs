using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using InternetData.Middleware;

namespace InternetData.AspNetCore;

/// <summary>Mounting the middleware.</summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Look up the visitor behind every request that reaches this point in the pipeline.
    /// </summary>
    /// <remarks>
    /// Place it after <c>UseForwardedHeaders</c> (so the connection address is the visitor's) and
    /// after <c>UseStaticFiles</c> (so an asset is served without a lookup). A misconfigured
    /// condition throws here rather than on the first request.
    /// </remarks>
    public static IApplicationBuilder UseInternetData(
        this IApplicationBuilder app, Action<InternetDataOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        InternetDataOptions options = new();
        configure(options);
        return app.UseMiddleware<InternetDataMiddleware>(options);
    }

    /// <inheritdoc cref="UseInternetData(IApplicationBuilder, Action{InternetDataOptions})"/>
    public static IApplicationBuilder UseInternetData(
        this IApplicationBuilder app, InternetDataOptions options) =>
        app.UseMiddleware<InternetDataMiddleware>(options);
}

/// <summary>Reading what the middleware found.</summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// What the middleware found out about this visitor, or null when it did not run for this
    /// request - it is not mounted on this branch, or <c>Skip</c> claimed it.
    /// </summary>
    public static Lookup? GetInternetData(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(InternetDataMiddleware.ItemKey, out object? found)
            ? found as Lookup
            : null;
    }
}
