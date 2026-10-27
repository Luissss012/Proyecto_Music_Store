# PowerShell 7. Uses only temporary users/catalog entries; removes them in finally.
$ErrorActionPreference = 'Stop'
$baseUrl = 'http://localhost:54834'
$tag = 'admin-test-' + [guid]::NewGuid().ToString('N')
$emailA = $tag + '-a@example.test'
$emailB = $tag + '-b@example.test'
$password = 'Aa9!' + [guid]::NewGuid().ToString('N')
$results = [System.Collections.Generic.List[string]]::new()
function Assert-Test($Condition, $Label) { if (!$Condition) { throw ('FAIL: ' + $Label) }; $results.Add('PASS: ' + $Label); Write-Host ('PASS: ' + $Label) }
function Page($Path, $Session) { Invoke-WebRequest ($baseUrl + $Path) -WebSession $Session -SkipHttpErrorCheck }
function Post($Path, $Session, $Data) { try { Invoke-WebRequest ($baseUrl + $Path) -Method Post -WebSession $Session -Body $Data -SkipHttpErrorCheck } catch { throw ('POST ' + $Path + ': ' + $_.Exception.Message) } }
function Field($Html, $Name) {
 $input = [regex]::Match($Html, '<input\b[^>]*\bname="' + [regex]::Escape($Name) + '"[^>]*>')
 return [System.Net.WebUtility]::HtmlDecode([regex]::Match($input.Value, '\bvalue="([^"]*)"').Groups[1].Value)
}
function Token($Response) { Field $Response.Content '__RequestVerificationToken' }
function Text($Response) { [System.Net.WebUtility]::HtmlDecode($Response.Content) }
function Query($Database, $Sql, $Values = @{}) {
 $connection = [System.Data.SqlClient.SqlConnection]::new('Data Source=.\SQLEXPRESS;Initial Catalog=' + $Database + ';Integrated Security=True;')
 try { $connection.Open(); $command = $connection.CreateCommand(); $command.CommandText = $Sql
  foreach ($key in $Values.Keys) { $command.Parameters.AddWithValue('@' + $key,$Values[$key]) | Out-Null }
  return $command.ExecuteScalar()
 } finally { $connection.Dispose() }
}
function Register($Email, $Session) {
 $response = Page '/Account/Register' $Session
 $response = Post '/Account/Register' $Session @{__RequestVerificationToken=(Token $response);Email=$Email;Password=$password;ConfirmPassword=$password;MakeAdministrator='true';Role='Administrator'}
 Assert-Test ($response.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') 'Temporary registration succeeds'
}
function Login($Email, $Session) {
 $response = Page '/Account/Login' $Session
 $response = Post '/Account/Login' $Session @{__RequestVerificationToken=(Token $response);Email=$Email;Password=$password}
 Assert-Test ($response.Content.Contains($Email)) 'Temporary user login succeeds'
}
function RoleForm($Target, $Session) { Page ('/Users/ChangeRole/' + $Target) $Session }
function RolePost($Form, $Session, $Pass) {
 Post '/Users/ChangeRole' $Session @{__RequestVerificationToken=(Token $Form);Id=(Field $Form.Content 'Id');Stamp=(Field $Form.Content 'Stamp');MakeAdministrator=(Field $Form.Content 'MakeAdministrator');Password=$Pass}
}
$a = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$b = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$anon = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$beforeCatalog = Query MusicStore "SELECT (SELECT AlbumId,Title,ArtistId,GenreId,Price,AlbumArtUrl FROM dbo.Albums ORDER BY AlbumId FOR JSON PATH)"
try {
 foreach ($route in @('/Admin','/Artists','/Genres','/Users','/Users/Audit','/Artists/Create','/Genres/Create')) {
  $response = Page $route $anon
  Assert-Test ($response.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') ('Anonymous denied ' + $route)
 }
 Register $emailA $a; Register $emailB $b
 $idA = Query MusicStoreAccounts 'SELECT Id FROM dbo.AspNetUsers WHERE Email=@email' @{email=$emailA}
 $idB = Query MusicStoreAccounts 'SELECT Id FROM dbo.AspNetUsers WHERE Email=@email' @{email=$emailB}
 $adminRoles = Query MusicStoreAccounts "SELECT COUNT(*) FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON r.Id=ur.RoleId WHERE r.Name='Administrator' AND ur.UserId IN (@a,@b)" @{a=$idA;b=$idB}
 Assert-Test ($adminRoles -eq 0) 'Registration cannot grant administrator through extra fields'
 Login $emailB $b
 foreach ($route in @('/Admin','/Artists','/Genres','/Users','/Users/Audit','/Artists/Create','/Genres/Create')) {
  $response = Page $route $b
  Assert-Test ([int]$response.StatusCode -eq 403) ('Normal user denied ' + $route)
 }
 $response = Page '/' $b
 $response = Post '/Artists/Create' $b @{__RequestVerificationToken=(Token $response);Name=$tag}
 Assert-Test ([int]$response.StatusCode -eq 403) 'Normal user cannot POST a new artist'
 & (Join-Path $PSScriptRoot 'AccountsAdmin.ps1') -Action promote -Email $emailA | Out-Null
 Login $emailA $a
 foreach ($route in @('/Users','/Users/Audit',('/Users/ChangeRole/'+$idB))) {
  $response = Page $route $a
  Assert-Test ([int]$response.StatusCode -eq 403) ('Ordinary administrator denied ' + $route)
 }
 # Test-only owner membership; never alter the real owner's roles.
 Query MusicStoreAccounts "INSERT dbo.AspNetUserRoles(UserId,RoleId) SELECT @id,Id FROM dbo.AspNetRoles WHERE Name='Owner'" @{id=$idA} | Out-Null
 Login $emailA $a
 foreach ($route in @('/Admin','/Artists','/Genres','/Users','/Users/Audit')) {
  $response = Page $route $a
  Assert-Test ([int]$response.StatusCode -eq 200 -and $response.BaseResponse.RequestMessage.RequestUri.AbsolutePath -ne '/Account/Login') ('Administrator reaches ' + $route)
 }
 $response = Post '/Artists/Create' $a @{Name=$tag}
 Assert-Test ([int]$response.StatusCode -eq 400) 'Missing antiforgery token rejected'
 foreach ($controller in @('Artists','Genres')) {
  $response = Page ('/' + $controller + '/Create') $a
  $response = Post ('/' + $controller + '/Create') $a @{__RequestVerificationToken=(Token $response);Name='   '}
  Assert-Test ($response.Content.Contains('validation-summary-errors')) ($controller + ': blank name rejected')
  $response = Page ('/' + $controller + '/Create') $a
  $response = Post ('/' + $controller + '/Create') $a @{__RequestVerificationToken=(Token $response);Name=('x' * 161)}
  Assert-Test ($response.Content.Contains('validation-summary-errors')) ($controller + ': long name rejected')
  $response = Page ('/' + $controller + '/Create') $a
  $response = Post ('/' + $controller + '/Create') $a @{__RequestVerificationToken=(Token $response);Name='<script>alert(1)</script>'}
  Assert-Test ([int]$response.StatusCode -eq 400) ($controller + ': HTML payload rejected without stack trace')
  $response = Page ('/' + $controller + '/Create') $a
  $response = Post ('/' + $controller + '/Create') $a @{__RequestVerificationToken=(Token $response);Name=$tag;Description='Description';Id='1';AlbumCount='0'}
  Assert-Test ($response.BaseResponse.RequestMessage.RequestUri.AbsolutePath.TrimEnd('/') -eq ('/' + $controller)) ($controller + ': create succeeds despite extra fields')
  $response = Page ('/' + $controller + '/Create') $a
  $response = Post ('/' + $controller + '/Create') $a @{__RequestVerificationToken=(Token $response);Name=('  ' + $tag.ToUpperInvariant() + '  ')}
  Assert-Test ($response.Content.Contains('validation-summary-errors')) ($controller + ': normalized duplicate rejected')
  $key = if ($controller -eq 'Artists') {'ArtistId'} else {'GenreId'}
  $itemId = Query MusicStore ('SELECT ' + $key + ' FROM dbo.' + $controller + ' WHERE Name=@name') @{name=$tag}
  $editOne = Page ('/' + $controller + '/Edit/' + $itemId) $a
  $stale = Page ('/' + $controller + '/Edit/' + $itemId) $a
  $response = Post ('/' + $controller + '/Edit/' + $itemId) $a @{__RequestVerificationToken=(Token $editOne);Version=(Field $editOne.Content 'Version');Name=($tag + '-edited');Description='Edited'}
  Assert-Test ($response.BaseResponse.RequestMessage.RequestUri.AbsolutePath.TrimEnd('/') -eq ('/' + $controller)) ($controller + ': edit succeeds')
  $response = Post ('/' + $controller + '/Edit/' + $itemId) $a @{__RequestVerificationToken=(Token $stale);Version=(Field $stale.Content 'Version');Name=($tag + '-stale');Description='Stale'}
  Assert-Test ([int]$response.StatusCode -eq 409) ($controller + ': stale edit rejected')
  $savedName = Query MusicStore ('SELECT Name FROM dbo.' + $controller + ' WHERE ' + $key + '=@id') @{id=$itemId}
  Assert-Test ($savedName -eq ($tag + '-edited')) ($controller + ': stale edit preserves first change')
  $healthy = Page '/Admin' $a
  Assert-Test ([int]$healthy.StatusCode -eq 200) ($controller + ': server stays available after conflict')
  $response = Post ('/' + $controller + '/Delete/' + $itemId) $a @{__RequestVerificationToken=(Token $stale);Version=(Field $stale.Content 'Version')}
  Assert-Test ([int]$response.StatusCode -eq 409) ($controller + ': stale delete rejected')
  $response = Page ('/' + $controller + '/Edit/-1') $a
  Assert-Test ([int]$response.StatusCode -eq 400) ($controller + ': negative identifier rejected')
  $response = Page ('/' + $controller + '/Edit/2147483647') $a
  Assert-Test ([int]$response.StatusCode -eq 404) ($controller + ': nonexistent identifier rejected')
  $response = Page ('/' + $controller + '?page=-1') $a
  Assert-Test ([int]$response.StatusCode -eq 400) ($controller + ': invalid page rejected')
  Set-Variable -Name ($controller + 'Id') -Value $itemId
 }
 $response = Page '/StoreManager/Create' $a
 $response = Post '/StoreManager/Create' $a @{__RequestVerificationToken=(Token $response);Title=$tag;Price='10';ArtistId='2147483647';GenreId=$GenresId;AlbumId='1'}
 Assert-Test ($response.Content.Contains('Selecciona un artista existente')) 'Album with manipulated artist rejected'
 $response = Page '/StoreManager/Create' $a
 $response = Post '/StoreManager/Create' $a @{__RequestVerificationToken=(Token $response);Title=$tag;Price='-1';ArtistId=$ArtistsId;GenreId=$GenresId}
 Assert-Test ($response.Content.Contains('field-validation-error')) 'Album with negative price rejected'
 $response = Page '/StoreManager/Create' $a
 $response = Post '/StoreManager/Create' $a @{__RequestVerificationToken=(Token $response);Title=$tag;Price='10';ArtistId=$ArtistsId;GenreId=$GenresId;AlbumArtUrl='javascript:alert(1)'}
 Assert-Test ($response.Content.Contains('field-validation-error')) 'Unsafe cover URL rejected'
 $response = Page '/StoreManager/Create' $a
 $response = Post '/StoreManager/Create' $a @{__RequestVerificationToken=(Token $response);Title=$tag;Price='10';ArtistId=$ArtistsId;GenreId=$GenresId;AlbumId='1'}
 Assert-Test ($response.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/StoreManager') 'Album using new artist and genre created'
 $albumId = Query MusicStore 'SELECT AlbumId FROM dbo.Albums WHERE Title=@name' @{name=$tag}
 foreach ($controller in @('Artists','Genres')) {
  $itemId = Get-Variable -Name ($controller + 'Id') -ValueOnly
  $response = Page ('/' + $controller + '/Delete/' + $itemId) $a
  $version = Query MusicStore ('SELECT RowVersion FROM dbo.' + $controller + ' WHERE ' + $(if ($controller -eq 'Artists') {'ArtistId'} else {'GenreId'}) + '=@id') @{id=$itemId}
  $response = Post ('/' + $controller + '/Delete/' + $itemId) $a @{__RequestVerificationToken=(Token $response);Version=[Convert]::ToBase64String($version)}
  Assert-Test ((Text $response).Contains('existen álbumes asociados')) ($controller + ': crafted delete cannot remove referenced record')
 }
 $fkActions = Query MusicStore "SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.Albums') AND delete_referential_action<>0"
 Assert-Test ($fkActions -eq 0) 'Database disables cascading deletion of catalog references'
 $first = Page ('/StoreManager/Edit/' + $albumId) $a
 $stale = Page ('/StoreManager/Edit/' + $albumId) $a
 $response = Post ('/StoreManager/Edit/' + $albumId) $a @{__RequestVerificationToken=(Token $first);Version=(Field $first.Content 'Version');AlbumId=$albumId;Title=($tag+'-edited');Price='11';ArtistId=$ArtistsId;GenreId=$GenresId}
 Assert-Test ($response.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/StoreManager') 'Album edit succeeds'
 $response = Post ('/StoreManager/Edit/' + $albumId) $a @{__RequestVerificationToken=(Token $stale);Version=(Field $stale.Content 'Version');AlbumId=$albumId;Title=($tag+'-stale');Price='12';ArtistId=$ArtistsId;GenreId=$GenresId}
 Assert-Test ([int]$response.StatusCode -eq 409) 'Stale album edit rejected'
 $response = Post ('/StoreManager/Delete/' + $albumId) $a @{__RequestVerificationToken=(Token $stale);Version=(Field $stale.Content 'Version')}
 Assert-Test ([int]$response.StatusCode -eq 409) 'Stale album delete rejected'
 $response = Page ('/StoreManager/Delete/' + $albumId) $a
 $response = Post ('/StoreManager/Delete/' + $albumId) $a @{__RequestVerificationToken=(Token $response);Version=(Field $response.Content 'Version')}
 Assert-Test ($response.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/StoreManager') 'Temporary album delete succeeds'
 foreach ($controller in @('Artists','Genres')) {
  $itemId = Get-Variable -Name ($controller + 'Id') -ValueOnly
  $response = Page ('/' + $controller + '/Delete/' + $itemId) $a
  $response = Post ('/' + $controller + '/Delete/' + $itemId) $a @{__RequestVerificationToken=(Token $response);Version=(Field $response.Content 'Version')}
  Assert-Test ($response.BaseResponse.RequestMessage.RequestUri.AbsolutePath.TrimEnd('/') -eq ('/' + $controller)) ($controller + ': unreferenced record delete succeeds')
  $response = Post ('/' + $controller + '/Delete/' + $itemId) $a @{__RequestVerificationToken=(Token $response);Version=[Convert]::ToBase64String([byte[]](1..8))}
  Assert-Test ([int]$response.StatusCode -eq 404) ($controller + ': repeated delete handled')
 }
 $self = RoleForm $idA $a
 Assert-Test ([int]$self.StatusCode -eq 400) 'Administrator cannot open self role change'
 $form = RoleForm $idB $a
 $response = RolePost $form $a 'Wrong-password9!'
 Assert-Test ((Text $response).Contains('contraseña de confirmación es incorrecta')) 'Role change requires current administrator password'
 $response = RolePost $form $a $password
 Assert-Test ($response.Content.Contains('Permisos actualizados')) 'Administrator grants role through interface'
 $response = RolePost $form $a $password
 Assert-Test ((Text $response).Contains('cuenta cambió')) 'Replayed role form rejected'
 Login $emailB $b
 $response = Page '/Admin' $b
 Assert-Test ([int]$response.StatusCode -eq 200) 'Promoted user reaches administration'
 foreach ($route in @('/Users','/Users/Audit')) {
  $response = Page $route $b
  Assert-Test ([int]$response.StatusCode -eq 403) ('New administrator cannot manage permissions ' + $route)
 }
 $response = Page '/Admin' $b
 $response = Post '/Users/ChangeRole' $b @{__RequestVerificationToken=(Token $response);Id=$idA;Stamp='tampered';MakeAdministrator='false';Password=$password}
 Assert-Test ([int]$response.StatusCode -eq 403) 'Administrator cannot revoke owner through crafted POST'
 # Defense in depth: even an owner cannot edit another protected owner through the web.
 Query MusicStoreAccounts "INSERT dbo.AspNetUserRoles(UserId,RoleId) SELECT @id,Id FROM dbo.AspNetRoles WHERE Name='Owner'" @{id=$idB} | Out-Null
 $response = RoleForm $idB $a
 Assert-Test ([int]$response.StatusCode -eq 403) 'Protected owner cannot open role edit'
 $response = Page '/Users' $a
 $response = Post '/Users/ChangeRole' $a @{__RequestVerificationToken=(Token $response);Id=$idB;Stamp='tampered';MakeAdministrator='false';Password=$password}
 Assert-Test ((Text $response).Contains('propietaria está protegida')) 'Protected owner rejects role mutation in service'
 Query MusicStoreAccounts "DELETE ur FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON ur.RoleId=r.Id WHERE ur.UserId=@id AND r.Name='Owner'" @{id=$idB} | Out-Null
 $form = RoleForm $idB $a
 $response = RolePost $form $a $password
 Assert-Test ($response.Content.Contains('Permisos actualizados')) 'Administrator revokes role through interface'
 $response = Page '/Admin' $b
 Assert-Test ($response.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') 'Revocation invalidates existing authenticated session'
 Login $emailB $b
 $response = Page '/Admin' $b
 Assert-Test ([int]$response.StatusCode -eq 403) 'Revoked user logs in as normal user'
 $auditCount = Query MusicStoreAccounts 'SELECT COUNT(*) FROM dbo.AdminRoleAudit WHERE ActorId=@a AND TargetId=@b' @{a=$idA;b=$idB}
 Assert-Test ($auditCount -eq 2) 'Grant and revoke are recorded in audit history'
 $form = RoleForm $idB $a
 $response = Post '/Users/ChangeRole' $a @{__RequestVerificationToken=(Token $form);Id=$idA;Stamp=(Field $form.Content 'Stamp');MakeAdministrator='false';Password=$password}
 Assert-Test ($response.Content.Contains('propios permisos')) 'Crafted POST cannot remove own administrator role'
 $response = Post '/Users/ChangeRole' $a @{Id=$idB;Stamp=(Field $form.Content 'Stamp');MakeAdministrator='true';Password=$password}
 Assert-Test ([int]$response.StatusCode -eq 400) 'Role change without antiforgery token rejected'
 $response = Page '/Users?search=%27%20OR%201%3D1--' $a
 Assert-Test ([int]$response.StatusCode -eq 200 -and (Text $response).Contains('No hay resultados')) 'SQL-shaped user search treated as literal text'
 # Two separate authenticated sessions create the same artist simultaneously.
 $a2 = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
 Login $emailA $a2
 $create1 = Page '/Artists/Create' $a
 $create2 = Page '/Artists/Create' $a2
 $raceJobs = @()
 $worker = { param($Url,$Session,$Data) $r=Invoke-WebRequest $Url -Method Post -WebSession $Session -Body $Data -SkipHttpErrorCheck; [int]$r.StatusCode }
 $raceJobs += Start-ThreadJob -ScriptBlock $worker -ArgumentList ($baseUrl+'/Artists/Create'),$a,@{__RequestVerificationToken=(Token $create1);Name=($tag+'-race')}
 $raceJobs += Start-ThreadJob -ScriptBlock $worker -ArgumentList ($baseUrl+'/Artists/Create'),$a2,@{__RequestVerificationToken=(Token $create2);Name=($tag+'-race')}
 try {
  $raceJobs | Wait-Job | Out-Null
  $raceStatus = @($raceJobs | Receive-Job -ErrorAction Stop)
  $raceCount = Query MusicStore 'SELECT COUNT(*) FROM dbo.Artists WHERE Name=@name' @{name=($tag+'-race')}
  Assert-Test ($raceCount -eq 1 -and @($raceStatus | Where-Object { $_ -notin @(200,409) }).Count -eq 0) 'Concurrent duplicate creation stores exactly one artist'
 } finally { $raceJobs | Remove-Job -Force }
 # A double submission by the owner revokes only once; the stale form is rejected.
 $form = RoleForm $idB $a
 $response = RolePost $form $a $password
 Login $emailB $b
 $revokeB = RoleForm $idB $a
 $revokeA = RoleForm $idB $a2
 $bodyA = @{__RequestVerificationToken=(Token $revokeB);Id=$idB;Stamp=(Field $revokeB.Content 'Stamp');MakeAdministrator='false';Password=$password}
 $bodyB = @{__RequestVerificationToken=(Token $revokeA);Id=$idB;Stamp=(Field $revokeA.Content 'Stamp');MakeAdministrator='false';Password=$password}
 $raceJobs = @()
 $raceJobs += Start-ThreadJob -ScriptBlock $worker -ArgumentList ($baseUrl+'/Users/ChangeRole'),$a,$bodyA
 $raceJobs += Start-ThreadJob -ScriptBlock $worker -ArgumentList ($baseUrl+'/Users/ChangeRole'),$a2,$bodyB
 try {
  $raceJobs | Wait-Job | Out-Null
  $raceStatus = @($raceJobs | Receive-Job -ErrorAction Stop)
  $remaining = Query MusicStoreAccounts "SELECT COUNT(*) FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetRoles r ON ur.RoleId=r.Id WHERE r.Name='Administrator' AND ur.UserId IN (@a,@b)" @{a=$idA;b=$idB}
  Assert-Test ($remaining -eq 1 -and @($raceStatus | Where-Object { $_ -ge 500 }).Count -eq 0) 'Concurrent duplicate revocation preserves owner and removes target only once'
 } finally { $raceJobs | Remove-Job -Force }
 $afterCatalog = Query MusicStore "SELECT (SELECT AlbumId,Title,ArtistId,GenreId,Price,AlbumArtUrl FROM dbo.Albums WHERE Title NOT LIKE @prefix ORDER BY AlbumId FOR JSON PATH)" @{prefix=($tag+'%')}
 Assert-Test ($beforeCatalog -eq $afterCatalog) 'Original albums preserved exactly'
 Write-Host ('TOTAL: ' + $results.Count + ' passed')
} finally {
 Query MusicStore 'DELETE c FROM dbo.Carts c JOIN dbo.UserCarts uc ON c.CartId=uc.CartId JOIN MusicStoreAccounts.dbo.AspNetUsers u ON u.Id=uc.UserId WHERE u.Email IN (@a,@b); DELETE uc FROM dbo.UserCarts uc JOIN MusicStoreAccounts.dbo.AspNetUsers u ON u.Id=uc.UserId WHERE u.Email IN (@a,@b);' @{a=$emailA;b=$emailB} | Out-Null
 Query MusicStore 'DELETE FROM dbo.Albums WHERE Title LIKE @prefix; DELETE FROM dbo.Artists WHERE Name LIKE @prefix; DELETE FROM dbo.Genres WHERE Name LIKE @prefix;' @{prefix=($tag+'%')} | Out-Null
 Query MusicStoreAccounts 'DELETE FROM dbo.AdminRoleAudit WHERE ActorId IN (SELECT Id FROM dbo.AspNetUsers WHERE Email IN (@a,@b)); DELETE ur FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetUsers u ON ur.UserId=u.Id WHERE u.Email IN (@a,@b); DELETE FROM dbo.AspNetUsers WHERE Email IN (@a,@b);' @{a=$emailA;b=$emailB} | Out-Null
}
