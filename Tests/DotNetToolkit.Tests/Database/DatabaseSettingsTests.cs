using DotNetToolkit.Database.Configuration;
using Xunit;

namespace DotNetToolkit.Tests.Database
{
    public class DatabaseSettingsTests
    {
        [Fact]
        public void DatabaseSettings_Defaults_AreExpected()
        {
            var settings = new DatabaseSettings();

            Assert.Equal(string.Empty, settings.ConnectionString);
            Assert.Equal(string.Empty, settings.ProviderName);
            Assert.Equal(30, settings.CommandTimeoutSeconds);
        }
    }
}
