using System.Threading.Tasks;
using Xunit;

namespace ASC.Mail.Tests.ControllerTests
{
    public class FolderControllerTests : IntegrationTest
    {
        [Fact]
        public async Task GetFolders_withResponse()
        {
            // Arrange
            await AuthenticateAsync();

            // Act
            var resporse = await TestClient.GetAsync("folders.json");

            // Asset
            resporse.EnsureSuccessStatusCode();
        }
    }
}
