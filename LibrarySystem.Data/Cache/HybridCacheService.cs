namespace LibrarySystem.Data.Cache;

using Microsoft.Extensions.Caching.Hybrid;
using StackExchange.Redis;

public class HybridCacheService : IHybridCacheService
{
    private static readonly TimeSpan _defaultExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _defaultLocalExpiration = TimeSpan.FromMinutes(1);

    private readonly HybridCache _hybridCache;
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public HybridCacheService(
        HybridCache hybridCache,
        IConnectionMultiplexer connectionMultiplexer)
    {
        _hybridCache = hybridCache;
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryFlags flags = HybridCacheEntryFlags.None,
        IEnumerable<string>? tags = null,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        CancellationToken cancellationToken = default)
    {
        T? cacheItem = await _hybridCache.GetOrCreateAsync<T>(key, factory, new HybridCacheEntryOptions
        {
            Expiration = expiration ?? _defaultExpiration,
            LocalCacheExpiration = localExpiration ?? _defaultLocalExpiration,
            Flags = flags
        }, tags, cancellationToken);

        if (cacheItem is not null)
            return cacheItem;

        cacheItem = await factory(cancellationToken);

        await SetAsync(key, cacheItem, flags, tags, expiration, localExpiration, cancellationToken);

        return cacheItem;
    }

    public async ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryFlags flags = HybridCacheEntryFlags.None,
        IEnumerable<string>? tags = null,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        CancellationToken cancellationToken = default)
    {
        await _hybridCache.SetAsync<T>(key, value, new HybridCacheEntryOptions
        {
            Expiration = expiration ?? _defaultExpiration,
            LocalCacheExpiration = localExpiration ?? _defaultLocalExpiration,
            Flags = flags
        }, tags, cancellationToken);
    }

    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _hybridCache.RemoveAsync(key, cancellationToken);

        var subscriber = _connectionMultiplexer.GetSubscriber();
        await subscriber.PublishAsync(
            RedisChannel.Literal(LibrarySystemConstants.RedisChannelCacheInvalidationKeyName),
            new RedisValue(key)
        );

    }

    public async ValueTask RemoveByTagsAsync(List<string> tags, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();
        var subscriber = _connectionMultiplexer.GetSubscriber();

        foreach (var tag in tags)
        {
            tasks.Add(_hybridCache.RemoveByTagAsync(tag, cancellationToken).AsTask());
            tasks.Add(
                subscriber.PublishAsync(
                    RedisChannel.Literal(LibrarySystemConstants.RedisChannelCacheInvalidationTagName),
                    new RedisValue(tag)
                )
           );
        }

        await Task.WhenAll(tasks);
    }
}
