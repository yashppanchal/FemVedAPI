param(
    [string]$BaseUrl = "http://localhost:5064/api/v1",
    [string]$AdminToken = "",
    [string]$UserToken = "",
    [string]$ExpertToken = "",
    [string]$RefreshToken = "",
    [string]$DomainId = "",
    [string]$CategoryId = "",
    [string]$ProgramId = "",
    [string]$OrderId = "",
    [string]$AccessId = "",
    [string]$UserId = "",
    [string]$ExpertId = "",
    [string]$CouponId = "",
    [string]$GdprRequestId = ""
)

$ErrorActionPreference = "Stop"

function Invoke-JsonApi {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        [string]$Token,
        [hashtable]$Body,
        [string[]]$RequiredKeys
    )

    Write-Host "\n=== $Name ===" -ForegroundColor Cyan
    Write-Host "$Method $Url"

    $headers = @{ "Content-Type" = "application/json" }
    if (-not [string]::IsNullOrWhiteSpace($Token)) {
        $headers["Authorization"] = "Bearer $Token"
    }

    $payload = $null
    if ($null -ne $Body) {
        $payload = ($Body | ConvertTo-Json -Depth 8)
    }

    $response = Invoke-RestMethod -Method $Method -Uri $Url -Headers $headers -Body $payload
    $json = $response | ConvertTo-Json -Depth 8
    Write-Host "Response: $json"

    foreach ($k in $RequiredKeys) {
        if (-not ($response.PSObject.Properties.Name -contains $k)) {
            throw "[$Name] Missing expected key '$k'."
        }
    }

    Write-Host "PASS: $Name" -ForegroundColor Green
}

function Run-IfProvided {
    param(
        [string]$Name,
        [scriptblock]$Condition,
        [scriptblock]$Action
    )

    if (& $Condition) {
        & $Action
    }
    else {
        Write-Host "SKIP: $Name (missing required inputs)" -ForegroundColor Yellow
    }
}

Write-Host "Starting mutation confirmation smoke tests..." -ForegroundColor Magenta

Run-IfProvided "Guided - Update Domain" \
    { $AdminToken -and $DomainId } \
    {
        Invoke-JsonApi -Name "Update Domain" -Method "PUT" -Url "$BaseUrl/guided/domains/$DomainId" -Token $AdminToken -Body @{ sortOrder = 9 } -RequiredKeys @("id", "isUpdated")
    }

Run-IfProvided "Guided - Update Category" \
    { $AdminToken -and $CategoryId } \
    {
        Invoke-JsonApi -Name "Update Category" -Method "PUT" -Url "$BaseUrl/guided/categories/$CategoryId" -Token $AdminToken -Body @{ heroTitle = "Updated Hero" } -RequiredKeys @("id", "isUpdated")
    }

Run-IfProvided "Guided - Update Program" \
    { $AdminToken -and $ProgramId } \
    {
        Invoke-JsonApi -Name "Update Program" -Method "PUT" -Url "$BaseUrl/guided/programs/$ProgramId" -Token $AdminToken -Body @{ sortOrder = 3 } -RequiredKeys @("id", "isUpdated")
    }

Run-IfProvided "Guided - Submit Program" \
    { ($AdminToken -or $ExpertToken) -and $ProgramId } \
    {
        $token = if ($ExpertToken) { $ExpertToken } else { $AdminToken }
        Invoke-JsonApi -Name "Submit Program" -Method "POST" -Url "$BaseUrl/guided/programs/$ProgramId/submit" -Token $token -Body $null -RequiredKeys @("id", "status", "isUpdated")
    }

Run-IfProvided "Guided - Publish Program" \
    { $AdminToken -and $ProgramId } \
    {
        Invoke-JsonApi -Name "Publish Program" -Method "POST" -Url "$BaseUrl/guided/programs/$ProgramId/publish" -Token $AdminToken -Body $null -RequiredKeys @("id", "status", "isUpdated")
    }

Run-IfProvided "Guided - Archive Program" \
    { $AdminToken -and $ProgramId } \
    {
        Invoke-JsonApi -Name "Archive Program" -Method "POST" -Url "$BaseUrl/guided/programs/$ProgramId/archive" -Token $AdminToken -Body $null -RequiredKeys @("id", "status", "isUpdated")
    }

Run-IfProvided "Orders - Refund" \
    { $AdminToken -and $OrderId } \
    {
        Invoke-JsonApi -Name "Refund" -Method "POST" -Url "$BaseUrl/orders/$OrderId/refund" -Token $AdminToken -Body @{ refundAmount = 1; reason = "Smoke test" } -RequiredKeys @("orderId", "action", "isUpdated")
    }

Run-IfProvided "Experts - Progress Update" \
    { $ExpertToken -and $AccessId } \
    {
        Invoke-JsonApi -Name "Progress Update" -Method "POST" -Url "$BaseUrl/experts/me/enrollments/$AccessId/progress-update" -Token $ExpertToken -Body @{ updateNote = "Weekly progress update"; sendEmail = $false } -RequiredKeys @("accessId", "isUpdated", "emailQueued")
    }

Run-IfProvided "Admin - Activate User" \
    { $AdminToken -and $UserId } \
    {
        Invoke-JsonApi -Name "Activate User" -Method "PUT" -Url "$BaseUrl/admin/users/$UserId/activate" -Token $AdminToken -Body $null -RequiredKeys @("id", "isActive", "isUpdated")
    }

Run-IfProvided "Admin - Deactivate User" \
    { $AdminToken -and $UserId } \
    {
        Invoke-JsonApi -Name "Deactivate User" -Method "PUT" -Url "$BaseUrl/admin/users/$UserId/deactivate" -Token $AdminToken -Body $null -RequiredKeys @("id", "isActive", "isUpdated")
    }

Run-IfProvided "Admin - Activate Expert" \
    { $AdminToken -and $ExpertId } \
    {
        Invoke-JsonApi -Name "Activate Expert" -Method "PUT" -Url "$BaseUrl/admin/experts/$ExpertId/activate" -Token $AdminToken -Body $null -RequiredKeys @("id", "isActive", "isUpdated")
    }

Run-IfProvided "Admin - Deactivate Expert" \
    { $AdminToken -and $ExpertId } \
    {
        Invoke-JsonApi -Name "Deactivate Expert" -Method "PUT" -Url "$BaseUrl/admin/experts/$ExpertId/deactivate" -Token $AdminToken -Body $null -RequiredKeys @("id", "isActive", "isUpdated")
    }

Run-IfProvided "Admin - Update Expert" \
    { $AdminToken -and $ExpertId } \
    {
        Invoke-JsonApi -Name "Update Expert" -Method "PUT" -Url "$BaseUrl/admin/experts/$ExpertId" -Token $AdminToken -Body @{ title = "Updated Title" } -RequiredKeys @("id", "isUpdated")
    }

Run-IfProvided "Admin - Change Role" \
    { $AdminToken -and $UserId } \
    {
        Invoke-JsonApi -Name "Change Role" -Method "PUT" -Url "$BaseUrl/admin/user/change-role" -Token $AdminToken -Body @{ userId = $UserId; roleId = 3 } -RequiredKeys @("userId", "roleId", "isUpdated")
    }

Run-IfProvided "Admin - Deactivate Coupon" \
    { $AdminToken -and $CouponId } \
    {
        Invoke-JsonApi -Name "Deactivate Coupon" -Method "PUT" -Url "$BaseUrl/admin/coupons/$CouponId/deactivate" -Token $AdminToken -Body $null -RequiredKeys @("id", "isActive", "isUpdated")
    }

Run-IfProvided "Admin - Process GDPR" \
    { $AdminToken -and $GdprRequestId } \
    {
        Invoke-JsonApi -Name "Process GDPR" -Method "POST" -Url "$BaseUrl/admin/gdpr-requests/$GdprRequestId/process" -Token $AdminToken -Body @{ action = "Reject"; rejectionReason = "Smoke test" } -RequiredKeys @("requestId", "action", "isUpdated")
    }

Run-IfProvided "Auth - Logout" \
    { $UserToken -and $RefreshToken } \
    {
        Invoke-JsonApi -Name "Logout" -Method "POST" -Url "$BaseUrl/auth/logout" -Token $UserToken -Body @{ refreshToken = $RefreshToken } -RequiredKeys @("userId", "isLoggedOut")
    }

Write-Host "\nCompleted mutation confirmation smoke tests." -ForegroundColor Magenta
