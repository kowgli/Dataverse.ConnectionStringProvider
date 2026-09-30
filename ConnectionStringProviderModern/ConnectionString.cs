using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace ConnectionStringProvider;

/// <summary>
/// Gets a connection string, optionally retrieving it from Azure Key Vault.
/// </summary>
public static class ConnectionString
{
    /// <summary>
    /// Gets connection string with optional query to Azure Key Vault.
    /// </summary>
    /// <param name="connectionString">AKV secret if parameter contains VaultUrl (case insensitive), otherwise the connection string value.</param>
    /// <param name="silent">When true, suppresses informational console messages.</param>
    /// <example>
    /// "VaultUrl=https://my_vault.vault.azure.net/; Key=MyConnectionStringSecret" is retrieved from AKV.
    /// "Url=https://org.crm.dynamics.com; AuthType=ClientSecret; ClientId=xxxxxx-yyyyy-zzzzzz; ClientSecret=XYZ123" is returned as is.
    /// </example>
    /// <returns>Actual connection string.</returns>
    public static string Get(string connectionString, bool silent = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        if (!UseAzureKeyVault(connectionString))
        {
            if (!silent)
            {
                Console.WriteLine("Detected Dataverse connection string. Using explicitly.");
            }

            return connectionString;
        }

        try
        {
            if (!silent)
            {
                Console.WriteLine("Detected Azure Key Vault connection string. Trying to retrieve...");
            }

            (Uri vaultUri, string secretName) = ParseKeyVaultConfig(connectionString);

            // AzureCliCredential deliberately uses only the identity cached by `az login`,
            // matching the legacy RunAs=Developer;DeveloperTool=AzureCli behavior.
            var client = new SecretClient(vaultUri, new AzureCliCredential());
            KeyVaultSecret retrievedSecret = client.GetSecret(secretName).Value;

            if (!silent)
            {
                Console.WriteLine("Connection string successfully retrieved.");
            }

            return retrievedSecret.Value;
        }
        catch (CredentialUnavailableException ex)
        {
            throw CreateAzureCliAuthenticationException(ex);
        }
        catch (AuthenticationFailedException ex)
        {
            throw CreateAzureCliAuthenticationException(ex);
        }
    }

    private static bool UseAzureKeyVault(string connectionString) =>
        connectionString.Contains("VaultUrl", StringComparison.OrdinalIgnoreCase);

    private static (Uri VaultUri, string SecretName) ParseKeyVaultConfig(string connectionString)
    {
        string? vaultUrl = null;
        string? secretName = null;

        foreach (string part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int separatorIndex = part.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            string name = part[..separatorIndex].Trim();
            string value = part[(separatorIndex + 1)..].Trim();

            if (name.Equals("VaultUrl", StringComparison.OrdinalIgnoreCase))
            {
                vaultUrl = value;
            }
            else if (name.Equals("Key", StringComparison.OrdinalIgnoreCase))
            {
                secretName = value;
            }
        }

        if (string.IsNullOrWhiteSpace(vaultUrl))
        {
            throw new ArgumentException("Azure Key Vault configuration must contain a non-empty VaultUrl value.", nameof(connectionString));
        }

        if (!Uri.TryCreate(vaultUrl, UriKind.Absolute, out Uri? vaultUri))
        {
            throw new ArgumentException($"Azure Key Vault configuration contains an invalid VaultUrl: '{vaultUrl}'.", nameof(connectionString));
        }

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Azure Key Vault configuration must contain a non-empty Key value.", nameof(connectionString));
        }

        return (vaultUri, secretName);
    }

    private static Exception CreateAzureCliAuthenticationException(Exception innerException) =>
        new InvalidOperationException(
            "Unable to log into Azure Key Vault.\r\n" +
            "Run \"az login\" to obtain a token.\r\n" +
            "Requires Azure CLI - https://aka.ms/installazurecliwindows\r\n" +
            $"Details: {innerException.Message}",
            innerException);
}
