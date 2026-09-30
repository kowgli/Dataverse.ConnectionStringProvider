# Dataverse.ConnectionStringProvider.Modern

A lightweight **.NET 10** library that lets Dataverse applications accept either a normal connection string or a reference to a secret stored in Azure Key Vault.

Calling code passes the configured value to one method. The provider returns either the original connection string or the secret retrieved securely from Key Vault.

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

If the value does not contain `VaultUrl`, it is returned unchanged.

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

## Authentication

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
ConnectionStringProviderModern\bin\NuGet\Dataverse.ConnectionStringProvider.Modern.<version>.nupkg
```

## License

Licensed under the [MIT License](https://opensource.org/license/mit/).
