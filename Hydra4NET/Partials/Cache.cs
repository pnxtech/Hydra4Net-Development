using Hydra4NET.Helpers;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace Hydra4NET
{
    //TODO : implement IDistributedCache in hosting extensions?
    public partial class Hydra
    {
        //TODO: support sliding expiration
        //TODO: clean up cache if no more instances are active? or make expiry mandatory?
        //TODO: Add shared cache for all hydra services.  Possibly make this caching component its own class (expose via Hydra.Cache?)
        private string GetKey(string key) => $"{_redis_pre_key}:{ServiceName}:cache:{key}";

        private Task<bool> SetCacheItem(string key, RedisValue value, TimeSpan? expiry) => _redis?.GetDatabase()?.StringSetAsync(GetKey(key), value, expiry) ?? Task.FromResult(false);


        private async Task<T> GetCacheItem<T>(string key, Func<RedisValue, T> castAction) //necesary due to how redis casts things
        {
            if (_redis != null)
            {
                RedisValue val = await _redis.GetDatabase().StringGetAsync(GetKey(key));
                if (!val.IsNull)
                {
                    return castAction(val);
                }
            }
#pragma warning disable CS8603 // Possible null reference return.
            return default(T);
#pragma warning restore CS8603 // Possible null reference return.
        }

        //TODO: Add more supported types.  can cache a number of types natively.  consider numeric types
        public Task<bool> SetCacheString(string key, string value, TimeSpan? expiry = null) => SetCacheItem(key, value, expiry);

        public Task<string?> GetCacheString(string key) => GetCacheItem(key, (v) => (string?)v);

        public Task<bool> SetCacheBytes(string key, byte[] value, TimeSpan? expiry = null) => SetCacheItem(key, value, expiry);

        public Task<byte[]?> GetCacheBytes(string key) => GetCacheItem(key, (v) => (byte[]?)v);

        public Task<bool> SetCacheBool(string key, bool value, TimeSpan? expiry = null) => SetCacheItem(key, value, expiry);

        public Task<bool?> GetCacheBool(string key) => GetCacheItem(key, (v) => (bool?)v);

        public Task<bool> RemoveCacheItem(string key) => _redis?.GetDatabase()?.KeyDeleteAsync(GetKey(key)) ?? Task.FromResult(false);

        public Task<bool> SetCacheJson<T>(string key, T value, TimeSpan? expiry = null) where T : class
        {
            //TODO: compress it??
            var json = StandardSerializer.SerializeForCache(value);
            return SetCacheString(key, json, expiry);
        }

        public async Task<T?> GetCacheJson<T>(string key) where T : class
        {
            var json = await GetCacheString(key);
            if (json != null)
                return StandardSerializer.Deserialize<T>(json);
            return null;
        }
    }
}
