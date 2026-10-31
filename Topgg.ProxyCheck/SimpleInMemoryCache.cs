using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

namespace Topgg.ProxyCheck
{
    /// <summary>
    /// Keeps results in memory for a fixed time. Safe to share between threads, since one
    /// <see cref="ProxyCheck"/> instance usually serves many requests at once.
    /// </summary>
    public class SimpleInMemoryCache : IProxyCheckCacheProvider
    {
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

        private readonly ConcurrentDictionary<CacheKey, CacheEntry> _entries = new();
        private readonly TimeSpan _maxCacheAge = TimeSpan.FromHours(1);
        private long _nextCleanupTicks;

        private readonly record struct CacheKey(IPAddress IpAddress, ProxyCheckRequestOptions Options);

        private readonly record struct CacheEntry(ProxyCheckResult.IpResult Result, DateTimeOffset ExpiresAt);

        public SimpleInMemoryCache()
        {
        }

        public SimpleInMemoryCache(TimeSpan maxCacheAge)
        {
            _maxCacheAge = maxCacheAge;
        }

        public ProxyCheckResult.IpResult? Get(IPAddress ipAddress, ProxyCheckRequestOptions options)
        {
            if (!_entries.TryGetValue(new CacheKey(ipAddress, options), out var entry))
                return null;

            return entry.ExpiresAt > DateTimeOffset.UtcNow ? entry.Result : null;
        }

        public void Set(IPAddress ipAddress, ProxyCheckRequestOptions options, ProxyCheckResult.IpResult result)
        {
            var now = DateTimeOffset.UtcNow;
            RemoveExpired(now);

            // ProxyCheck passes its own options object and changes it in place when a setting
            // changes, so the key keeps a copy.
            _entries[new CacheKey(ipAddress, options.Clone())] = new CacheEntry(result, now + _maxCacheAge);
        }

        private void RemoveExpired(DateTimeOffset now)
        {
            var nextCleanupTicks = Interlocked.Read(ref _nextCleanupTicks);
            if (now.UtcTicks < nextCleanupTicks)
                return;

            // Only the caller that moves the next cleanup forward sweeps, the rest skip this round.
            var movedCleanup = Interlocked.CompareExchange(
                ref _nextCleanupTicks,
                (now + CleanupInterval).UtcTicks,
                nextCleanupTicks) == nextCleanupTicks;
            if (!movedCleanup)
                return;

            foreach (var entry in _entries)
            {
                // A record refreshed between this check and the removal is dropped too. That only
                // costs one extra lookup.
                if (entry.Value.ExpiresAt <= now)
                    _entries.TryRemove(entry.Key, out _);
            }
        }
    }
}
