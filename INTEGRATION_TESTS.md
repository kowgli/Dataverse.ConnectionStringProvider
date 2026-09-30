# Integration tests

Both integration-test projects exercise their respective `ConnectionString.Get` implementation. They do not access Azure Key Vault directly.

## Local configuration

The vault URL is intentionally not stored in source control. Set this environment variable in your user profile:

```powershell
[Environment]::SetEnvironmentVariable(
    "CONNECTION_STRING_PROVIDER_TEST_VAULT_URL",
    "https://your-test-vault.vault.azure.net/",
    "User")
```

Restart the terminal or IDE after setting a persistent variable, or set it for the current PowerShell process:

```powershell
$env:CONNECTION_STRING_PROVIDER_TEST_VAULT_URL = "https://your-test-vault.vault.azure.net/"
```

Authenticate before running the tests:

```powershell
az login
dotnet test ConnectionStringProvider.IntegrationTests.Net462
dotnet test ConnectionStringProviderModern.IntegrationTests
```

The signed-in identity must have permission to read `TEST-SECRET`. Each test expects its value to be `12345`.

