<#
.SYNOPSIS
    Verifies that the imported tree differs from the pinned upstream snapshot only by the
    documented renames and deviations.

.DESCRIPTION
    Every .cs file that came from upstream is compared after both sides are canonicalized:
    project/namespace names collapse to NS.* and the internal connector type collapses to
    CONNTYPE. A difference therefore means the import changed something that is not a rename.

    Files listed in $expected carry a recorded deviation (see docs/opengauss-connector-import.md,
    section "Recorded deviations from upstream") and are reported as informational.

.PARAMETER UpstreamPath
    Local checkout of the pinned upstream commit ba5e8ba.
#>
param(
    [string] $UpstreamPath
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
# Same relative location the docs use: <repo>/../../dotnet-source/openGauss-connector-adonet
if (-not $UpstreamPath) {
    $UpstreamPath = Join-Path $repo '..\..\dotnet-source\openGauss-connector-adonet'
}
$up = (Resolve-Path $UpstreamPath).Path

$projectMap = [ordered]@{
    'OpenGaussConnector.SourceGenerators'    = 'NS.SourceGenerators'
    'OpenGaussConnector.GeoJSON'             = 'NS.GeoJSON'
    'OpenGaussConnector.Json.NET'            = 'NS.Json.NET'
    'OpenGaussConnector.NetTopologySuite'    = 'NS.NetTopologySuite'
    'OpenGaussConnector.NodaTime.Tests'      = 'NS.NodaTime.Tests'
    'OpenGaussConnector.NodaTime'            = 'NS.NodaTime'
    'OpenGaussConnector.OpenTelemetry'       = 'NS.OpenTelemetry'
    'OpenGaussConnector.Specification.Tests' = 'NS.Specification.Tests'
    'OpenGaussConnector.Benchmarks'          = 'NS.Benchmarks'
    'OpenGaussConnector.TrimmingTests'       = 'NS.TrimmingTests'
    'OpenGaussConnector.PluginTests'         = 'NS.PluginTests'
    'OpenGaussConnector.Tests'               = 'NS.Tests'
    'OpenGaussConnector'                     = 'NS'
}

$upProjectMap = [ordered]@{
    'OpenGauss.SourceGenerators'    = 'NS.SourceGenerators'
    'OpenGauss.GeoJSON.NET'         = 'NS.GeoJSON'
    'OpenGauss.Json.NET'            = 'NS.Json.NET'
    'OpenGauss.NetTopologySuite'    = 'NS.NetTopologySuite'
    'OpenGauss.NodaTime.Tests'      = 'NS.NodaTime.Tests'
    'OpenGauss.NodaTime.NET'        = 'NS.NodaTime'
    'OpenGauss.OpenTelemetry.NET'   = 'NS.OpenTelemetry'
    'OpenGauss.Specification.Tests' = 'NS.Specification.Tests'
    'OpenGauss.Benchmarks'          = 'NS.Benchmarks'
    'OpenGauss.TrimmingTests'       = 'NS.TrimmingTests'
    'OpenGauss.PluginTests'         = 'NS.PluginTests'
    'OpenGauss.Tests'               = 'NS.Tests'
    'OpenGauss.NET'                 = 'NS'
}

function Normalize-Ours([string] $text) {
    foreach ($k in $projectMap.Keys) { $text = $text.Replace($k, $projectMap[$k]) }
    # The internal type was renamed because its old name equals the root namespace.
    return $text.Replace('PhysicalConnector', 'CONNTYPE')
}

function Normalize-Up([string] $text) {
    foreach ($k in $upProjectMap.Keys) { $text = $text.Replace($k, $upProjectMap[$k]) }
    $text = $text.Replace('OpenGaussConnector', 'CONNTYPE')
    # Upstream reached the namespace through the parent segment 'OpenGauss' (e.g. 'NET.Schema.X').
    return [regex]::Replace($text, '(?<![\w.])NET\.([A-Z])', 'NS.$1')
}

function Map-Path([string] $rel) {
    $r = $rel.Replace('\', '/')
    $map = [ordered]@{
        'src/OpenGauss.NET/Internal/OpenGaussConnector' = 'src/OpenGaussConnector/Internal/PhysicalConnector'
        'src/OpenGauss.NET/'                  = 'src/OpenGaussConnector/'
        'src/OpenGauss.SourceGenerators/'     = 'src/OpenGaussConnector.SourceGenerators/'
        'src/OpenGauss.GeoJSON.NET/'          = 'src/OpenGaussConnector.GeoJSON/'
        'src/OpenGauss.Json.NET/'             = 'src/OpenGaussConnector.Json.NET/'
        'src/OpenGauss.NetTopologySuite/'     = 'src/OpenGaussConnector.NetTopologySuite/'
        'src/OpenGauss.NodaTime.NET/'         = 'src/OpenGaussConnector.NodaTime/'
        'src/OpenGauss.OpenTelemetry.NET/'    = 'src/OpenGaussConnector.OpenTelemetry/'
        'src/Shared/'                         = 'src/Shared/'
        'test/OpenGauss.Tests/'               = 'tests/OpenGaussConnector.Tests/'
        'test/OpenGauss.PluginTests/'         = 'tests/OpenGaussConnector.PluginTests/'
        'test/OpenGauss.NodaTime.Tests/'      = 'tests/OpenGaussConnector.NodaTime.Tests/'
        'test/OpenGauss.Specification.Tests/' = 'tests/OpenGaussConnector.Specification.Tests/'
        'test/OpenGauss.TrimmingTests/'       = 'tests/OpenGaussConnector.TrimmingTests/'
        'test/OpenGauss.Benchmarks/'          = 'tests/OpenGaussConnector.Benchmarks/'
    }
    foreach ($k in $map.Keys) { if ($r.StartsWith($k)) { return $map[$k] + $r.Substring($k.Length) } }
    return $r
}

# Files with an explained difference. Every entry cites the record that accounts for it;
# anything not listed here is a defect.
$expected = @(
    # Import deviations D01-D15, see docs/opengauss-connector-import.md.
    'src/OpenGaussConnector/Properties/AssemblyInfo.cs'
    'src/OpenGaussConnector/OpenGaussException.cs'
    'src/OpenGaussConnector/PostgresException.cs'
    'src/OpenGaussConnector/Internal/PhysicalConnector.cs'
    'src/OpenGaussConnector/Internal/PhysicalConnector.Auth.cs'
    'src/OpenGaussConnector.SourceGenerators/TypeHandlerSourceGenerator.cs'
    'src/OpenGaussConnector/Types/OpenGaussDbType.cs'
    'src/OpenGaussConnector/Types/OpenGaussInterval.cs'
    'src/OpenGaussConnector/Types/OpenGaussTimeSpan.cs'
    'tests/OpenGaussConnector.TrimmingTests/Program.cs'
    # M1-B: data source support adds an internal constructor and a fixed connection string.
    'src/OpenGaussConnector/OpenGaussConnection.cs'
    # M1-C..G: the snapshot's plugin handlers predate the object-level handler API, so they gained
    # ValidateObjectAndGetLength/WriteObjectWithLength; the simple handler base provides them once.
    'src/OpenGaussConnector/Internal/TypeHandling/OpenGaussSimpleTypeHandler.cs'
    'src/OpenGaussConnector.GeoJSON/Internal/GeoJSONHandler.cs'
    'src/OpenGaussConnector.NetTopologySuite/Internal/NetTopologySuiteHandler.cs'
    # D16: the test connection string comes from OPENGAUSS_TEST_DB, with NPGSQL_TEST_DB as a fallback.
    'tests/OpenGaussConnector.Tests/TestUtil.cs'
    # D17: messages and test reasons name the AppContext switches the provider actually reads.
    'src/OpenGaussConnector/Internal/TypeHandlers/DateTimeHandlers/DateTimeUtils.cs'
    'src/OpenGaussConnector/Internal/TypeHandlers/DateTimeHandlers/TimestampHandler.cs'
    'src/OpenGaussConnector/Internal/TypeHandlers/DateTimeHandlers/TimestampTzHandler.cs'
    'src/OpenGaussConnector.NodaTime/Internal/DateHandler.cs'
    'src/OpenGaussConnector.NodaTime/Internal/TimestampHandler.cs'
    'src/OpenGaussConnector.NodaTime/Internal/TimestampTzHandler.cs'
    'tests/OpenGaussConnector.NodaTime.Tests/LegacyNodaTimeTests.cs'
    'tests/OpenGaussConnector.NodaTime.Tests/NodaTimeInfinityTests.cs'
    'tests/OpenGaussConnector.Tests/Types/DateTimeInfinityTests.cs'
    'tests/OpenGaussConnector.Tests/Types/LegacyDateTimeTests.cs'
    # D02 follow-up: the plugin test dropped a `using` for the pre-import dotted namespace.
    'tests/OpenGaussConnector.PluginTests/GeoJSONTests.cs'
)

# Upstream files with no counterpart: renamed connector files and the dropped NodaTime assembly info.
$expectedMissing = @(
    'src/OpenGauss.NodaTime.NET/Properties/AssemblyInfo.cs'
)

$files = Get-ChildItem "$up\src", "$up\test" -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$differing = @()
$unexpectedMissing = @()
$compared = 0

foreach ($f in $files) {
    $rel = $f.FullName.Replace("$up\", '')
    $oursRel = Map-Path $rel
    $ours = Join-Path $repo $oursRel
    if (-not (Test-Path $ours)) {
        if (-not ($expectedMissing -contains ($rel.Replace('\', '/')))) { $unexpectedMissing += $rel }
        continue
    }
    $compared++
    $upText = Normalize-Up ((Get-Content $f.FullName -Raw) -replace "`r`n", "`n")
    $ourText = Normalize-Ours ((Get-Content $ours -Raw) -replace "`r`n", "`n")
    if ($upText -ne $ourText) {
        $differing += [pscustomobject]@{ Rel = $oursRel; Expected = ($expected -contains $oursRel) }
    }
}

"compared: $compared, differing: $($differing.Count), unexpected missing: $($unexpectedMissing.Count)"
foreach ($m in $unexpectedMissing) { "  MISSING $m" }
foreach ($d in $differing | Where-Object { $_.Expected }) { "  deviation: $($d.Rel)" }
$unexpected = @($differing | Where-Object { -not $_.Expected })
if ($unexpected.Count -gt 0) {
    "UNEXPECTED differences:"
    foreach ($d in $unexpected) { "  $($d.Rel)" }
    exit 1
}
"no unexpected differences"
