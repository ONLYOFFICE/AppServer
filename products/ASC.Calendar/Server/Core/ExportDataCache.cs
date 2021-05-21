using ASC.Api.Core;
using ASC.Common;
using ASC.Common.Caching;
using ASC.Core;
using ASC.Web.Studio.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASC.Calendar.Core
{
    [Scope]
    public class ExportDataCache
    {
        private readonly ICache cache;

        private TenantManager TenantManager { get; }
        public ExportDataCache(
            TenantManager tenantManager, ICache cache)
        {
            this.cache = cache;
            TenantManager = tenantManager;
        }
        public String GetCacheKey(string calendarId)
        {
            return String.Format("{0}_ExportCalendar_{1}", TenantManager.GetCurrentTenant().TenantId, calendarId);
        }

        public string Get(string calendarId)
        {
            return cache.Get<string>(GetCacheKey(calendarId));
        }

        public void Insert(string calendarId, string data)
        {
            if (string.IsNullOrEmpty(data))
                Reset(calendarId);
            else
                cache.Insert(GetCacheKey(calendarId), data, TimeSpan.FromMinutes(5));
        }

        public void Reset(string calendarId)
        {
            cache.Remove(GetCacheKey(calendarId));
        }
    }
}
