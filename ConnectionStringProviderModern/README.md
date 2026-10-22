# Dataverse.ConnectionStringProvider.Modern

A lightweight **.NET 10** library that lets Dataverse applications accept a normal connection string, a path to a plain-text file containing it, or a reference to a secret stored in Azure Key Vault.

Calling code passes the configured value to one method. The provider returns the original connection string, the trimmed contents of the specified file, or the secret retrieved securely from Key Vault.

Current release: **1.3.0**.

## Installation

```powershell
Install-Package Dataverse.ConnectionStringProvider.Modern
```

The package targets **.NET 10** (`net10.0`). It does not target .NET Framework. For .NET Framework 4.6.2, use the legacy `Dataverse.ConnectionStringProvider` package. Both packages expose the same client API in the `ConnectionStringProvider` namespace, so consuming code can use `ConnectionString.Get(...)` on either target framework.

## Usage

### Direct connection string

```csharp
using ConnectionStringProvider;

var configuredValue =
    "Url=https://example.crm.dynamics.com;" +
    "AuthType=ClientSecret;" +
    "ClientId=00000000-0000-0000-0000-000000000000;" +
    "ClientSecret=your-client-secret";

string connectionString = ConnectionString.Get(configuredValue);
```

If the value starts with neither `VaultUrl=` nor `File=` (case-insensitive), it is returned unchanged. Prefix detection does not trim leading whitespace; these prefixes must start at the first character.

### Connection string from a file

Store the complete connection string in a plain-text file, for example `connection-string.txt`:

```text
Url=https://contoso.crm.dynamics.com;AuthType=ClientSecret;ClientId=00000000-0000-0000-0000-000000000000;ClientSecret=your-client-secret
```

Pass the file path with the `File=` prefix:

```csharp
using ConnectionStringProvider;

string connectionString = ConnectionString.Get("File=connection-string.txt");

// An absolute path is also supported. Use a verbatim C# string for Windows paths.
string absolutePathConnectionString =
    ConnectionString.Get(@"File=C:\secrets\connection-string.txt");
```

Expected format:

```text
File=<path-to-file>
```

- The `File=` prefix is case-insensitive and must appear at the very beginning of the configured value, without leading whitespace or spaces around `=`. Supply a non-empty path; `File=` alone is not a valid file reference.
- Relative paths are resolved against the application's current working directory, not necessarily its executable directory. The process must have permission to read the file.
- Everything after `File=` is used as the path, unchanged. Do not wrap the path in quotes or append a semicolon or other configuration entries. Spaces within the path are supported, but the provider does not trim the path or expand environment variables.
- Each call reads the entire file with `File.ReadAllText` and returns its contents after `Trim()`, removing leading and trailing whitespace, including a trailing newline. Internal whitespace is preserved. An empty or whitespace-only file returns an empty string.
- The file should contain the connection string itself, not JSON, XML, or a `File=` assignment. Its contents are returned without validation or further resolution, even if they start with `File=` or `VaultUrl=`.
- File-read errors, such as a missing file or insufficient permissions, propagate to the caller. There is no fallback to Azure Key Vault or to the original configured value.
- File resolution does not require Azure CLI authentication and does not write informational console messages, regardless of `silent`.

The file contains plaintext credentials. Restrict access to it and keep it out of source control.

### Azure Key Vault secret

Store the complete Dataverse connection string as an Azure Key Vault secret, then provide the vault URL and secret name:

```csharp
using ConnectionStringProvider;

var configuredValue =
    "VaultUrl=https://your-vault.vault.azure.net/;" +
    "Key=DataverseConnectionString";

string connectionString = ConnectionString.Get(configuredValue);
```

Expected format:

```text
VaultUrl=https://<vault-name>.vault.azure.net/;Key=<secret-name>
```

Start the configuration with `VaultUrl=`, followed by `Key=<secret-name>`. Use the canonical key names shown above for compatibility with both packages.

### Console output

Pass `silent: true` to suppress informational console messages for direct values and Azure Key Vault retrieval:

```csharp
string connectionString = ConnectionString.Get(configuredValue, silent: true);
```

The file branch is always silent. This option does not suppress exceptions.

## Authentication

Authentication is required only for Azure Key Vault references, not for direct connection strings or files.

Azure Key Vault access uses the current Azure SDKs, `Azure.Identity` and `Azure.Security.KeyVault.Secrets`. The provider deliberately uses `AzureCliCredential`, so local authentication comes from the identity established through the Azure CLI:

```powershell
az login
```

If the browser-based sign-in flow cannot be opened directly—for example, when working through Citrix—use device-code authentication instead:

```powershell
az login --use-device-code
```

The authenticated identity must have permission to read the requested Key Vault secret.

## Building and packaging

Build the modern project in Release mode:

```powershell
dotnet build ConnectionStringProviderModern\ConnectionStringProviderModern.csproj --configuration Release
```

Every successful Release build creates:

```text
ConnectionStringProviderModern\bin\NuGet\Dataverse.ConnectionStringProvider.Modern.1.3.0.nupkg
```

The NuGet version comes from the project's `<Version>` property, set to `1.3.0` for this release. The SDK derives assembly and file versions of `1.3.0.0`.

## Tests

See the [test guide](../INTEGRATION_TESTS.md) for local direct/file tests and Azure Key Vault integration-test setup.

## License

Licensed under the [MIT License](https://opensource.org/license/mit/).
