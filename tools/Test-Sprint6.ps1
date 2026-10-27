# PowerShell 7. Only this run's temporary albums, cart rows and account are cleaned up.
param([string]$BaseUrl = 'http://localhost:54834')
$ErrorActionPreference = 'Stop'
$tag = 'sprint6-' + [guid]::NewGuid().ToString('N')
$testEmail = $tag + '@example.test'
$testPassword = 'Aa9!' + [guid]::NewGuid().ToString('N')
$passed = 0
function Assert-Test($Condition, $Label) { if (!$Condition) { throw ('FAIL: ' + $Label) }; $script:passed++; Write-Host ('PASS: ' + $Label) }
function Query($Database, $Sql, $Values = @{}) {
 $connection = [System.Data.SqlClient.SqlConnection]::new('Data Source=.\SQLEXPRESS;Initial Catalog='+$Database+';Integrated Security=True;')
 try { $connection.Open(); $command=$connection.CreateCommand(); $command.CommandText=$Sql
  foreach ($key in $Values.Keys) { $command.Parameters.AddWithValue('@'+$key,$Values[$key]) | Out-Null }
  return $command.ExecuteScalar()
 } finally { $connection.Dispose() }
}
function Page($Path,$Session) { Invoke-WebRequest ($BaseUrl+$Path) -WebSession $Session -SkipHttpErrorCheck }
function Field($Html,$Name) {
 $element=[regex]::Match($Html,'<input\b[^>]*\bname="'+[regex]::Escape($Name)+'"[^>]*>')
 [System.Net.WebUtility]::HtmlDecode([regex]::Match($element.Value,'\bvalue="([^"]*)"').Groups[1].Value)
}
function Token($Page) { Field $Page.Content '__RequestVerificationToken' }
function Value($Page,$Name) { [regex]::Match($Page.Content,('data-'+$Name+'="([^"]+)"')).Groups[1].Value }
function Total($Page) { [decimal]::Parse((Value $Page 'total'),[System.Globalization.CultureInfo]::InvariantCulture) }
function Count($Page) { [int](Value $Page 'count') }
function FirstLine($Page) { [regex]::Match($Page.Content,'<tr data-record-id="\d+">[\s\S]*?</tr>').Value }
function Post($Path,$Session,$Data,[bool]$Ajax=$true) {
 # An empty cart has no mutation form. Obtain the token from its actual add-to-cart entry point.
 if ($Data.ContainsKey('__RequestVerificationToken') -and [string]::IsNullOrEmpty($Data.__RequestVerificationToken)) {
  $detail=Page ('/Store/Details/'+$script:albumId) $Session
  $Data.__RequestVerificationToken=Token $detail
 }
 $headers = @{}
 $Session.Headers.Remove('X-Requested-With') | Out-Null
 if ($Ajax) { $headers['X-Requested-With']='XMLHttpRequest' }
 Invoke-WebRequest ($BaseUrl+$Path) -Method Post -WebSession $Session -Body $Data -Headers $headers -SkipHttpErrorCheck
}
function Add($Album,$Session,$Current,[bool]$Ajax=$true) { Post '/ShoppingCart/AddToCart' $Session @{__RequestVerificationToken=(Token $Current);albumId=$Album} $Ajax }
$a=[Microsoft.PowerShell.Commands.WebRequestSession]::new()
$b=[Microsoft.PowerShell.Commands.WebRequestSession]::new()
$limit=[Microsoft.PowerShell.Commands.WebRequestSession]::new()
$before = Query MusicStore 'SELECT (SELECT AlbumId,Title,Price,ArtistId,GenreId,AlbumArtUrl FROM dbo.Albums ORDER BY AlbumId FOR JSON PATH)'
try {
 $seed = Query MusicStore @'
DECLARE @artist int=(SELECT MIN(ArtistId) FROM dbo.Artists), @genre int=(SELECT MIN(GenreId) FROM dbo.Genres);
IF @artist IS NULL OR @genre IS NULL THROW 51000,'Existing catalog required.',1;
INSERT dbo.Albums(Title,ArtistId,GenreId,Price,AlbumArtUrl)
SELECT TOP (51) @tag+'-'+CONVERT(nvarchar(10),ROW_NUMBER() OVER (ORDER BY object_id)),@artist,@genre,12.34,'' FROM sys.all_objects;
SELECT (SELECT AlbumId FROM dbo.Albums WHERE Title LIKE @prefix ORDER BY AlbumId FOR JSON PATH);
'@ @{tag=$tag;prefix=($tag+'%')}
 $albums=@($seed | ConvertFrom-Json)
 $albumId=[int]$albums[0].AlbumId
 $pageA=Page '/ShoppingCart' $a
 Assert-Test ([int]$pageA.StatusCode -eq 200 -and (Count $pageA) -eq 0) 'Anonymous empty cart available'
 Assert-Test ($pageA.Headers['Cache-Control'] -match 'no-store') 'Cart response is not cached'
 $noToken=Post '/ShoppingCart/AddToCart' $a @{albumId=$albumId}
 Assert-Test ([int]$noToken.StatusCode -eq 400) 'Missing antiforgery token rejected'
 $unsafeGet=Page ('/ShoppingCart/AddToCart?albumId='+$albumId) $a
 Assert-Test ([int]$unsafeGet.StatusCode -in @(404,405)) 'GET cannot mutate cart'
 foreach ($invalid in @('0','-1','abc','9999999999999999999999')) {
  $response=Add $invalid $a $pageA
  Assert-Test ([int]$response.StatusCode -eq 400 -and (Count $response) -eq 0) ('Invalid album identifier rejected: '+$invalid)
 }
 $response=Add 2147483647 $a $pageA
 Assert-Test ([int]$response.StatusCode -eq 404) 'Missing album rejected'
 $pageA=Post '/ShoppingCart/AddToCart' $a @{__RequestVerificationToken=(Token $pageA);albumId=$albumId;CartId='foreign';Count='99';Price='0.01';Total='0.01'}
 Assert-Test ((Count $pageA) -eq 1 -and (Total $pageA) -eq [decimal]12.34) 'Add ignores client identity, quantity and price'
 Assert-Test (!$pageA.Content.Contains('<html>') -and $pageA.Content.Contains('data-cart-snapshot')) 'AJAX returns only the cart fragment'
 $pageB=Page '/ShoppingCart' $b
 Assert-Test ((Count $pageB) -eq 0) 'Separate browser starts with an independent cart'
 $line=FirstLine $pageA
 $record=Field $line 'id'; $version=Field $line 'version'
 $response=Post '/ShoppingCart/UpdateQuantity' $b @{__RequestVerificationToken=(Token $pageB);id=$record;quantity='50';version=$version}
 Assert-Test ([int]$response.StatusCode -eq 404 -and (Count $response) -eq 0) 'Other cart cannot update a known record identifier'
 $response=Post '/ShoppingCart/RemoveFromCart' $b @{__RequestVerificationToken=(Token $pageB);id=$record;version=$version}
 Assert-Test ([int]$response.StatusCode -eq 404) 'Other cart cannot remove a known record identifier'
 $pageA=Add $albumId $a $pageA
 Assert-Test ((Count $pageA) -eq 2 -and ([regex]::Matches($pageA.Content,'<tr data-record-id=')).Count -eq 1 -and (Total $pageA) -eq [decimal]24.68) 'Repeated album merges into one line with accurate total'
 $line=FirstLine $pageA; $record=Field $line 'id'; $version=Field $line 'version'
 foreach ($invalid in @('0','-1','100','1.5','abc','99999999999999999999','')) {
  $response=Post '/ShoppingCart/UpdateQuantity' $a @{__RequestVerificationToken=(Token $pageA);id=$record;quantity=$invalid;version=$version}
  Assert-Test ([int]$response.StatusCode -eq 400 -and (Count $response) -eq 2) ('Invalid quantity rejected: '+$invalid)
 }
 $response=Post '/ShoppingCart/UpdateQuantity' $a @{__RequestVerificationToken=(Token $pageA);id=$record;quantity='3';version='invalid'}
 Assert-Test ([int]$response.StatusCode -eq 400) 'Malformed version rejected'
 $stale=$pageA
 $pageA=Post '/ShoppingCart/UpdateQuantity' $a @{__RequestVerificationToken=(Token $pageA);id=$record;quantity='3';version=$version;UnitPrice='0';CartId='foreign'}
 Assert-Test ((Count $pageA) -eq 3 -and (Total $pageA) -eq [decimal]37.02) 'Quantity update uses server decimal price'
 $response=Post '/ShoppingCart/UpdateQuantity' $a @{__RequestVerificationToken=(Token $stale);id=$record;quantity='5';version=$version}
 Assert-Test ([int]$response.StatusCode -eq 409 -and (Count $response) -eq 3) 'Stale quantity form cannot overwrite first update'
 $response=Post '/ShoppingCart/RemoveFromCart' $a @{__RequestVerificationToken=(Token $stale);id=$record;version=$version}
 Assert-Test ([int]$response.StatusCode -eq 409 -and (Count $response) -eq 3) 'Stale delete cannot remove updated line'
 $healthy=Page '/ShoppingCart' $a
 Assert-Test ([int]$healthy.StatusCode -eq 200) 'Cart stays available after conflict'
 $response=Post '/ShoppingCart/EmptyCart' $b @{__RequestVerificationToken=(Token $pageB);revision=(Value $pageA 'revision')}
 Assert-Test ([int]$response.StatusCode -eq 409 -and (Count $response) -eq 0) 'Borrowed revision cannot empty another cart'
 $line=FirstLine $pageA; $version=Field $line 'version'
 $pageA=Post '/ShoppingCart/UpdateQuantity' $a @{__RequestVerificationToken=(Token $pageA);id=$record;quantity='99';version=$version}
 Assert-Test ((Count $pageA) -eq 99 -and (Total $pageA) -eq [decimal]1221.66) 'Maximum quantity 99 calculates correctly'
 $response=Add $albumId $a $pageA
 Assert-Test ([int]$response.StatusCode -eq 400 -and (Count $response) -eq 99) 'Add cannot exceed quantity limit'
 # Exercise the SQL constraint inside a rolled-back transaction on this test's line only.
 $constraint=Query MusicStore @'
SET XACT_ABORT OFF;
BEGIN TRANSACTION;
BEGIN TRY
 UPDATE dbo.Carts SET [Count]=0 WHERE RecordId=@id;
 ROLLBACK; SELECT 0;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK; SELECT ERROR_NUMBER();
END CATCH;
'@ @{id=[int]$record}
 Assert-Test ($constraint -eq 547) 'Database rejects invalid quantities independently of UI'
 $constraint=Query MusicStore @'
SET XACT_ABORT OFF;
BEGIN TRANSACTION;
BEGIN TRY
 DELETE dbo.Albums WHERE AlbumId=@id;
 ROLLBACK; SELECT 0;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK; SELECT ERROR_NUMBER();
END CATCH;
'@ @{id=$albumId}
 Assert-Test ($constraint -eq 547) 'Album referenced by cart cannot be deleted in database'
 $pageA=Page '/ShoppingCart' $a
 $line=FirstLine $pageA
 $pageA=Post '/ShoppingCart/RemoveFromCart' $a @{__RequestVerificationToken=(Token $pageA);id=$record;version=(Field $line 'version')}
 Assert-Test ((Count $pageA) -eq 0 -and (Total $pageA) -eq 0) 'Remove deletes the entire line and resets total'
 $response=Post '/ShoppingCart/RemoveFromCart' $a @{__RequestVerificationToken=(Token $pageA);id=$record;version=$version}
 Assert-Test ([int]$response.StatusCode -eq 404) 'Repeated removal handled safely'
 $pageA=Add $albumId $a $pageA $false
 Assert-Test ($pageA.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/ShoppingCart' -and $pageA.Content.Contains('<html>')) 'Non-JavaScript add follows redirect to cart'
 $line=FirstLine $pageA; $record=Field $line 'id'
 $pageA=Post '/ShoppingCart/UpdateQuantity' $a @{__RequestVerificationToken=(Token $pageA);id=$record;quantity='2';version=(Field $line 'version')} $false
 Assert-Test ((Count $pageA) -eq 2 -and $pageA.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/ShoppingCart') 'Non-JavaScript update remains functional'
 $oldRevision=Value $pageA 'revision'
 $pageA=Add ([int]$albums[1].AlbumId) $a $pageA
 Assert-Test ((Count $pageA) -eq 3 -and (Total $pageA) -eq [decimal]37.02) 'Multiple albums sum correctly'
 $response=Post '/ShoppingCart/EmptyCart' $a @{__RequestVerificationToken=(Token $pageA);revision=$oldRevision}
 Assert-Test ([int]$response.StatusCode -eq 409 -and (Count $response) -eq 3) 'Old empty-cart form cannot remove newer additions'
 $pageA=Post '/ShoppingCart/EmptyCart' $a @{__RequestVerificationToken=(Token $pageA);revision=(Value $pageA 'revision')}
 Assert-Test ((Count $pageA) -eq 0) 'Empty cart clears current items'
 # Concurrent adds to the same new session cart must merge without lost updates.
 $first=Page ('/Store/Details/'+$albumId) $a
 $secondSession=[Microsoft.PowerShell.Commands.WebRequestSession]::new(); $secondSession.Cookies=$a.Cookies
 $worker={param($Url,$Session,$Data) $r=Invoke-WebRequest $Url -Method Post -WebSession $Session -Body $Data -Headers @{'X-Requested-With'='XMLHttpRequest'} -SkipHttpErrorCheck; [int]$r.StatusCode}
 $jobs=@()
 $body=@{__RequestVerificationToken=(Token $first);albumId=$albumId}
 $jobs+=Start-ThreadJob -ScriptBlock $worker -ArgumentList ($BaseUrl+'/ShoppingCart/AddToCart'),$a,$body
 $jobs+=Start-ThreadJob -ScriptBlock $worker -ArgumentList ($BaseUrl+'/ShoppingCart/AddToCart'),$secondSession,$body
 try {
  $jobs | Wait-Job | Out-Null; $statuses=@($jobs | Receive-Job -ErrorAction Stop)
  $pageA=Page '/ShoppingCart' $a
  Assert-Test ((Count $pageA) -eq 2 -and @($statuses | Where-Object { $_ -ne 200 }).Count -eq 0) 'Concurrent adds preserve both increments'
 } finally { $jobs | Remove-Job -Force }
 $limitPage=Page '/ShoppingCart' $limit
 foreach ($album in $albums | Select-Object -First 50) { $limitPage=Add ([int]$album.AlbumId) $limit $limitPage; if ([int]$limitPage.StatusCode -ne 200) { throw 'Could not prepare limit test' } }
 Assert-Test ((Count $limitPage) -eq 50) 'Cart accepts 50 different albums'
 $response=Add ([int]$albums[50].AlbumId) $limit $limitPage
 Assert-Test ([int]$response.StatusCode -eq 400 -and (Count $response) -eq 50) 'Cart rejects 51st distinct album'
 # Register and sign in using the same browser. Account-bound migration is Sprint 7.
 $register=Page '/Account/Register' $a
 $register=Post '/Account/Register' $a @{__RequestVerificationToken=(Token $register);Email=$testEmail;Password=$testPassword;ConfirmPassword=$testPassword} $false
 $login=Page '/Account/Login' $a
 $login=Post '/Account/Login' $a @{__RequestVerificationToken=(Token $login);Email=$testEmail;Password=$testPassword} $false
 $pageA=Page '/ShoppingCart' $a
 Assert-Test ((Count $pageA) -eq 2) 'Same session retains anonymous cart through login'
 $landingPage=Page '/' $a
 $logout=Post '/Account/LogOff' $a @{__RequestVerificationToken=(Token $landingPage)} $false
 $pageA=Page '/ShoppingCart' $a
 Assert-Test ((Count $pageA) -eq 0) 'Logout starts a fresh cart for the next account'
 $after=Query MusicStore 'SELECT (SELECT AlbumId,Title,Price,ArtistId,GenreId,AlbumArtUrl FROM dbo.Albums WHERE Title NOT LIKE @prefix ORDER BY AlbumId FOR JSON PATH)' @{prefix=($tag+'%')}
 Assert-Test ($before -eq $after) 'Original catalog preserved exactly'
 Write-Host ('TOTAL: '+$passed+' passed')
} finally {
 Query MusicStore 'DELETE c FROM dbo.Carts c JOIN dbo.UserCarts uc ON c.CartId=uc.CartId JOIN MusicStoreAccounts.dbo.AspNetUsers u ON u.Id=uc.UserId WHERE u.Email=@email; DELETE uc FROM dbo.UserCarts uc JOIN MusicStoreAccounts.dbo.AspNetUsers u ON u.Id=uc.UserId WHERE u.Email=@email;' @{email=$testEmail} | Out-Null
 Query MusicStore 'DELETE c FROM dbo.Carts c JOIN dbo.Albums a ON c.AlbumId=a.AlbumId WHERE a.Title LIKE @prefix; DELETE FROM dbo.Albums WHERE Title LIKE @prefix;' @{prefix=($tag+'%')} | Out-Null
 Query MusicStoreAccounts 'DELETE ur FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetUsers u ON ur.UserId=u.Id WHERE u.Email=@email; DELETE FROM dbo.AspNetUsers WHERE Email=@email;' @{email=$testEmail} | Out-Null
}
