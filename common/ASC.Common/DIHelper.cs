﻿using System;
using System.Collections.Generic;

using ASC.Common.Threading.Progress;
using ASC.Common.Threading.Workers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ASC.Common
{
    public class DIHelper
    {
        public List<string> Singleton { get; set; }
        public List<string> Scoped { get; set; }
        public List<string> Transient { get; set; }
        public List<string> Configured { get; set; }
        public IServiceCollection ServiceCollection { get; private set; }

        public DIHelper()
        {
            Singleton = new List<string>();
            Scoped = new List<string>();
            Transient = new List<string>();
            Configured = new List<string>();
        }

        public DIHelper(IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
        }

        public void Configure(IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
        }

        public bool TryAddScoped<TService>() where TService : class
        {
            var serviceName = $"{typeof(TService)}";
            if (!Scoped.Contains(serviceName))
            {
                Scoped.Add(serviceName);
                ServiceCollection.TryAddScoped<TService>();
                return true;
            }
            return false;
        }

        public bool TryAddScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            var serviceName = $"{typeof(TService)}{typeof(TImplementation)}";
            if (!Scoped.Contains(serviceName))
            {
                Scoped.Add(serviceName);
                ServiceCollection.TryAddScoped<TService, TImplementation>();
                return true;
            }

            return false;
        }

        public bool TryAddScoped<TService, TImplementation>(TService tservice, TImplementation tImplementation) where TService : Type where TImplementation : Type
        {
            var serviceName = $"{tservice}{tImplementation}";
            if (!Scoped.Contains(serviceName))
            {
                Scoped.Add(serviceName);
                ServiceCollection.TryAddScoped(tservice, tImplementation);
                return true;
            }

            return false;
        }


        public DIHelper TryAddSingleton<TService>() where TService : class
        {
            var serviceName = $"{typeof(TService)}";
            if (!Singleton.Contains(serviceName))
            {
                Singleton.Add(serviceName);
                ServiceCollection.TryAddSingleton<TService>();
            }

            return this;
        }

        public DIHelper TryAddSingleton<TService>(Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            var serviceName = $"{typeof(TService)}";
            if (!Singleton.Contains(serviceName))
            {
                Singleton.Add(serviceName);
                ServiceCollection.TryAddSingleton(implementationFactory);
            }

            return this;
        }

        public DIHelper TryAddSingleton<TService>(TService t) where TService : class
        {
            var serviceName = $"{typeof(TService)}";
            if (!Singleton.Contains(serviceName))
            {
                Singleton.Add(serviceName);
                ServiceCollection.TryAddSingleton(t);
            }

            return this;
        }

        public DIHelper TryAddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            var serviceName = $"{typeof(TService)}{typeof(TImplementation)}";
            if (!Singleton.Contains(serviceName))
            {
                Singleton.Add(serviceName);
                ServiceCollection.TryAddSingleton<TService, TImplementation>();
            }

            return this;
        }

        public DIHelper AddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            var serviceName = $"{typeof(TService)}{typeof(TImplementation)}";
            if (!Singleton.Contains(serviceName))
            {
                Singleton.Add(serviceName);
                ServiceCollection.AddSingleton<TService, TImplementation>();
            }

            return this;
        }

        public DIHelper TryAddSingleton<TService, TImplementation>(TService tservice, TImplementation tImplementation) where TService : Type where TImplementation : Type
        {
            var serviceName = $"{tservice}{tImplementation}";
            if (!Singleton.Contains(serviceName))
            {
                Singleton.Add(serviceName);
                ServiceCollection.TryAddSingleton(tservice, tImplementation);
            }
            return this;
        }


        public DIHelper TryAddTransient<TService>() where TService : class
        {
            var serviceName = $"{typeof(TService)}";
            if (!Transient.Contains(serviceName))
            {
                Transient.Add(serviceName);
                ServiceCollection.TryAddTransient<TService>();
            }

            return this;
        }

        public DIHelper Configure<TOptions>(Action<TOptions> configureOptions) where TOptions : class
        {
            var serviceName = $"{typeof(TOptions)}";
            if (!Configured.Contains(serviceName))
            {
                Configured.Add(serviceName);
                ServiceCollection.Configure(configureOptions);
            }

            return this;
        }

        private void AddToConfigured<TOptions>(string type, Action<TOptions> action) where TOptions : class
        {
            if (!Configured.Contains(type))
            {
                Configured.Add(type);
                ServiceCollection.Configure(action);
            }
        }

        public DIHelper AddWorkerQueue<T1>(int workerCount, int waitInterval, bool stopAfterFinsih, int errorCount)
        {
            void action(WorkerQueue<T1> a)
            {
                a.workerCount = workerCount;
                a.waitInterval = waitInterval;
                a.stopAfterFinsih = stopAfterFinsih;
                a.errorCount = errorCount;
            }
            AddToConfigured($"{typeof(WorkerQueue<T1>)}", (Action<WorkerQueue<T1>>)action);
            return this;
        }
        public DIHelper AddProgressQueue<T1>(int workerCount, int waitInterval, bool removeAfterCompleted, bool stopAfterFinsih, int errorCount) where T1 : class, IProgressItem
        {
            void action(ProgressQueue<T1> a)
            {
                a.workerCount = workerCount;
                a.waitInterval = waitInterval;
                a.stopAfterFinsih = stopAfterFinsih;
                a.errorCount = errorCount;
                a.removeAfterCompleted = removeAfterCompleted;
            }
            AddToConfigured($"{typeof(ProgressQueue<T1>)}", (Action<ProgressQueue<T1>>)action);
            return this;
        }
        public DIHelper Configure<TOptions>(string name, Action<TOptions> configureOptions) where TOptions : class
        {
            var serviceName = $"{typeof(TOptions)}{name}";
            if (!Configured.Contains(serviceName))
            {
                Configured.Add(serviceName);
                ServiceCollection.Configure(name, configureOptions);
            }

            return this;
        }
    }
}
