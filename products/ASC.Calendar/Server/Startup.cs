using System.Text;

using ASC.Api.Core;
using ASC.Calendar.Controllers;
using ASC.Common;
using ASC.Web.Studio.Core.Notify;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ASC.Calendar
{
    public class Startup : BaseStartup
    {
        public override string[] LogParams { get => new string[] { "ASC.Calendar" }; }
        public override bool ConfirmAddScheme { get => true; }
        public override bool AddControllersAsServices { get => true; }
        public override bool AddAndUseSession { get => true; }



        public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment)
            : base(configuration, hostEnvironment)
        {

        }

        public override void ConfigureServices(IServiceCollection services)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            services.AddDistributedMemoryCache();

            base.ConfigureServices(services);

            DIHelper.TryAdd<CalendarController>();

            NotifyConfigurationExtension.Register(DIHelper);
        }

        public override  void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors(builder =>
                 builder
                     .AllowAnyOrigin()
                     .AllowAnyHeader()
                     .AllowAnyMethod());

            base.Configure(app, env);
        }
    }
}
