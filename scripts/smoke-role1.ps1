[CmdletBinding()]
param(
    [string]$ApiBaseUrl = "http://localhost:7000",
    [string]$AdminEmail = "admin@cloneebay.local",
    [SecureString]$AdminPassword
)

$ErrorActionPreference = "Stop"
$baseUrl = $ApiBaseUrl.TrimEnd("/")
if ($null -eq $AdminPassword)
{
    $AdminPassword = Read-Host "Local admin password" -AsSecureString
}

$adminPasswordPlain = [System.Net.NetworkCredential]::new("", $AdminPassword).Password
$suffix = [Guid]::NewGuid().ToString("N").Substring(0, 10)
$testEmail = "role1-smoke-$suffix@cloneebay.local"
$testPassword = "Role1Smoke!$suffix"
$testUsername = "role1-$suffix"
$storeName = "Role 1 Smoke $suffix"
$description = "Role 1 partial-update smoke test $suffix"

function Invoke-Role1Api
{
    param(
        [ValidateSet("GET", "POST", "PUT")]
        [string]$Method,
        [string]$Path,
        [object]$Body,
        [string]$AccessToken
    )

    $parameters = @{
        Uri = "$baseUrl/$($Path.TrimStart('/'))"
        Method = $Method
        Headers = @{ "X-Correlation-Id" = "role1-smoke-$suffix" }
    }
    if (-not [string]::IsNullOrWhiteSpace($AccessToken))
    {
        $parameters.Headers.Authorization = "Bearer $AccessToken"
    }
    if ($null -ne $Body)
    {
        $parameters.ContentType = "application/json"
        $parameters.Body = $Body | ConvertTo-Json -Depth 8
    }

    Invoke-RestMethod @parameters
}

$registered = Invoke-Role1Api POST "/api/auth/register" @{
    username = $testUsername
    email = $testEmail
    password = $testPassword
}
if (-not $registered.success -or
    $registered.data.role -ne "Buyer" -or
    $registered.meta.traceId -ne "role1-smoke-$suffix")
{
    throw "Register/response-envelope acceptance check failed."
}

$buyerLogin = Invoke-Role1Api POST "/api/auth/login" @{
    email = $testEmail
    password = $testPassword
}
if (-not $buyerLogin.success -or $buyerLogin.data.user.role -ne "Buyer")
{
    throw "Buyer login acceptance check failed."
}

$application = Invoke-Role1Api POST "/api/seller/apply" @{
    sellerType = "Individual"
    legalName = "Role One Smoke Tester"
    phone = "0901234567"
    storeName = $storeName
} $buyerLogin.data.accessToken
if (-not $application.success -or $application.data.verificationStatus -ne "Pending")
{
    throw "Seller Apply acceptance check failed."
}

$verification = Invoke-Role1Api GET "/api/seller/verification-status" $null $buyerLogin.data.accessToken
if ($verification.data.verificationStatus -ne "Pending")
{
    throw "Verification status acceptance check failed."
}

$adminLogin = Invoke-Role1Api POST "/api/auth/login" @{
    email = $AdminEmail
    password = $adminPasswordPlain
}
if (-not $adminLogin.success -or $adminLogin.data.user.role -ne "Admin")
{
    throw "Admin login acceptance check failed."
}

$approved = Invoke-Role1Api PUT "/api/admin/seller/$($application.data.storeId)/approve" $null $adminLogin.data.accessToken
if ($approved.data.verificationStatus -ne "Approved")
{
    throw "Seller approval acceptance check failed."
}

$sellerSession = Invoke-Role1Api POST "/api/auth/refresh-token" @{
    refreshToken = $buyerLogin.data.refreshToken
}
if ($sellerSession.data.user.role -ne "Seller" -or -not $sellerSession.data.user.verified)
{
    throw "JWT Seller/verified claim acceptance check failed."
}

$updatedStore = Invoke-Role1Api PUT "/api/store" @{
    description = $description
} $sellerSession.data.accessToken
if ($updatedStore.data.storeName -ne $storeName -or $updatedStore.data.description -ne $description)
{
    throw "Partial Store Profile update acceptance check failed."
}

$publicStore = Invoke-Role1Api GET "/api/store/$($registered.data.id)"
if ($publicStore.data.description -ne $description -or $publicStore.data.verificationStatus -ne "Approved")
{
    throw "Public Store Profile acceptance check failed."
}

Invoke-Role1Api POST "/api/auth/logout" @{
    refreshToken = $sellerSession.data.refreshToken
} $sellerSession.data.accessToken | Out-Null

[pscustomobject]@{
    Result = "PASS"
    TestUser = $testEmail
    StoreId = $application.data.storeId
    CorrelationId = "role1-smoke-$suffix"
    Checks = "register, login, apply, status, approve, refresh, partial store update, public store, logout"
}
