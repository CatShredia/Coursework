param(
    [string]$SettingsPath = "..\ngrok.settings.json"
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = (Resolve-Path (Join-Path $scriptDir "..")).Path
$settingsFullPath = (Resolve-Path (Join-Path $scriptDir $SettingsPath)).Path

if (!(Test-Path $settingsFullPath)) {
    throw "Settings file not found: $settingsFullPath"
}

$settings = Get-Content $settingsFullPath -Raw | ConvertFrom-Json
$publicUrl = "$($settings.PublicUrl)".Trim()

if ([string]::IsNullOrWhiteSpace($publicUrl)) {
    throw "PublicUrl is empty in $settingsFullPath"
}

if ($publicUrl.EndsWith("/")) {
    $publicUrl = $publicUrl.TrimEnd("/")
}

if (-not $publicUrl.StartsWith("http")) {
    throw "PublicUrl must start with http/https. Current: $publicUrl"
}

$apiAppSettingsPath = Join-Path $root "CatshrediasNewsAPI\appsettings.json"
$clientAppSettingsPath = Join-Path $root "CatshrediasNews.Client\wwwroot\appsettings.json"

$api = Get-Content $apiAppSettingsPath -Raw | ConvertFrom-Json
if ($null -eq $api.Cors) {
    $api | Add-Member -MemberType NoteProperty -Name Cors -Value ([pscustomobject]@{})
}
$api.Cors.AllowedOrigins = @($publicUrl, "http://localhost:5110", "https://localhost:7255")

if ($null -eq $api.App) {
    $api | Add-Member -MemberType NoteProperty -Name App -Value ([pscustomobject]@{})
}
$api.App.BaseUrl = $publicUrl

if ($null -eq $api.Api) {
    $api | Add-Member -MemberType NoteProperty -Name Api -Value ([pscustomobject]@{})
}
$api.Api.BaseUrl = $publicUrl

$api | ConvertTo-Json -Depth 100 | Set-Content $apiAppSettingsPath -Encoding UTF8

$client = Get-Content $clientAppSettingsPath -Raw | ConvertFrom-Json
if ($null -eq $client.Api) {
    $client | Add-Member -MemberType NoteProperty -Name Api -Value ([pscustomobject]@{})
}
$client.Api.BaseUrl = "$publicUrl/"
$client | ConvertTo-Json -Depth 20 | Set-Content $clientAppSettingsPath -Encoding UTF8

Write-Host "ngrok url applied: $publicUrl" -ForegroundColor Green
Write-Host "Updated:" -ForegroundColor Green
Write-Host " - $apiAppSettingsPath"
Write-Host " - $clientAppSettingsPath"
