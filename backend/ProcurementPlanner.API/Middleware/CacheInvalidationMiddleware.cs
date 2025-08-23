using ProcurementPlanner.Core.Interfaces;
using ProcurementPlanner.Core.Models;
using System.Text.Json;

namespace ProcurementPlanner.API.Middleware;

public class CacheInvalidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CacheInvalidationMiddleware> _logger;

    public CacheInvalidationMiddleware(RequestDelegate next, ILogger<CacheInvalidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICacheService cacheService)
    {
        // Store the original response body stream
        var originalBodyStream = context.Response.Body;

        try
        {
            // Create a new memory stream for the response body
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            // Continue down the middleware pipeline
            await _next(context);

            // Check if we need to invalidate cache based on the request
            if (ShouldInvalidateCache(context))
            {
                await InvalidateCacheAsync(context, cacheService);
            }

            // Copy the contents of the new memory stream to the original stream
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static bool ShouldInvalidateCache(HttpContext context)
    {
        // Only invalidate cache for successful POST, PUT, DELETE operations
        if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300)
            return false;

        var method = context.Request.Method.ToUpperInvariant();
        return method is "POST" or "PUT" or "DELETE" or "PATCH";
    }

    private async Task InvalidateCacheAsync(HttpContext context, ICacheService cacheService)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            // Invalidate cache based on the API endpoint
            if (path.Contains("/orders"))
            {
                _logger.LogDebug("Invalidating order-related cache due to {Method} {Path}", context.Request.Method, path);
                await cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllOrders);
                await cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllDashboard);
            }
            else if (path.Contains("/suppliers"))
            {
                _logger.LogDebug("Invalidating supplier-related cache due to {Method} {Path}", context.Request.Method, path);
                await cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllSupplier);
                await cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllDashboard);
            }
            else if (path.Contains("/procurement"))
            {
                _logger.LogDebug("Invalidating procurement-related cache due to {Method} {Path}", context.Request.Method, path);
                await cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllPurchaseOrders);
                await cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllDashboard);
                await cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllSupplier);
            }
            else if (path.Contains("/reports"))
            {
                _logger.LogDebug("Invalidating report-related cache due to {Method} {Path}", context.Request.Method, path);
                await cacheService.RemoveByPatternAsync(CacheKeys.Patterns.AllReports);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for {Method} {Path}", context.Request.Method, path);
            // Don't throw - cache invalidation failure shouldn't break the request
        }
    }
}