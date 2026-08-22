[CmdletBinding()]
param(
    [string]$ApiBaseUrl = "http://localhost:7000",
    [string]$SellerEmail = "seller.approved@cloneebay.local",
    [SecureString]$SellerPassword,
    [int]$WaitSeconds = 75
)

$ErrorActionPreference = "Stop"
$baseUrl = $ApiBaseUrl.TrimEnd("/")
if ($null -eq $SellerPassword) { $SellerPassword = Read-Host "Approved demo Seller password" -AsSecureString }
$sellerPasswordPlain = [System.Net.NetworkCredential]::new("", $SellerPassword).Password
$suffix = [Guid]::NewGuid().ToString("N").Substring(0, 10)
$traceId = "role2-auction-$suffix"
$bidderEmail = "auction-bidder-$suffix@cloneebay.local"
$bidderPassword = "AuctionBid!$suffix"
$imagePath = Join-Path ([System.IO.Path]::GetTempPath()) "cloneebay-auction-$suffix.png"

function Invoke-AuctionApi
{
    param([string]$Method, [string]$Path, [object]$Body, [string]$Token)
    $headers = @{ "X-Correlation-Id" = $traceId }
    if ($Token) { $headers.Authorization = "Bearer $Token" }
    $parameters = @{ Uri = "$baseUrl/$($Path.TrimStart('/'))"; Method = $Method; Headers = $headers }
    if ($null -ne $Body)
    {
        $parameters.ContentType = "application/json"
        $parameters.Body = $Body | ConvertTo-Json -Depth 8
    }
    Invoke-RestMethod @parameters
}

try
{
    [System.IO.File]::WriteAllBytes(
        $imagePath,
        [Convert]::FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="))
    $seller = Invoke-AuctionApi POST "/api/auth/login" @{ email = $SellerEmail; password = $sellerPasswordPlain }
    $categories = Invoke-AuctionApi GET "/api/categories"
    $endTime = (Get-Date).ToUniversalTime().AddSeconds([Math]::Max(65, $WaitSeconds - 5)).ToString("O")
    $auction = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method POST -Headers @{
        Authorization = "Bearer $($seller.data.accessToken)"
        "X-Correlation-Id" = $traceId
    } -Form @{
        Title = "Auction Smoke $suffix"
        Description = "Automated auction close and order idempotency acceptance test."
        Price = "100000"
        CategoryId = "$($categories.data[0].id)"
        IsAuction = "true"
        AuctionEndTime = $endTime
        Quantity = "1"
        Images = Get-Item -LiteralPath $imagePath
    }

    $registeredBidder = Invoke-AuctionApi POST "/api/auth/register" @{
        username = "bidder-$suffix"
        email = $bidderEmail
        password = $bidderPassword
    }
    $bidder = Invoke-AuctionApi POST "/api/auth/login" @{ email = $bidderEmail; password = $bidderPassword }
    $bid = Invoke-AuctionApi POST "/api/products/$($auction.data.id)/bids" @{ amount = 125000 } $bidder.data.accessToken
    if ($bid.data.amount -ne 125000) { throw "Bid placement failed." }

    Start-Sleep -Seconds $WaitSeconds
    $closed = Invoke-AuctionApi POST "/api/products/$($auction.data.id)/close-auction" @{} $seller.data.accessToken
    if ($closed.data.winnerId -ne $registeredBidder.data.id -or $null -eq $closed.data.orderId -or $closed.data.orderCreationPending)
    {
        throw "Auction close/order creation acceptance check failed."
    }
    $closedAgain = Invoke-AuctionApi POST "/api/products/$($auction.data.id)/close-auction" @{} $seller.data.accessToken
    if ($closedAgain.data.orderId -ne $closed.data.orderId) { throw "Auction close is not idempotent." }

    [pscustomobject]@{
        Result = "PASS"
        ProductId = $auction.data.id
        WinnerId = $closed.data.winnerId
        OrderId = $closed.data.orderId
        CorrelationId = $traceId
        Checks = "auction create, buyer bid, automatic/manual close, OrderTable+OrderItem creation, idempotent repeat close"
    }
}
finally
{
    if (Test-Path -LiteralPath $imagePath) { Remove-Item -LiteralPath $imagePath -Force }
}
