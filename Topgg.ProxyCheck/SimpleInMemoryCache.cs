using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;

namespace Topgg.ProxyCheck
{
    public class SimpleInMemoryCache : IProxyCheckCacheProvider
    {
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

        private readonly ConcurrentDictionary<CacheKey, CacheEntry> _entries = new();
        private readonly TimeSpan _maxCacheAge = TimeSpan.FromHours(1);
        private long _nextCleanupTicks;

        private readonly record struct CacheKey(IPAddress IpAddress, int Options);

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
            if (!_entries.TryGetValue(CreateCacheKey(ipAddress, options), out var entry))
                return null;

            return entry.ExpiresAt > DateTimeOffset.UtcNow ? entry.Result : null;
        }

        public void Set(IPAddress ipAddress, ProxyCheckRequestOptions options, ProxyCheckResult.IpResult result)
        {
            var now = DateTimeOffset.UtcNow;
            RemoveExpired(now);

            _entries[CreateCacheKey(ipAddress, options)] = new CacheEntry(result, now + _maxCacheAge);
        }

        // Packs the options into one bit each, so two keys only match when every option matches.
        // A missing risk level is sent as Disabled, so it is keyed the same way.
        private static CacheKey CreateCacheKey(IPAddress ipAddress, ProxyCheckRequestOptions options)
        {
            var packed = 0;
            if (options.IncludeVPN) packed |= 1 << 0;
            if (options.UseTLS) packed |= 1 << 1;
            if (options.IncludeASN) packed |= 1 << 2;
            if (options.UseInference) packed |= 1 << 3;
            if (options.IncludePort) packed |= 1 << 4;
            if (options.IncludeLastSeen) packed |= 1 << 5;
            packed |= (int)(options.RiskLevel ?? RiskLevel.Disabled) << 6;

            return new CacheKey(ipAddress, packed);
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
