using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trinity
{
    public  class CachingService
    {

        public static CachingService CacheService { get; set; }


        public Dictionary<string, CacheObject> CacheDictionary { get; set; }

        public CachingService()
        {
            CacheDictionary = new Dictionary<string, CacheObject>();
        }

        public int ExpiresInSec { get; set; }

        public T TryGetCache<T>(string id)
        {
            CacheObject cache = null;

            CacheDictionary.TryGetValue(id, out cache);

            if (cache == null)
                return default(T);

            if (cache.Updated.AddSeconds(CachingService.CacheService.ExpiresInSec) > DateTime.Now)
                return default(T);

            return (T)cache.Data;


        }
    }


    public class CacheObject
    {
        public DateTime Updated { get; set; }

        public object Data { get; set; }

    }
}
