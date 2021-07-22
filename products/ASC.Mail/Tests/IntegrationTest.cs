using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
//using Microsoft.Extensions.DependencyInjection.Extensions;
//using ASC.Core.Common.EF.Context;
//using Microsoft.Extensions.DependencyInjection;
//using System.Data.Linq;

namespace ASC.Mail.Tests
{
    public class IntegrationTest
    {
        protected readonly HttpClient TestClient;

        public IntegrationTest()
        {
            var appFactory = new WebApplicationFactory<Startup>();
            //TODO: Replace real db to Fake inMemmory db
                //.WithWebHostBuilder(builder => {
                //    builder.ConfigureServices(services =>
                //    {
                //        services.RemoveAll(typeof(DataContext));
                //        services.AddDbContext<DbContext>(options =>
                //        {
                //            options.UseInMemmoryDatabase();
                //        });
                //    });
                //});

            TestClient = appFactory.CreateClient();

            TestClient.BaseAddress = new Uri("http://localhost:8092/api/2.0/mail/");

            TestClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        protected async Task AuthenticateAsync() {
            TestClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", await GetJwtAsync());
        }

        private async Task<string> GetJwtAsync() {

            //TODO: Change response token
           var token = await Task.FromResult("4PgTLAGww7BnJ9JrqwZ/NNShlILIqL11TsIG/7m9HmJRQzXrbyo03PhS0r/WxsUh2Bf7r/XefPif3rNpQN/" +
                "AL3CUsMhTt04fs7DLhPeLxpU0fVOUhVImdeiayHJ5s0GjQTfEAFEwDEA3QBtHIvczoQ==");

            return token;
        }
    }
}
