# Dataverse.ConnectionStringProvider

A lightweight .NET Framework library that lets Dataverse applications accept either a normal connection string or a reference to a secret stored in Azure Key Vault.

Calling code passes the configured value to one method. The provider returns either the original connection string or the secret retrieved securely from Key Vault.

## Installation

```powershell
Install-Package Dataverse.ConnectionStringProvider
```

The package targets **.NET Framework 4.6.2**.

## Usage

### Direct connection string

```csharp
using ConnectionStringProvider;

var configuredValue =
    "Url=https://contoso.crm.dynamics.com;" +
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
    "VaultUrl=https://contoso.vault.azure.net/;" +
    "Key=DataverseConnectionString";

string connectionString = ConnectionString.Get(configuredValue);
```

Expected format:

```text
VaultUrl=https://<vault-name>.vault.azure.net/;Key=<secret-name>
```

## Authentication

Azure Key Vault access uses `Microsoft.Azure.Services.AppAuthentication`. Install the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) if it is not already available. The current implementation authenticates local development through Azure CLI credentials:

```powershell
az login
```

If the browser-based sign-in flow cannot be opened directly—for example, when working through Citrix—use device-code authentication instead:

```powershell
az login --use-device-code
```

Follow the displayed instructions to open the verification page on a device with browser access and enter the provided code. The authenticated identity must have permission to read the requested secret.

## Building and packaging

Restore dependencies and build in Release mode. Every successful Release build creates:

```text
ConnectionStringProvider\bin\NuGet\Dataverse.ConnectionStringProvider.<version>.nupkg
```

The NuGet version comes from the compiled assembly version. Assembly version `1.0.0.0` produces package version `1.0.0`.

The build uses `tools\nuget.exe`. Override its location with the `NuGetExePath` MSBuild property if necessary.

## License

Licensed under the [MIT License](https://opensource.org/license/mit/).


