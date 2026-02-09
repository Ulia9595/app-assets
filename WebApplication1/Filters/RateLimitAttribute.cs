using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace WebApplication1.Filters
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RateLimitAttribute : ActionFilterAttribute
    {
        private readonly int _maxRequests;
        private readonly int _timeWindowMinutes;
        private readonly RateLimitType _limitType;

        public RateLimitAttribute(
            int maxRequests = 100,
            int timeWindowMinutes = 1,
            RateLimitType limitType = RateLimitType.Ip)
        {
            _maxRequests = maxRequests;
            _timeWindowMinutes = timeWindowMinutes;
            _limitType = limitType;
        }

        public override async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var memoryCache = context.HttpContext.RequestServices
                .GetRequiredService<IMemoryCache>();
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILogger<RateLimitAttribute>>();

            string cacheKey = GetCacheKey(context);

            if (string.IsNullOrEmpty(cacheKey))
            {
                await next();
                return;
            }

            var now = DateTime.UtcNow;
            var windowStart = now.AddMinutes(-_timeWindowMinutes);

            if (!memoryCache.TryGetValue<List<DateTime>>(cacheKey, out var requests))
            {
                requests = new List<DateTime>();
            }

            requests = requests.Where(r => r > windowStart).ToList();

            if (requests.Count >= _maxRequests)
            {
                logger.LogWarning(
                    "Rate limit exceeded. Key: {Key}, Requests: {Count}/{Max} in {Minutes}min",
                    cacheKey, requests.Count, _maxRequests, _timeWindowMinutes);

                context.Result = new ObjectResult(new
                {
                    success = false,
                    error = $"Слишком много запросов. Максимум {_maxRequests} запросов за {_timeWindowMinutes} минут.",
                    retryAfter = requests.Min().AddMinutes(_timeWindowMinutes) - now
                })
                {
                    StatusCode = 429
                };

                context.HttpContext.Response.Headers["Retry-After"] =
                    ((int)(requests.Min().AddMinutes(_timeWindowMinutes) - now).TotalSeconds).ToString();

                return;
            }

            requests.Add(now);

            memoryCache.Set(cacheKey, requests,
                TimeSpan.FromMinutes(_timeWindowMinutes + 1));

            await next();
        }

        private string GetCacheKey(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;

            switch (_limitType)
            {
                case RateLimitType.Ip:
                    var ip = httpContext.Connection.RemoteIpAddress?.ToString();
                    if (string.IsNullOrEmpty(ip))
                    {
                        ip = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                            ?? httpContext.Request.Headers["X-Real-IP"].FirstOrDefault()
                            ?? "unknown";
                    }
                    return $"ratelimit:ip:{ip}:{httpContext.Request.Path}";

                case RateLimitType.User:
                    var userId = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId)) return null;
                    return $"ratelimit:user:{userId}:{httpContext.Request.Path}";

                case RateLimitType.Global:
                    return $"ratelimit:global:{httpContext.Request.Path}";

                default:
                    return null;
            }
        }
    }

    public enum RateLimitType
    {
        Ip,
        User,
        Global
    }
}