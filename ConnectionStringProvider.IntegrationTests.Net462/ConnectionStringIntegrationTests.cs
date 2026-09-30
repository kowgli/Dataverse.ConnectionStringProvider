using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConnectionStringProvider.IntegrationTests.Net462
{
    [TestClass]
    public class ConnectionStringIntegrationTests
    {
        private const string VaultUrlVariable = "CONNECTION_STRING_PROVIDER_TEST_VAULT_URL";
        private const string SecretName = "TEST-SECRET";

        // Configure the vault URL as a persistent environment variable for the current Windows user:
        // [Environment]::SetEnvironmentVariable(
        //     "CONNECTION_STRING_PROVIDER_TEST_VAULT_URL",
        //     "https://your-test-vault.vault.azure.net/",
        //     [EnvironmentVariableTarget]::User)
        // Restart Visual Studio or the terminal afterward so the new value is loaded.

        [TestMethod]
        [TestCategory("Integration")]
        public void Get_RetrievesExpectedSecretFromConfiguredVault()
        {
            string vaultUrl = Environment.GetEnvironmentVariable(VaultUrlVariable);

            Assert.IsFalse(
                string.IsNullOrWhiteSpace(vaultUrl),
                "Set the {0} user environment variable to the test vault URL. See the PowerShell instructions above, then restart the test runner.",
                VaultUrlVariable);

            string keyVaultConfiguration = string.Format(
                "VaultUrl={0}; Key={1}",
                vaultUrl.TrimEnd('/'),
                SecretName);

            string value = global::ConnectionStringProvider.ConnectionString.Get(keyVaultConfiguration, silent: true);

            Assert.AreEqual("12345", value);
        }

        [TestMethod]
        public void Get_ReturnsExplicitConnectionStringUnchanged()
        {
            const string explicitConnectionString =
                "Url=https://example.crm.dynamics.com; AuthType=ClientSecret; ClientId=00000000-0000-0000-0000-000000000000; ClientSecret=example-secret";

            string value = global::ConnectionStringProvider.ConnectionString.Get(explicitConnectionString, silent: true);

            Assert.AreEqual(explicitConnectionString, value);
        }
    }
}
