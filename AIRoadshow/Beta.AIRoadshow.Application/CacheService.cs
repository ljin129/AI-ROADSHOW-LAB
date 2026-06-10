using Beta.RedisExtensions.StackExchangeExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Beta.AIRoadshow.Application;

/// <summary>
/// Redis 缓存服务
/// </summary>
[Service(ServiceLifetime.Singleton)]
public class CacheService
{
    private readonly IStackExchangeRedisCache _redisCache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(ILogger<CacheService> logger, IStackExchangeRedisCache redisCache)
    {
        _redisCache = redisCache;
        _logger = logger;
    }

    public async Task<TResponse> GetWithRedisAsync<TResponse>(string key)
    {
        return await _redisCache.GetAsync<TResponse>(key);
    }

    public async Task SetWithRedisAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var result = await _redisCache.SetAsync(key, value, expiration ?? TimeSpan.FromMinutes(10));
        if (!result)
        {
            _logger.LogWarning("Set redis cache failed. key={Key}", key);
        }
    }
}
