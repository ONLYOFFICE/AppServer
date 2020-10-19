﻿using System;
using System.Collections.Generic;

using ASC.Common;
using ASC.Common.Logging;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ASC.Core.Common.EF
{
    public class BaseDbContextManager<T> : OptionsManager<T>, IDisposable where T : class, IDisposable, IAsyncDisposable, new()
    {
        private Dictionary<string, T> Pairs { get; set; }
        private List<T> AsyncList { get; set; }
        private IOptionsFactory<T> Factory { get; }
        private IConfiguration Configuration { get; }

        public BaseDbContextManager(IOptionsFactory<T> factory, IConfiguration configuration) : base(factory)
        {
            Pairs = new Dictionary<string, T>();
            AsyncList = new List<T>();
            Factory = factory;
            Configuration = configuration;
        }

        public override T Get(string name)
        {
            if (!Pairs.ContainsKey(name))
            {
                var t = base.Get(name);
                Pairs.Add(name, t);

                if (t is BaseDbContext dbContext)
                {
                    if (Configuration["migration:enabled"] == "true")
                    {
                        dbContext.Migrate();
                    }
                }
            }

            return Pairs[name];
        }

        public T GetNew(string name = "default")
        {
            var result = Factory.Create(name);

            AsyncList.Add(result);

            return result;
        }

        public void Dispose()
        {
            foreach (var v in Pairs)
            {
                v.Value.Dispose();
            }

            foreach (var v in AsyncList)
            {
                v.Dispose();
            }
        }
    }

    public class DbContextManager<T> : BaseDbContextManager<T> where T : BaseDbContext, new()
    {
        public DbContextManager(IOptionsFactory<T> factory, IConfiguration configuration) : base(factory, configuration)
        {
        }
    }

    public class MultiRegionalDbContextManager<T> : BaseDbContextManager<MultiRegionalDbContext<T>> where T : BaseDbContext, new()
    {
        public MultiRegionalDbContextManager(IOptionsFactory<MultiRegionalDbContext<T>> factory, IConfiguration configuration) : base(factory, configuration)
        {
        }
    }

    public static class DbContextManagerExtension
    {
        public static DIHelper AddDbContextManagerService<T>(this DIHelper services) where T : BaseDbContext, new()
        {
            if (services.TryAddScoped<DbContextManager<T>>())
            {
                services.TryAddScoped<MultiRegionalDbContextManager<T>>();
                services.TryAddScoped<IConfigureOptions<T>, ConfigureDbContext>();
                services.TryAddScoped<IConfigureOptions<MultiRegionalDbContext<T>>, ConfigureMultiRegionalDbContext<T>>();
                return services.AddLoggerService();
            }
            return services;
        }
    }
}
