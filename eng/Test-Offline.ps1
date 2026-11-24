<#
.SYNOPSIS
Runs every test that does not need a database server.

.DESCRIPTION
The suites that talk to a server (the upstream NUnit suite, the plugin tests, the NodaTime tests and the
ADO.NET specification tests) only run with OPENGAUSS_TEST_DB (or the legacy NPGSQL_TEST_DB) pointing at a
reachable instance. This script covers the rest, so a checkout can be verified without one:

- Dependency injection registration, lifetime and disposal (15 tests)
- Extension registration and OpenTelemetry source subscription (8 tests)
- Upstream fixtures that are pure in-memory: connection string builder, buffers, SQL parser, name
  translator, pgpass files and entries (167 tests)

.PARAMETER Configuration
Build configuration; Release by default.

.EXAMPLE
./eng/Test-Offline.ps1
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

# Fixtures verified to need no server. Extend this list when a fixture stops touching the network.
$offlineFilter = @(
    'FullyQualifiedName~ConnectionStringBuilderTests',
    'FullyQualifiedName~PgPassEntryTests',
    'FullyQualifiedName~PgPassFileTests',
    'FullyQualifiedName~ReadBufferTests',
    'FullyQualifiedName~WriteBufferTests',
    'FullyQualifiedName~SnakeCaseNameTranslatorTests',
    'FullyQualifiedName~SqlQueryParserTests'
) -join '|'

$suites = @(
    @{ Name = 'DependencyInjection.Tests'; Project = 'tests/OpenGaussConnector.DependencyInjection.Tests/OpenGaussConnector.DependencyInjection.Tests.csproj'; Filter = $null },
    @{ Name = 'Extensions.Tests'; Project = 'tests/OpenGaussConnector.Extensions.Tests/OpenGaussConnector.Extensions.Tests.csproj'; Filter = $null },
    @{ Name = 'Tests (offline fixtures)'; Project = 'tests/OpenGaussConnector.Tests/OpenGaussConnector.Tests.csproj'; Filter = $offlineFilter }
)

$failed = @()

foreach ($suite in $suites) {
    Write-Host "==> $($suite.Name)" -ForegroundColor Cyan
    $arguments = @('test', (Join-Path $root $suite.Project), '-c', $Configuration, '--nologo')
    if ($suite.Filter) {
        $arguments += @('--filter', $suite.Filter)
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        $failed += $suite.Name
    }
}

Write-Host ''
if ($failed) {
    Write-Host "Offline tests FAILED: $($failed -join ', ')" -ForegroundColor Red
    exit 1
}

Write-Host 'Offline tests passed. Suites that need a server were skipped; set OPENGAUSS_TEST_DB to run them.' -ForegroundColor Green
