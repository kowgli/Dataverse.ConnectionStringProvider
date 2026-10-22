using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.IO;
using System.Linq;

namespace ConnectionStringProvider
{
    /// <summary>
    /// Resolves a connection string from a direct value, a plain-text file, or Azure Key Vault.
    /// </summary>
    public static class ConnectionString
    {
        /// <summary>
        /// Resolves a connection string from a direct value, a plain-text file, or Azure Key Vault.
        /// </summary>
        /// <param name="connectionString">
        /// A value starting with File= followed by a non-empty file path, or VaultUrl= followed by
        /// Azure Key Vault configuration; prefix detection is case-insensitive. Other values are returned unchanged.
        /// </param>
        /// <param name="silent">When true, suppresses informational console messages. File resolution is always silent.</param>
        /// <remarks>
        /// Prefixes must appear at the beginning of the value; leading whitespace is not removed.
        /// For File=, the entire remainder is used as the path without trimming, unquoting, or environment-variable expansion.
        /// Relative paths use the current working directory. Each call reads the whole file and trims leading and trailing
        /// whitespace from its contents. Empty or whitespace-only files return an empty string. Contents are not validated
        /// or resolved again, and file-read exceptions propagate without falling back to another source.
        /// File and direct-value resolution do not require Azure authentication.
        /// Use VaultUrl=&lt;vault-url&gt;;Key=&lt;secret-name&gt; for Azure Key Vault configuration.
        /// </remarks>
        /// <example>
        /// "File=connection-string.txt" returns the trimmed contents of the file.
        /// "VaultUrl=https://my-vault.vault.azure.net/;Key=MyConnectionStringSecret" retrieves the secret from Azure Key Vault.
        /// "Url=https://org.crm.dynamics.com;AuthType=ClientSecret;ClientId=example;ClientSecret=example" is returned unchanged.
        /// </example>
        /// <returns>The unchanged direct value, trimmed file contents, or Azure Key Vault secret value.</returns>
        public static string Get(string connectionString, bool silent = false)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (IsDirectConnectionString(connectionString))
            {
                if (!silent) { Console.WriteLine("Detected Dataverse connection string. Using explicitly."); }
                return connectionString;
            }

            if (IsFileConnectionString(connectionString))
            {
                return File.ReadAllText(connectionString.Substring(5)).Trim();
            }

            try
            {
                if (!silent) { Console.WriteLine("Detected Azure Key Vault connection string. Trying to retrieve..."); }

                (string vaultUrl, string secret) = ParseKeyVaultConfig(connectionString);

                var tokenCallback = new AzureServiceTokenProvider("RunAs=Developer; DeveloperTool=AzureCli").KeyVaultTokenCallback;
                var authCallback = new KeyVaultClient.AuthenticationCallback(tokenCallback);
                var client = new KeyVaultClient(authCallback);

                var retrievedSecret = client.GetSecretAsync(vaultUrl, secret).Result;

                if (!silent) { Console.WriteLine("Connection string successfully retrieved."); }

                return retrievedSecret?.Value;
            }
            catch (AzureServiceTokenProviderException ex)
            {
                throw new Exception("Unable to log into Azure KeyVault.\r\n" +
                                    "Run \"az login\" to obtain a token.\r\n" +
                                    "Requires Azure CLI - https://aka.ms/installazurecliwindows\r\n" +
                                    $"Details: {ex.Message}"
                );
            }
        }

        private static bool IsDirectConnectionString(string connectionString)
        {
            return !connectionString.ToUpperInvariant().StartsWith("VAULTURL=") && !connectionString.ToUpperInvariant().StartsWith("FILE=");
        }

        private static bool IsFileConnectionString(string connectionString)
        {
            return connectionString.ToUpperInvariant().StartsWith("FILE=") && connectionString.Length > 5;
        }

        private static (string vaultUrl, string secret) ParseKeyVaultConfig(string connectionString)
        {
            string[] parts = connectionString.Split(';').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

            string vaultUrl = parts.Where(x => x.StartsWith("VaultUrl")).FirstOrDefault()?.Replace("VaultUrl", "")?.Replace("=", "")?.Trim();
            string secret = parts.Where(x => x.StartsWith("Key")).FirstOrDefault()?.Replace("Key", "")?.Replace("=", "")?.Trim();

            return (vaultUrl, secret);
        }
    }
}
