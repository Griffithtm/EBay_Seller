[CmdletBinding()]
param(
    [string]$ApiBaseUrl = "http://localhost:7000",
    [string]$SellerEmail = "seller.approved@cloneebay.local",
    [SecureString]$SellerPassword
)

$ErrorActionPreference = "Stop"
$baseUrl = $ApiBaseUrl.TrimEnd("/")

if ($null -eq $SellerPassword)
{
    $SellerPassword = Read-Host "Seller password (or demo password)" -AsSecureString
}

$sellerPasswordPlain = [System.Net.NetworkCredential]::new("", $SellerPassword).Password
$traceId = "role6-smoke-" + [Guid]::NewGuid().ToString("N").Substring(0, 8)

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "  Role 6 - Dashboard & Performance Acceptance Smoke Test   " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Login
Write-Host "`n[1/5] Logging in as verified seller ($SellerEmail)..." -ForegroundColor Yellow
$loginParams = @{
    Uri = "$baseUrl/api/auth/login"
    Method = "POST"
    ContentType = "application/json"
    Headers = @{ "X-Correlation-Id" = $traceId }
    Body = @{
        email = $SellerEmail
        password = $sellerPasswordPlain
    } | ConvertTo-Json
}

$loginRes = Invoke-RestMethod @loginParams
if (-not $loginRes.success -or -not $loginRes.data.accessToken)
{
    throw "Failed to login as seller: $($loginRes.error.message)"
}
$token = $loginRes.data.accessToken
Write-Host "  -> Login OK. Token acquired." -ForegroundColor Green

function Test-Endpoint
{
    param(
        [string]$Name,
        [string]$Path
    )

    Write-Host "`n--> Testing $Name ($Path)..." -ForegroundColor Yellow
    $headers = @{
        "Authorization" = "Bearer $token"
        "X-Correlation-Id" = $traceId
    }

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $res = Invoke-RestMethod -Uri "$baseUrl/$Path" -Method "GET" -Headers $headers
    $sw.Stop()
    $elapsedMs = $sw.ElapsedMilliseconds

    if (-not $res.success)
    {
        throw "API $Path returned failure envelope: $($res.error.message)"
    }

    Write-Host "  -> HTTP 200 OK | Response time: ${elapsedMs}ms | Envelope: Success=$($res.success)" -ForegroundColor Green

    if ($elapsedMs -gt 1000)
    {
        Write-Warning "  -> [WARNING] Response time ($elapsedMs ms) exceeded 1,000ms SLA!"
    }
    else
    {
        Write-Host "  -> [PASS] Meets performance SLA (< 1,000ms)." -ForegroundColor Cyan
    }

    return $res.data
}

# 2. Test Summary
$summary = Test-Endpoint "Dashboard Summary (Week)" "api/dashboard/summary?period=week"
Write-Host "     Revenue: $($summary.totalRevenue) | Orders: $($summary.totalOrders) | AOV: $($summary.averageOrderValue)" -ForegroundColor DarkGray

# 3. Test Top Products
$topProducts = Test-Endpoint "Top Products (Week)" "api/dashboard/top-products?period=week&limit=10"
Write-Host "     Top Products Count: $($topProducts.Count)" -ForegroundColor DarkGray

# 4. Test Revenue Chart
$chart = Test-Endpoint "Revenue Chart (Week)" "api/dashboard/revenue-chart?period=week"
Write-Host "     Chart Points Count: $($chart.points.Count) (Must be 7)" -ForegroundColor DarkGray
if ($chart.points.Count -ne 7)
{
    throw "Weekly chart did not return 7 day points!"
}

# 5. Test Performance
$perf = Test-Endpoint "Seller Performance (Week)" "api/dashboard/performance?period=week"
Write-Host "     Seller Level: $($perf.sellerLevel) | Dispute Rate: $($perf.disputeRate)%" -ForegroundColor DarkGray

# 6. Test Monthly
$monthSummary = Test-Endpoint "Dashboard Summary (Month)" "api/dashboard/summary?period=month"

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "  ROLE 6 DASHBOARD SMOKE TEST COMPLETED SUCCESSFULLY!    " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green