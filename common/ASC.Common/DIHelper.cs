using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using ASC.Common.DependencyInjection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ASC.Common
{
    public enum DIAttributeEnum
    {
        Singletone,
        Scope,
        Transient
    }

    public class TransientAttribute : DIAttribute
    {
        public override DIAttributeEnum DIAttributeEnum { get => DIAttributeEnum.Transient; }

        public TransientAttribute() { }

        public TransientAttribute(Type service) : base(service) { }

        public TransientAttribute(Type service, Type implementation) : base(service, implementation) { }

        public override void TryAdd(IServiceCollection services, Type service, Type implementation = null)
        {
            if (implementation != null)
            {
                services.AddTransient(service, implementation);
            }
            else
            {
                services.AddTransient(service);
            }
        }
    }

    public class ScopeAttribute : DIAttribute
    {
        public override DIAttributeEnum DIAttributeEnum { get => DIAttributeEnum.Scope; }

        public ScopeAttribute() { }

        public ScopeAttribute(Type service) : base(service) { }

        public ScopeAttribute(Type service, Type implementation) : base(service, implementation) { }

        public override void TryAdd(IServiceCollection services, Type service, Type implementation = null)
        {
            if (implementation != null)
            {
                services.AddScoped(service, implementation);
            }
            else
            {
                services.AddScoped(service);
            }
        }
    }

    public class SingletoneAttribute : DIAttribute
    {
        public override DIAttributeEnum DIAttributeEnum { get => DIAttributeEnum.Singletone; }

        public SingletoneAttribute() { }

        public SingletoneAttribute(Type service) : base(service) { }

        public SingletoneAttribute(Type service, Type implementation) : base(service, implementation) { }

        public override void TryAdd(IServiceCollection services, Type service, Type implementation = null)
        {
            if (implementation != null)
            {
                services.AddSingleton(service, implementation);
            }
            else
            {
                services.AddSingleton(service);
            }
        }
    }

    public abstract class DIAttribute : Attribute
    {
        public abstract DIAttributeEnum DIAttributeEnum { get; }
        public Type Implementation { get; }
        public Type Service { get; }
        public Type Additional { get; set; }

        public DIAttribute() { }

        public DIAttribute(Type service)
        {
            Service = service;
        }

        public DIAttribute(Type service, Type implementation)
        {
            Implementation = implementation;
            Service = service;
        }

        public abstract void TryAdd(IServiceCollection services, Type service, Type implementation = null);
    }

    public class DIHelper
    {
        public Dictionary<DIAttributeEnum, List<string>> Services { get; set; }
        public List<string> Added { get; set; }
        public List<string> Configured { get; set; }
        public IServiceCollection ServiceCollection { get; private set; }

        public DIHelper()
        {
            Services = new Dictionary<DIAttributeEnum, List<string>>()
            {
                { DIAttributeEnum.Singletone, new List<string>() },
                { DIAttributeEnum.Scope, new List<string>() },
                { DIAttributeEnum.Transient, new List<string>() }
            };
            Added = new List<string>();
            Configured = new List<string>();
        }

        public DIHelper(IServiceCollection serviceCollection) : this()
        {
            ServiceCollection = serviceCollection;
        }

        public void Configure(IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;
        }

        public void RegisterProducts(IConfiguration configuration, string path)
        {
            var types = AutofacExtension.FindAndLoad(configuration, path);

            foreach (var t in types)
            {
                TryAdd(Type.GetType(t));
            }
        }

        public bool TryAdd<TService>() where TService : class
        {
            return TryAdd(typeof(TService));
        }

        public bool TryAdd<TService, TImplementation>() where TService : class
        {
            return TryAdd(typeof(TService), typeof(TImplementation));
        }

        public bool TryAdd(Type service, Type implementation = null)
        {
            if (service.IsInterface && service.IsGenericType && implementation == null &&
                (service.GetGenericTypeDefinition() == typeof(IOptionsSnapshot<>) ||
                service.GetGenericTypeDefinition() == typeof(IOptions<>) ||
                service.GetGenericTypeDefinition() == typeof(IOptionsMonitor<>)
                ))
            {
                service = service.GetGenericArguments().FirstOrDefault();
                if (service == null)
                {
                    return false;
                }
            }

            var serviceName = $"{service}{implementation}";
            if (Added.Contains(serviceName)) return false;
            Added.Add(serviceName);

            var di = service.IsGenericType && (
                service.GetGenericTypeDefinition() == typeof(IConfigureOptions<>) ||
                service.GetGenericTypeDefinition() == typeof(IPostConfigureOptions<>) ||
                service.GetGenericTypeDefinition() == typeof(IOptionsMonitor<>)
                ) && implementation != null ? implementation.GetCustomAttribute<DIAttribute>() : service.GetCustomAttribute<DIAttribute>();
            var isnew = false;

            if (di != null)
            {
                if (di.Additional != null)
                {
                    var m = di.Additional.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
                    m.Invoke(null, new[] { this });
                }

                if (!service.IsInterface || implementation != null)
                {
                    isnew = implementation != null ? Register(service, implementation) : Register(service);
                    if (!isnew) return false;
                }

                if (service.IsInterface && implementation == null || !service.IsInterface)
                {
                    if (di.Service != null)
                    {
                        var a = di.Service.GetInterfaces().FirstOrDefault(x => x.IsGenericType && (
                        x.GetGenericTypeDefinition() == typeof(IConfigureOptions<>) ||
                        x.GetGenericTypeDefinition() == typeof(IPostConfigureOptions<>) ||
                        x.GetGenericTypeDefinition() == typeof(IOptionsMonitor<>)
                        ));
                        if (a != null)
                        {
                            if (!a.ContainsGenericParameters)
                            {
                                var b = a.GetGenericArguments();

                                foreach (var g in b)
                                {
                                    if (g != service)
                                    {
                                        TryAdd(g);
                                        if (service.IsInterface && di.Implementation == null)
                                        {
                                            TryAdd(service, g);
                                        }
                                    }
                                }

                                TryAdd(a, di.Service);
                            }
                            else
                            {
                                Type c = null;
                                var a1 = a.GetGenericTypeDefinition();
                                var b = a.GetGenericArguments().FirstOrDefault();

                                if (b != null && b.IsGenericType)
                                {
                                    var b1 = b.GetGenericTypeDefinition().MakeGenericType(service.GetGenericArguments());

                                    TryAdd(b1);
                                    c = a1.MakeGenericType(b1);
                                }
                                else
                                {
                                    c = a1.MakeGenericType(service.GetGenericArguments());
                                }

                                TryAdd(c, di.Service.MakeGenericType(service.GetGenericArguments()));
                                //a, di.Service
                            }
                        }
                        else
                        {
                            if (di.Implementation == null)
                            {
                                isnew = Register(service, di.Service);
                                TryAdd(di.Service);
                            }
                            else
                            {
                                Register(di.Service);
                            }

                        }
                    }

                    if (di.Implementation != null)
                    {
                        var a = di.Implementation.GetInterfaces().FirstOrDefault(x => x.IsGenericType &&
                        (x.GetGenericTypeDefinition() == typeof(IConfigureOptions<>) ||
                        x.GetGenericTypeDefinition() == typeof(IPostConfigureOptions<>) ||
                        x.GetGenericTypeDefinition() == typeof(IOptionsMonitor<>))
                        );
                        if (a != null)
                        {
                            if (!a.ContainsGenericParameters)
                            {
                                var b = a.GetGenericArguments();

                                foreach (var g in b)
                                {
                                    if (g != service)
                                    {
                                        //TryAdd(g);
                                        if (service.IsInterface && implementation == null)
                                        {
                                            TryAdd(service, g);
                                        }
                                    }
                                }

                                TryAdd(a, di.Implementation);
                            }
                            else
                            {
                                Type c = null;
                                var a1 = a.GetGenericTypeDefinition();
                                var b = a.GetGenericArguments().FirstOrDefault();

                                if (b != null && b.IsGenericType)
                                {
                                    var b1 = b.GetGenericTypeDefinition().MakeGenericType(service.GetGenericArguments());

                                    TryAdd(b1);
                                    c = a1.MakeGenericType(b1);
                                }
                                else
                                {
                                    c = a1.MakeGenericType(service.GetGenericArguments());
                                }

                                TryAdd(c, di.Implementation.MakeGenericType(service.GetGenericArguments()));
                                //a, di.Service
                            }
                        }
                        else
                        {
                            isnew = TryAdd(service, di.Implementation);
                        }
                    }
                }
            }

            if (isnew)
            {
                ConstructorInfo[] props = null;

                if (!service.IsInterface)
                {
                    props = service.GetConstructors();
                }
                else if (implementation != null)
                {
                    props = implementation.GetConstructors();
                }
                else if (di.Service != null)
                {
                    props = di.Service.GetConstructors();
                }

                if (props != null)
                {
                    var par = props.SelectMany(r => r.GetParameters()).Distinct();
                    foreach (var p1 in par)
                    {
                        TryAdd(p1.ParameterType);
                    }
                }
            }

            return isnew;
        }

        private bool Register(Type service, Type implementation = null)
        {
            if (service.IsSubclassOf(typeof(ControllerBase)) || service.GetInterfaces().Contains(typeof(IResourceFilter)) || service.GetInterfaces().Contains(typeof(IDictionary<string, string>))) return true;
            var c = service.IsGenericType && (
                service.GetGenericTypeDefinition() == typeof(IConfigureOptions<>) ||
                service.GetGenericTypeDefinition() == typeof(IPostConfigureOptions<>) ||
                service.GetGenericTypeDefinition() == typeof(IOptionsMonitor<>)
                ) && implementation != null ? implementation.GetCustomAttribute<DIAttribute>() : service.GetCustomAttribute<DIAttribute>();
            var serviceName = $"{service}{implementation}";

            if (!Services[c.DIAttributeEnum].Contains(serviceName))
            {
                c.TryAdd(ServiceCollection, service, implementation);
                Services[c.DIAttributeEnum].Add(serviceName);
                return true;
            }

            return false;
        }

        public DIHelper TryAddSingleton<TService>(Func<IServiceProvider, TService> implementationFactory) where TService : class
        {
            var serviceName = $"{typeof(TService)}";
            if (!Services[DIAttributeEnum.Singletone].Contains(serviceName))
            {
                Services[DIAttributeEnum.Singletone].Add(serviceName);
                ServiceCollection.TryAddSingleton(implementationFactory);
            }

            return this;
        }

        public DIHelper TryAddSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            var serviceName = $"{typeof(TService)}{typeof(TImplementation)}";
            if (!Services[DIAttributeEnum.Singletone].Contains(serviceName))
            {
                Services[DIAttributeEnum.Singletone].Add(serviceName);
                ServiceCollection.TryAddSingleton<TService, TImplementation>();
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
