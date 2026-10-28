param([int]$Port = 5187)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$apiDirectory = Join-Path $root 'src\MovieApp.Api'
$evidenceDirectory = Join-Path $root ('artifacts\local-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $evidenceDirectory | Out-Null
$saved = @{}
foreach ($name in @('ASPNETCORE_ENVIRONMENT','Admin__ApiKey','Database__Path','Firebase__Mode')) { $saved[$name] = [Environment]::GetEnvironmentVariable($name,'Process') }
$apiProcess = $null
$utf8 = [System.Text.UTF8Encoding]::new($false, $true)
$evidence = [ordered]@{ status = 'RUNNING'; baseUrl = "http://127.0.0.1:$Port"; directory = $evidenceDirectory; checks = @() }
function Assert-Check([bool]$condition,[string]$name) { if (-not $condition) { throw "Verification failed: $name" }; $script:evidence.checks += $name; Write-Output "PASS: $name" }
function Start-TestApi([string]$suffix) {
    $script:apiProcess = Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @('bin\Debug\net10.0\MovieApp.Api.dll','--urls',"http://127.0.0.1:$Port") -WorkingDirectory $apiDirectory -WindowStyle Hidden -RedirectStandardOutput (Join-Path $evidenceDirectory "stdout-$suffix.log") -RedirectStandardError (Join-Path $evidenceDirectory "stderr-$suffix.log") -PassThru
    for ($attempt=0;$attempt -lt 80;$attempt++) {
        $script:apiProcess.Refresh()
        if ($script:apiProcess.HasExited) { throw "API startup exited; see logs in $evidenceDirectory" }
        try { $health = Invoke-RestMethod "$base/health" -TimeoutSec 2; if ($health.status -eq 'healthy') { return } } catch { Start-Sleep -Milliseconds 250 }
    }
    throw 'API startup timeout'
}
function Stop-TestApi {
    if ($script:apiProcess) { $script:apiProcess.Refresh(); if (-not $script:apiProcess.HasExited) { Stop-Process -Id $script:apiProcess.Id; $script:apiProcess.WaitForExit() }; $script:apiProcess = $null }
}
try {
    $probe = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback,$Port)
    $probe.Start(); $probe.Stop()
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $random = New-Object byte[] 32
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($random) } finally { $rng.Dispose() }
    $env:Admin__ApiKey = [Convert]::ToBase64String($random)
    $env:Database__Path = Join-Path $evidenceDirectory 'verification.db'
    $env:Firebase__Mode = 'Local'
    $base = "http://127.0.0.1:$Port"
    Start-TestApi 'first'
    Assert-Check $true 'GET /health returns healthy'
    $page = Invoke-RestMethod "$base/api/movies?page=1&pageSize=10"
    Assert-Check ($page.items.Count -eq 10 -and $page.totalItems -eq 31 -and $page.totalPages -eq 4 -and $page.hasNextPage) 'Pagination and metadata'
    $search = Invoke-RestMethod "$base/api/movies/search?query=toy"
    Assert-Check ($search.totalItems -eq 2) 'Search toy returns two records'
    $ordinary = Invoke-RestMethod "$base/api/movies/1001"
    Assert-Check ($ordinary.title -eq 'Toy Harbor' -and $ordinary.eligibilityEvidence -eq 'unverified') 'Details and demo trust marker'
    $unauthorized = 0
    try { Invoke-WebRequest "$base/api/movies/9001" -Method Patch -ContentType 'application/json' -Body '{"title":"Unauthorized"}' -UseBasicParsing | Out-Null } catch { $unauthorized = [int]$_.Exception.Response.StatusCode }
    Assert-Check ($unauthorized -eq 401) 'PATCH without key rejected'
    $patch = Invoke-WebRequest "$base/api/movies/9001" -Method Patch -Headers @{'X-Admin-Key'=$env:Admin__ApiKey} -ContentType 'application/json' -Body '{"title":"Demo Edit Sandbox - Verified","overview":"Isolated HTTP verification record.","voteAverage":8.3}' -UseBasicParsing
    Assert-Check ($patch.StatusCode -eq 200 -and $patch.Headers['X-Notification-Status'] -eq 'not_configured') 'Protected PATCH persists; explicitly no real delivery'
    $updated = Invoke-RestMethod "$base/api/movies/9001"
    Assert-Check ($updated.title -eq 'Demo Edit Sandbox - Verified' -and $updated.voteAverage -eq 8.3) 'GET confirms edit'
    $swagger = Invoke-WebRequest "$base/swagger/index.html" -UseBasicParsing
    Assert-Check ($swagger.StatusCode -eq 200 -and $swagger.Content.Contains('Swagger UI')) 'Swagger UI loads'
    $swaggerScript = Invoke-WebRequest "$base/swagger/swagger-ui-bundle.js" -UseBasicParsing
    Assert-Check ($swaggerScript.StatusCode -eq 200) 'Swagger JavaScript asset loads'
    $openapi = Invoke-WebRequest "$base/swagger/v1/swagger.json" -UseBasicParsing
    Assert-Check ($openapi.StatusCode -eq 200 -and $openapi.Content.Contains('X-Admin-Key')) 'OpenAPI schema includes authentication header'
    [System.IO.File]::WriteAllText((Join-Path $evidenceDirectory 'openapi.json'),$openapi.Content,$utf8)
    $admin = Invoke-WebRequest "$base/admin" -UseBasicParsing
    Assert-Check ($admin.StatusCode -eq 200 -and $admin.Content.Contains('MovieApp demo editor')) 'Admin page loads'
    $registration = Invoke-RestMethod "$base/api/devices/register" -Method Post -ContentType 'application/json' -Body '{"token":"local-verification-not-a-real-fcm-token-12345"}'
    Assert-Check ($registration.status -eq 'registered') 'Device registration'
    $removal = Invoke-WebRequest "$base/api/devices/unregister" -Method Post -ContentType 'application/json' -Body '{"token":"local-verification-not-a-real-fcm-token-12345"}' -UseBasicParsing
    Assert-Check ($removal.StatusCode -eq 204) 'Device unregistration'
    Stop-TestApi
    Start-TestApi 'restart'
    $persisted = Invoke-RestMethod "$base/api/movies/9001"
    Assert-Check ($persisted.title -eq $updated.title -and $persisted.lastModified -eq $updated.lastModified) 'Edit survives API process restart and migration startup'
    $ordinaryAfter = Invoke-RestMethod "$base/api/movies/1001"
    Assert-Check ($ordinaryAfter.title -eq $ordinary.title -and $ordinaryAfter.lastModified -eq $ordinary.lastModified) 'Ordinary seed record unchanged'
    $evidence.status = 'PASS'
} catch { $evidence.status = 'FAIL'; $evidence.error = $_.Exception.Message; throw }
finally {
    Stop-TestApi
    foreach ($name in $saved.Keys) { [Environment]::SetEnvironmentVariable($name,$saved[$name],'Process') }
    $evidence.processStopped = $true
    $evidencePath = Join-Path $evidenceDirectory 'verification.json'
    [System.IO.File]::WriteAllText($evidencePath,($evidence | ConvertTo-Json -Depth 5),$utf8)
    Write-Output "Evidence: $evidencePath"
}
