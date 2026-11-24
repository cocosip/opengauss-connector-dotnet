<#
.SYNOPSIS
Packs the seven packages, inspects them, and consumes them from a throwaway project.

.DESCRIPTION
Verifies the publishable surface of this repository:

1. `dotnet pack` produces the six packages plus core and their symbol packages.
2. Every package carries the five target-framework assets, the license, the icon and the expected
   dependency floor (`OpenGaussConnector` 0.1.0 is a minimum, not an exact pin).
3. Throwaway consumers restore from the produced folder only: a netstandard2.0 and a netstandard2.1
   library prove the standard assets resolve, and net8.0/net9.0/net10.0 console apps build and run
   against all seven packages.

Nothing is left in the repository; the consumers live in a temporary directory.

.PARAMETER Configuration
Build configuration; Release by default.

.PARAMETER PackagesDirectory
Where packages are written and consumed from; defaults to artifacts/packages.

.PARAMETER SkipConsumers
Only pack and inspect, without building or running the consumer projects.

.EXAMPLE
./eng/Verify-Packages.ps1
#>
[CmdletBinding()]
param(
    [string] $Configuration = 'Release',
    [string] $PackagesDirectory = (Join-Path (Split-Path $PSScriptRoot -Parent) 'artifacts/packages'),
    [switch] $SkipConsumers
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$version = '0.1.0'
$targetFrameworks = @('netstandard2.0', 'netstandard2.1', 'net8.0', 'net9.0', 'net10.0')
$extensionPackages = @(
    'OpenGaussConnector.DependencyInjection',
    'OpenGaussConnector.GeoJSON',
    'OpenGaussConnector.Json.NET',
    'OpenGaussConnector.NetTopologySuite',
    'OpenGaussConnector.NodaTime',
    'OpenGaussConnector.OpenTelemetry'
)
$packages = @('OpenGaussConnector') + $extensionPackages
$failures = @()

function Fail([string] $message) { $script:failures += $message }

Write-Host '==> pack' -ForegroundColor Cyan
if (Test-Path $PackagesDirectory) { Remove-Item $PackagesDirectory -Recurse -Force }
& dotnet pack (Join-Path $root 'OpenGaussConnector.slnx') -c $Configuration -o $PackagesDirectory --nologo
if ($LASTEXITCODE -ne 0) { throw 'dotnet pack failed' }

$work = Join-Path ([System.IO.Path]::GetTempPath()) ('opengauss-connector-verify-' + [guid]::NewGuid().ToString('N'))

try {
    Write-Host '==> inspect' -ForegroundColor Cyan
    foreach ($id in $packages) {
        $nupkg = Join-Path $PackagesDirectory "$id.$version.nupkg"
        $snupkg = Join-Path $PackagesDirectory "$id.$version.snupkg"
        if (-not (Test-Path $nupkg)) { Fail "${id}: package missing"; continue }
        if (-not (Test-Path $snupkg)) { Fail "${id}: symbol package missing" }

        $extract = Join-Path $work $id
        Expand-Archive $nupkg -DestinationPath $extract
        $nuspec = [xml](Get-Content (Get-ChildItem $extract -Filter *.nuspec).FullName -Raw)
        $metadata = $nuspec.package.metadata

        if ($metadata.id -ne $id) { Fail "${id}: nuspec id is $($metadata.id)" }
        if ($metadata.version -ne $version) { Fail "${id}: nuspec version is $($metadata.version)" }
        if ($metadata.license.InnerText -ne 'MulanPSL-2.0') { Fail "${id}: license is $($metadata.license.InnerText)" }
        if (-not $metadata.authors -or $metadata.authors -match 'Microestc') { Fail "${id}: authors are $($metadata.authors)" }
        if ($metadata.icon -and -not (Test-Path (Join-Path $extract $metadata.icon))) { Fail "${id}: icon $($metadata.icon) missing from package" }
        if (-not $metadata.readme) { Fail "${id}: nuspec declares no readme" }
        elseif (-not (Test-Path (Join-Path $extract $metadata.readme))) { Fail "${id}: readme $($metadata.readme) missing from package" }

        foreach ($tfm in $targetFrameworks) {
            $assembly = Join-Path $extract "lib/$tfm/$id.dll"
            if (-not (Test-Path $assembly)) { Fail "${id}: lib/$tfm/$id.dll missing" }
            if (-not (Test-Path (Join-Path $extract "lib/$tfm/$id.xml"))) { Fail "${id}: lib/$tfm/$id.xml missing" }
        }

        foreach ($group in $metadata.dependencies.group) {
            $tfm = $group.targetFramework
            $dependency = @($group.dependency) | Where-Object { $_.id -eq 'OpenGaussConnector' }
            if ($id -eq 'OpenGaussConnector') {
                if ($dependency) { Fail "${id}: must not depend on itself ($tfm)" }
            }
            elseif (-not $dependency) {
                Fail "${id}: no OpenGaussConnector dependency for $tfm"
            }
            elseif ($dependency.version -ne $version) {
                Fail "${id}: OpenGaussConnector floor is $($dependency.version) for $tfm, expected $version"
            }
        }

        # The core package only supplements the BCL for the standard targets; the modern ones carry nothing.
        # Extensions always depend on the core package, but must never drag in BCL supplements themselves.
        $groups = @($metadata.dependencies.group)
        if ($id -eq 'OpenGaussConnector') {
            foreach ($group in $groups) {
                $isStandard = $group.targetFramework -match 'Standard'
                $groupDependencies = @($group.dependency | Where-Object { $_ })
                if ($isStandard -and -not $groupDependencies.Count) { Fail "${id}: standard group $($group.targetFramework) has no dependencies" }
                if (-not $isStandard -and $groupDependencies.Count) { Fail "${id}: modern group $($group.targetFramework) carries dependencies" }
            }
        }
        else {
            foreach ($group in $groups) {
                $supplements = @($group.dependency | Where-Object { $_ -and ($_.id -like 'System.*' -or $_.id -eq 'Microsoft.Bcl.HashCode') })
                if ($supplements.Count) { Fail "${id}: BCL supplement in $($group.targetFramework)" }
            }
        }

        if (Test-Path $snupkg) {
            $symbols = Join-Path $work "$id-symbols"
            Expand-Archive $snupkg -DestinationPath $symbols
            foreach ($tfm in $targetFrameworks) {
                if (-not (Test-Path (Join-Path $symbols "lib/$tfm/$id.pdb"))) { Fail "${id}: no symbol for $tfm" }
            }
        }

        Write-Host "  ok: $id"
    }

    if (-not $SkipConsumers) {
        Write-Host '==> consume' -ForegroundColor Cyan
        $feed = $PackagesDirectory
        # An isolated package cache proves the consumers resolve this run's packages, not a cached copy.
        $cache = Join-Path $work 'packages'

        function Get-AssetPath([string] $projectDirectory, [string] $packageId) {
            $assets = Get-Content (Join-Path $projectDirectory 'obj/project.assets.json') -Raw | ConvertFrom-Json
            $target = $assets.targets.PSObject.Properties | Select-Object -First 1
            $entry = $target.Value.PSObject.Properties | Where-Object { $_.Name -like "$packageId/*" } | Select-Object -First 1
            if (-not $entry) { return $null }
            $compile = $entry.Value.compile.PSObject.Properties | Select-Object -First 1
            return $compile.Name
        }

        foreach ($tfm in @('netstandard2.0', 'netstandard2.1')) {
            $project = Join-Path $work "consume-$tfm"
            New-Item -ItemType Directory -Force -Path $project | Out-Null
            @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>$tfm</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="OpenGaussConnector" Version="$version" />
  </ItemGroup>
</Project>
"@ | Set-Content (Join-Path $project 'consume.csproj')
            @'
using OpenGaussConnector;

namespace Consume
{
    public static class Probe
    {
        public static string Describe(string connectionString)
        {
            var builder = new OpenGaussConnectionStringBuilder(connectionString);
            using var connection = new OpenGaussConnection(builder.ConnectionString);
            return connection.State.ToString() + builder.Database;
        }
    }
}
'@ | Set-Content (Join-Path $project 'Probe.cs')
            @"
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content (Join-Path $project 'nuget.config')
            & dotnet restore (Join-Path $project 'consume.csproj') --nologo --packages $cache
            if ($LASTEXITCODE -ne 0) { Fail "${tfm}: consumer restore failed"; continue }
            $asset = Get-AssetPath $project 'OpenGaussConnector'
            if ($asset -ne "lib/$tfm/OpenGaussConnector.dll") { Fail "${tfm}: resolved asset is $asset" ; continue }
            & dotnet build (Join-Path $project 'consume.csproj') -c $Configuration --no-restore --nologo
            if ($LASTEXITCODE -ne 0) { Fail "${tfm}: consumer build failed"; continue }
            Write-Host "  ok: $tfm ($asset)"
        }

        foreach ($tfm in @('net8.0', 'net9.0', 'net10.0')) {
            $project = Join-Path $work "run-$tfm"
            New-Item -ItemType Directory -Force -Path $project | Out-Null
            $references = ($packages | ForEach-Object { "    <PackageReference Include=`"$_`" Version=`"$version`" />" }) -join "`n"
            @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>$tfm</TargetFramework>
    <OutputType>Exe</OutputType>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
$references
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="$(if ($tfm -eq 'net8.0') { '8.0.0' } elseif ($tfm -eq 'net9.0') { '9.0.0' } else { '10.0.0' })" />
    <PackageReference Include="OpenTelemetry" Version="1.20.1" />
  </ItemGroup>
</Project>
"@ | Set-Content (Join-Path $project 'consume.csproj')
            $program = @'
using System;
using System.Data.Common;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using OpenGaussConnector;
using OpenGaussConnector.TypeMapping;
using OpenTelemetry;
using OpenTelemetry.Trace;

var connectionString = "Host=127.0.0.1;Port=5432;Database=driver_probe;Username=probe;Password=secret";

DbProviderFactories.RegisterFactory("OpenGaussConnector", OpenGaussFactory.Instance!);
var factory = DbProviderFactories.GetFactory("OpenGaussConnector");
if (factory.GetType() != typeof(OpenGaussFactory))
    throw new Exception("DbProviderFactories did not resolve OpenGaussFactory");

var builder = new OpenGaussConnectionStringBuilder(connectionString);
if (builder.Database != "driver_probe")
    throw new Exception("connection string builder lost the database");

using var pooled = new OpenGaussConnection(builder.ConnectionString);
if (pooled.State != System.Data.ConnectionState.Closed)
    throw new Exception("a new connection must be closed");

// Data source
using var dataSource = new OpenGaussDataSourceBuilder(connectionString).Build();
using var fromDataSource = dataSource.CreateConnection();
if (fromDataSource.ConnectionString.Contains("secret"))
    throw new Exception("data source connection string must not expose the password");
using var command = dataSource.CreateCommand("SELECT 1");
if (command.CommandText != "SELECT 1")
    throw new Exception("data source did not create the command");

// Dependency injection
using var provider = new ServiceCollection()
    .AddOpenGaussDataSource(connectionString)
    .AddOpenGaussConnection(connectionString)
    .BuildServiceProvider();
if (provider.GetRequiredService<DbDataSource>() is not OpenGaussDataSource)
    throw new Exception("DI did not resolve the data source alias");
if (provider.GetRequiredService<DbConnection>() is not OpenGaussConnection)
    throw new Exception("DI did not resolve the connection alias");

// Extensions
OpenGaussConnection.GlobalTypeMapper
    .UseNodaTime()
    .UseGeoJson()
    .UseNetTopologySuite()
    .UseJsonNet();

using var tracerProvider = Sdk.CreateTracerProviderBuilder().AddOpenGauss().Build();
if (tracerProvider is null)
    throw new Exception("OpenTelemetry plugin did not build a provider");

var targetFramework = args.Length > 0 ? args[0] : throw new Exception("expected the target framework as the first argument");
var providerAssemblies = AppDomain.CurrentDomain.GetAssemblies()
    .Where(a => a.GetName().Name!.StartsWith("OpenGaussConnector", StringComparison.Ordinal))
    .ToArray();
if (providerAssemblies.Length != 7)
    throw new Exception($"expected 7 provider assemblies, loaded {providerAssemblies.Length}");
foreach (var assembly in providerAssemblies)
{
    var name = assembly.GetName();
    if (name.Version!.ToString() != "0.1.0.0")
        throw new Exception($"{name.Name} has version {name.Version}");
    if (!assembly.Location.StartsWith(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase))
        throw new Exception($"{name.Name} was loaded from {assembly.Location}");
}

Console.WriteLine($"consumer ok ({targetFramework}): " + string.Join(", ",
    new[] { "core", "data source", "di", "extensions", "tracing" }) + " · " + string.Join(", ", providerAssemblies.Select(a => a.GetName().Name)));
'@
            Set-Content (Join-Path $project 'Program.cs') $program
            @"
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$feed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content (Join-Path $project 'nuget.config')
            & dotnet restore (Join-Path $project 'consume.csproj') --nologo --packages $cache
            if ($LASTEXITCODE -ne 0) { Fail "${tfm}: consumer restore failed"; continue }
            foreach ($id in $packages) {
                $asset = Get-AssetPath $project $id
                if ($asset -ne "lib/$tfm/$id.dll") { Fail "${tfm}: $id resolved to $asset" }
            }
            & dotnet build (Join-Path $project 'consume.csproj') -c $Configuration --no-restore --nologo
            if ($LASTEXITCODE -ne 0) { Fail "${tfm}: consumer build failed"; continue }
            & dotnet (Join-Path $project "bin/$Configuration/$tfm/consume.dll") $tfm
            if ($LASTEXITCODE -ne 0) { Fail "${tfm}: consumer run failed"; continue }
            Write-Host "  ok: $tfm"
        }
    }
}
finally {
    if (Test-Path $work) { Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue }
}

Write-Host ''
if ($failures.Count) {
    foreach ($failure in $failures) { Write-Host "  FAIL $failure" -ForegroundColor Red }
    Write-Host "Package verification FAILED ($($failures.Count) problems)." -ForegroundColor Red
    exit 1
}

Write-Host "Package verification passed: $($packages.Count) packages, $($targetFrameworks.Count) assets each." -ForegroundColor Green
