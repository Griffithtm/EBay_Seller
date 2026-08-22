param(
    [string]$SqlServer = "GRIFFITHTM",
    [string]$Database = "CloneEbayDB",
    [string]$SqlUser = "sa"
)

$apiProject = Join-Path $PSScriptRoot "..\CloneEbay.API\CloneEbay.API.csproj"
$sqlPasswordSecure = Read-Host "SQL Server password" -AsSecureString
$adminPasswordSecure = Read-Host "Local admin password" -AsSecureString
$demoSellerPasswordSecure = Read-Host "Password for the three local demo sellers" -AsSecureString
$sqlPassword = [System.Net.NetworkCredential]::new("", $sqlPasswordSecure).Password
$adminPassword = [System.Net.NetworkCredential]::new("", $adminPasswordSecure).Password
$demoSellerPassword = [System.Net.NetworkCredential]::new("", $demoSellerPasswordSecure).Password
$jwtKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$connectionString = "Server=$SqlServer;Database=$Database;User Id=$SqlUser;Password=$sqlPassword;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=False"

dotnet user-secrets set "ConnectionStrings:DefaultConnection" $connectionString --project $apiProject
dotnet user-secrets set "Jwt:Key" $jwtKey --project $apiProject
dotnet user-secrets set "Seed:AdminPassword" $adminPassword --project $apiProject
dotnet user-secrets set "Seed:ResetAdminPassword" "true" --project $apiProject
dotnet user-secrets set "Seed:DemoSellerPassword" $demoSellerPassword --project $apiProject
dotnet user-secrets set "Seed:ResetDemoSellerData" "true" --project $apiProject

Write-Host "Local secrets configured for $SqlServer/$Database. The next API startup will seed the admin and three demo seller states."
Write-Host "No password was written to the repository."
