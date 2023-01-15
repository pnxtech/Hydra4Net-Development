using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hydra4NET
{
    //TODO : implement IDistributedCache in hosting extensions
    public partial class Hydra
    {
        string GetKey(string key) => $"hydra:cache:{ServiceName}:{key}";
        private Task<bool> SetCacheItem(string key, RedisValue value, TimeSpan? expiry) => _redis?.GetDatabase()?.StringSetAsync(GetKey(key), value, expiry) ?? Task.FromResult(false);
        private async Task<T> GetCacheItem<T>(string key, CancellationToken token = default) where T : class?
        {
            #pragma warning disable CS8603 // Possible null reference return.
            if (_redis != null)
            {
                RedisValue val = await _redis.GetDatabase().StringGetAsync(GetKey(key));
                if (val != RedisValue.Null)
                    return val as T;
            }
            return null;
            #pragma warning restore CS8603 // Possible null reference return.
        }

        //TODO: Add more supported types

        public Task<bool> SetCacheString(string key, string value, TimeSpan? expiry = null, CancellationToken token = default) => SetCacheItem(key, value, expiry);    

        public  Task<string?> GetCacheString(string key, CancellationToken token = default) => GetCacheItem<string?>(key); 
       
        public Task<bool> SetCacheBytes(string key, byte[] value, TimeSpan? expiry = null, CancellationToken token = default) => SetCacheItem(key, value, expiry);     

        public Task<byte[]?> GetCacheBytes(string key,  CancellationToken token = default) => GetCacheItem<byte[]?>(key); 

        public Task<bool> RemoveCacheItem(string key, CancellationToken token = default) => _redis?.GetDatabase()?.KeyDeleteAsync(GetKey(key)) ?? Task.FromResult(false);


        //TODO: Add shared cache for all hydra services.  possibly make this caching component its own class (expose via Hydra.Cache?)
    }
}
