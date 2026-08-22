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
    $SellerPassword = Read-Host "Approved demo Seller password" -AsSecureString
}
$sellerPasswordPlain = [System.Net.NetworkCredential]::new("", $SellerPassword).Password
$suffix = [Guid]::NewGuid().ToString("N").Substring(0, 10)
$traceId = "role2-smoke-$suffix"
$imagePath = Join-Path ([System.IO.Path]::GetTempPath()) "cloneebay-$suffix.png"

function Invoke-Role2Json
{
    param(
        [ValidateSet("GET", "POST", "PUT", "PATCH", "DELETE")]
        [string]$Method,
        [string]$Path,
        [object]$Body,
        [string]$AccessToken
    )

    $headers = @{ "X-Correlation-Id" = $traceId }
    if (-not [string]::IsNullOrWhiteSpace($AccessToken)) { $headers.Authorization = "Bearer $AccessToken" }
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

    $login = Invoke-Role2Json POST "/api/auth/login" @{ email = $SellerEmail; password = $sellerPasswordPlain }
    if (-not $login.success -or -not $login.data.user.verified) { throw "Verified Seller login failed." }
    $token = $login.data.accessToken
    $categories = Invoke-Role2Json GET "/api/categories"
    $categoryId = $categories.data[0].id

    $created = Invoke-RestMethod -Uri "$baseUrl/api/products" -Method POST -Headers @{
        Authorization = "Bearer $token"
        "X-Correlation-Id" = $traceId
    } -Form @{
        Title = "Role 2 Smoke $suffix"
        Description = "Role 2 automated product workflow acceptance test."
        Price = "125000"
        CategoryId = "$categoryId"
        IsAuction = "false"
        Quantity = "3"
        Images = Get-Item -LiteralPath $imagePath
    }
    if (-not $created.success -or $created.data.inventory.quantity -ne 3) { throw "Product create failed." }
    $productId = $created.data.id

    $detail = Invoke-Role2Json GET "/api/products/$productId"
    if ($detail.data.title -ne "Role 2 Smoke $suffix") { throw "Product detail failed." }

    $updated = Invoke-RestMethod -Uri "$baseUrl/api/products/$productId" -Method PUT -Headers @{
        Authorization = "Bearer $token"
        "X-Correlation-Id" = $traceId
    } -Form @{
        Title = "Role 2 Smoke Updated $suffix"
        Description = "Role 2 automated product workflow acceptance test updated."
        Price = "150000"
        CategoryId = "$categoryId"
        IsAuction = "false"
        Quantity = "7"
    }
    if ($updated.data.inventory.quantity -ne 7 -or $updated.data.price -ne 150000) { throw "Atomic product/inventory update failed." }

    $hidden = Invoke-Role2Json PATCH "/api/products/$productId/hide" $null $token
    if ($hidden.data.status -ne "Hidden") { throw "Product hide failed." }
    $visible = Invoke-Role2Json PATCH "/api/products/$productId/hide" $null $token
    if ($visible.data.status -ne "Active") { throw "Product restore failed." }

    Invoke-Role2Json DELETE "/api/products/$productId" $null $token | Out-Null
    Invoke-Role2Json POST "/api/auth/logout" @{ refreshToken = $login.data.refreshToken } $token | Out-Null

    [pscustomobject]@{
        Result = "PASS"
        Seller = $SellerEmail
        ProductId = $productId
        CorrelationId = $traceId
        Checks = "login, categories, multipart create, detail, atomic update+inventory, hide, restore, delete, logout"
    }
}
finally
{
    if (Test-Path -LiteralPath $imagePath) { Remove-Item -LiteralPath $imagePath -Force }
}
