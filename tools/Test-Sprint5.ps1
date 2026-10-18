# Run with PowerShell 7. Temporary account is removed in finally.
$ErrorActionPreference = 'Stop'
$baseUrl = 'http://localhost:54834'
$testEmail = 'sprint5-' + [guid]::NewGuid().ToString('N') + '@example.test'
$testPassword = 'Aa9!' + [guid]::NewGuid().ToString('N')
$results = [System.Collections.Generic.List[string]]::new()
function Assert-Test($Condition, $Label) { if (!$Condition) { throw ('FAIL: ' + $Label) }; $results.Add('PASS: ' + $Label) }
function Get-Page($Path, $Session) { Invoke-WebRequest ($baseUrl + $Path) -WebSession $Session -SkipHttpErrorCheck }
function Get-Token($Page) { [System.Net.WebUtility]::HtmlDecode([regex]::Match($Page.Content, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value) }
function Post-Form($Path, $Session, $Data) { Invoke-WebRequest ($baseUrl + $Path) -Method Post -WebSession $Session -Body $Data -SkipHttpErrorCheck }
try {
 $session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
 $page = Get-Page '/StoreManager' $session
 Assert-Test ($page.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') 'Anonymous administration redirects to login'
 $page = Get-Page '/Account/Register' $session
 $page = Post-Form '/Account/Register' $session @{__RequestVerificationToken=(Get-Token $page);Email=$testEmail;Password='short';ConfirmPassword='different'}
 Assert-Test ($page.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Register') 'Invalid registration stays on form'
 $page = Post-Form '/Account/Register' $session @{__RequestVerificationToken=(Get-Token $page);Email=$testEmail;Password=$testPassword;ConfirmPassword=$testPassword}
 Assert-Test ($page.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') 'Registration succeeds'
 $duplicateSession = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
 $duplicatePage = Get-Page '/Account/Register' $duplicateSession
 $duplicatePage = Post-Form '/Account/Register' $duplicateSession @{__RequestVerificationToken=(Get-Token $duplicatePage);Email=$testEmail;Password=$testPassword;ConfirmPassword=$testPassword}
 Assert-Test ($duplicatePage.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Register') 'Duplicate email rejected'
 $page = Post-Form '/Account/Login' $session @{__RequestVerificationToken=(Get-Token $page);Email=$testEmail;Password='Wrong-password9!'}
 Assert-Test ($page.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') 'Wrong password rejected'
 $page = Post-Form '/Account/Login?returnUrl=https://example.com' $session @{__RequestVerificationToken=(Get-Token $page);Email=$testEmail;Password=$testPassword}
 Assert-Test ($page.BaseResponse.RequestMessage.RequestUri.Host -eq 'localhost' -and $page.Content.Contains($testEmail)) 'Login succeeds and external return URL rejected'
 $page = Get-Page '/StoreManager' $session
 Assert-Test ([int]$page.StatusCode -eq 403) 'Normal user denied administration'
 $page = Get-Page '/StoreManager/Create' $session
 Assert-Test ([int]$page.StatusCode -eq 403) 'Normal user denied create URL'
 $page = Get-Page '/' $session
 $page = Post-Form '/Account/LogOff' $session @{__RequestVerificationToken=(Get-Token $page)}
 Assert-Test (!$page.Content.Contains($testEmail)) 'Logout clears identity'
 $page = Get-Page '/StoreManager' $session
 Assert-Test ($page.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') 'Administration protected after logout'
 & (Join-Path $PSScriptRoot '../MusicStore/bin/AccountsAdmin.exe') promote $testEmail | Out-Null
 if ($LASTEXITCODE -ne 0) { throw 'Promotion failed' }
 $page = Get-Page '/Account/Login' $session
 $page = Post-Form '/Account/Login' $session @{__RequestVerificationToken=(Get-Token $page);Email=$testEmail;Password=$testPassword}
 $page = Get-Page '/StoreManager' $session
 Assert-Test ([int]$page.StatusCode -eq 200 -and $page.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/StoreManager') 'Administrator reaches catalog management'
 $page = Get-Page '/StoreManager/Create' $session
 Assert-Test ([int]$page.StatusCode -eq 200) 'Administrator reaches create form'
 $csrfPage = Post-Form '/Account/LogOff' $session @{}
 Assert-Test ([int]$csrfPage.StatusCode -ge 400) 'Logout without antiforgery token rejected'
 $page = Get-Page '/' $session
 Assert-Test ($page.Content.Contains($testEmail)) 'Rejected logout preserves session'
 $page = Post-Form '/Account/LogOff' $session @{__RequestVerificationToken=(Get-Token $page)}
 foreach ($attempt in 1..5) {
  $page = Get-Page '/Account/Login' $session
  $page = Post-Form '/Account/Login' $session @{__RequestVerificationToken=(Get-Token $page);Email=$testEmail;Password='Wrong-password9!'}
 }
 $page = Get-Page '/Account/Login' $session
 $page = Post-Form '/Account/Login' $session @{__RequestVerificationToken=(Get-Token $page);Email=$testEmail;Password=$testPassword}
 Assert-Test ($page.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') 'Five failed attempts temporarily lock account'
 $results
} finally {
 $sqlConnection = [System.Data.SqlClient.SqlConnection]::new('Data Source=.\SQLEXPRESS;Initial Catalog=MusicStoreAccounts;Integrated Security=True;')
 try {
  $sqlConnection.Open()
  $cleanup = $sqlConnection.CreateCommand()
  $cleanup.CommandText = 'DELETE ur FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetUsers u ON ur.UserId=u.Id WHERE u.Email=@email; DELETE FROM dbo.AspNetUsers WHERE Email=@email;'
  $cleanup.Parameters.AddWithValue('@email',$testEmail) | Out-Null
  $cleanup.ExecuteNonQuery() | Out-Null
 } finally { $sqlConnection.Dispose() }
}
