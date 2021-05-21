using System.Text.Json.Serialization;

using ASC.Api.Core.Auth;
using ASC.Api.Core.Core;
using ASC.Api.Core.Middleware;
using ASC.Common;
using ASC.Common.Caching;
using ASC.Common.DependencyInjection;
using ASC.Common.Logging;

using Autofac;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
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
        public virtual string[] LogParams { get; }
        public virtual JsonConverter[] Converters { get; }
        public virtual bool AddControllers { get; } = true;
        public virtual bool AddControllersAsServices { get; } = false;
        public virtual bool ConfirmAddScheme { get; } = false;
        public virtual bool AddAndUseSession { get; } = false;
        protected DIHelper DIHelper { get; }

        public BaseStartup(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            Configuration = configuration;
            HostEnvironment = hostEnvironment;
            DIHelper = new DIHelper();
        }

        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddMemoryCache();

            if(AddAndUseSession)
                services.AddSession();

            DIHelper.Configure(services);

            if (AddControllersAsServices)
            {
                services.AddControllers().AddControllersAsServices()
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
            }
            else if(AddControllers)
            {
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
            }

            DIHelper.TryAdd<DisposeMiddleware>();
            DIHelper.TryAdd<CultureMiddleware>();
            DIHelper.TryAdd<IpSecurityFilter>();
            DIHelper.TryAdd<PaymentFilter>();
            DIHelper.TryAdd<ProductSecurityFilter>();
            DIHelper.TryAdd<TenantStatusFilter>();
            DIHelper.TryAdd<ConfirmAuthHandler>();

            DIHelper.TryAdd(typeof(ICacheNotify<>), typeof(KafkaCache<>));

            DIHelper.RegisterProducts(Configuration, HostEnvironment.ContentRootPath);

            var builder = services.AddMvcCore(config =>
            {
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

            if (LogParams != null)
            {
                LogNLogExtension.ConfigureLog(DIHelper, LogParams);
            }
        }

        public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseRouting();

            if (AddAndUseSession)
                app.UseSession();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseCultureMiddleware();

            app.UseDisposeMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapCustom();
            });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.Register(Configuration);
        }
    }
}