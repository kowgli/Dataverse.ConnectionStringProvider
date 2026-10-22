# Integration tests

Both integration-test projects exercise their respective `ConnectionString.Get` implementation. Azure Key Vault tests retrieve a real secret through the provider rather than using the Azure SDK directly.

## Coverage

| Project | Target framework | Direct connection string | File connection string | Azure Key Vault secret |
| --- | --- | --- | --- | --- |
| `ConnectionStringProvider.IntegrationTests.Net462` | .NET Framework 4.6.2 | Yes | No file-specific test yet | Yes |
| `ConnectionStringProviderModern.IntegrationTests` | .NET 10 | Yes | Yes | Yes |

Both library implementations support `File=<path>`; currently only the modern test project has a file-specific test.

`Get_ReturnsFileConnectionString` writes a randomly generated value to `TestConnectionString.txt` in the test process's current working directory, resolves `File=TestConnectionString.txt`, and checks that the same value is returned. It requires a writable working directory, does not use Azure, and currently leaves the file behind after running. This test covers a relative path and basic retrieval; it does not check whitespace trimming, case-insensitive prefixes, absolute paths, or file-read failures.

## Run local tests without Azure

The Key Vault tests have the `Integration` category. Exclude that category to run the direct-connection-string tests in both projects and the file test in the modern project without a vault URL or Azure CLI login:

```powershell
dotnet test ConnectionStringProvider.IntegrationTests.Net462 --filter "TestCategory!=Integration"
dotnet test ConnectionStringProviderModern.IntegrationTests --filter "TestCategory!=Integration"
```

Run these commands from the repository root. The modern project requires a .NET 10 SDK; the .NET Framework project requires Windows and the .NET Framework 4.6.2 targeting/build tools.

## Azure Key Vault configuration

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

Each Key Vault test resolves `VaultUrl=<configured-vault-url>; Key=TEST-SECRET` through the provider and expects the secret's value to be `12345`. The signed-in identity must have permission to read `TEST-SECRET`.

Authenticate before running the full suites:

```powershell
az login
dotnet test ConnectionStringProvider.IntegrationTests.Net462
dotnet test ConnectionStringProviderModern.IntegrationTests
```

If browser-based sign-in is unavailable, use `az login --use-device-code` instead. Running the full suites without a configured vault URL or a usable Azure CLI identity will fail the Key Vault tests; file and direct-value tests do not require either.
