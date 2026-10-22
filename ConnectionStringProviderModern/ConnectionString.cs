using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace ConnectionStringProvider;

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
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        if (IsDirectConnectionString(connectionString))
        {
            if (!silent)
            {
                Console.WriteLine("Detected Dataverse connection string. Using explicitly.");
            }

            return connectionString;
        }

        if (IsFileConnectionString(connectionString))
        {
            return File.ReadAllText(connectionString.Substring(5)).Trim();
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

    private static bool IsDirectConnectionString(string connectionString)
    {
        return !connectionString.ToUpperInvariant().StartsWith("VAULTURL=") && !connectionString.ToUpperInvariant().StartsWith("FILE=");
    }

    private static bool IsFileConnectionString(string connectionString)
    {
        return connectionString.ToUpperInvariant().StartsWith("FILE=") && connectionString.Length > 5;
    }

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
