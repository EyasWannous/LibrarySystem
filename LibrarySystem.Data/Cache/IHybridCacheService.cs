using Microsoft.Extensions.Caching.Hybrid;

namespace LibrarySystem.Data.Cache;

public interface IHybridCacheService
{
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryFlags flags = HybridCacheEntryFlags.None,
        IEnumerable<string>? tags = null,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        CancellationToken cancellationToken = default
    );

    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
    ValueTask RemoveByTagsAsync(List<string> tags, CancellationToken cancellationToken = default);

    ValueTask SetAsync<T>(
        string key,
        T value,
        HybridCacheEntryFlags flags = HybridCacheEntryFlags.None,
        IEnumerable<string>? tags = null,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        CancellationToken cancellationToken = default
    );
}
