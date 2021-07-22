using System.Reflection;
using System.Text.Json.Serialization;

using ASC.Api.Core.Auth;
using ASC.Api.Core.Convention;
using ASC.Api.Core.Core;
using ASC.Api.Core.Middleware;
using ASC.Common;
using ASC.Common.Caching;
using ASC.Common.DependencyInjection;
using ASC.Common.Mapping;

using Autofac;

using HealthChecks.UI.Client;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ASC.Api.Core
{
    public abstract class BaseStartup
    {
        public IConfiguration Configuration { get; }
        public IHostEnvironment HostEnvironment { get; }
        public virtual JsonConverter[] Converters { get; }
        public virtual bool ConfirmAddScheme { get; } = false;
        protected DIHelper DIHelper { get; }
        protected bool LoadProducts { get; } = true;
        protected bool LoadConsumers { get; } = true;

        public BaseStartup(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            Configuration = configuration;
            HostEnvironment = hostEnvironment;
            DIHelper = new DIHelper();
        }

        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddCustomHealthCheck(Configuration);
            services.AddHttpContextAccessor();
            services.AddMemoryCache();

            DIHelper.Configure(services);

            services.AddControllers()
                .AddXmlSerializerFormatters()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.WriteIndented = false;
                    options.JsonSerializerOptions.IgnoreNullValues = true;
                    options.JsonSerializerOptions.Converters.Add(new ApiDateTimeConverter());

                    if (Converters != null)
                    {
                        foreach (var c in Converters)
                        {
                            options.JsonSerializerOptions.Converters.Add(c);
                        }
                    }
                });

            DIHelper.TryAdd<DisposeMiddleware>();
            DIHelper.TryAdd<CultureMiddleware>();
            DIHelper.TryAdd<IpSecurityFilter>();
            DIHelper.TryAdd<PaymentFilter>();
            DIHelper.TryAdd<ProductSecurityFilter>();
            DIHelper.TryAdd<TenantStatusFilter>();
            DIHelper.TryAdd<ConfirmAuthHandler>();

            DIHelper.TryAdd(typeof(ICacheNotify<>), typeof(KafkaCache<>));

            if (LoadProducts)
            {
                DIHelper.RegisterProducts(Configuration, HostEnvironment.ContentRootPath);
            }

            var builder = services.AddMvcCore(config =>
            {
                config.Conventions.Add(new ControllerNameAttributeConvention());

                var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

                config.Filters.Add(new AuthorizeFilter(policy));
                config.Filters.Add(new TypeFilterAttribute(typeof(TenantStatusFilter)));
                config.Filters.Add(new TypeFilterAttribute(typeof(PaymentFilter)));
                config.Filters.Add(new TypeFilterAttribute(typeof(IpSecurityFilter)));
                config.Filters.Add(new TypeFilterAttribute(typeof(ProductSecurityFilter)));
                config.Filters.Add(new CustomResponseFilterAttribute());
                config.Filters.Add(new CustomExceptionFilterAttribute());
                config.Filters.Add(new TypeFilterAttribute(typeof(FormatFilter)));

                config.OutputFormatters.RemoveType<XmlSerializerOutputFormatter>();
                config.OutputFormatters.Add(new XmlOutputFormatter());
            });


            var authBuilder = services.AddAuthentication("cookie")
                .AddScheme<AuthenticationSchemeOptions, CookieAuthHandler>("cookie", a => { });

            if (ConfirmAddScheme)
            {
                authBuilder.AddScheme<AuthenticationSchemeOptions, ConfirmAuthHandler>("confirm", a => { });
            }

            services.AddAutoMapper(Assembly.GetAssembly(typeof(MappingProfile)));
        }

        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseCultureMiddleware();

            app.UseDisposeMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapCustom();

                endpoints.MapHealthChecks("/health", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
                {
                    Predicate = r => r.Name.Contains("self")
                });
            });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.Register(Configuration, LoadProducts, LoadConsumers);
        }
    }
}