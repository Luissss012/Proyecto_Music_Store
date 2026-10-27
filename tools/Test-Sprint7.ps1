# PowerShell 7. Uses only this run's temporary accounts/catalog/orders; cleanup runs in finally.
param([string]$BaseUrl='http://localhost:54834')
$ErrorActionPreference='Stop'
$tag='sprint7-'+[guid]::NewGuid().ToString('N')
$emailA=$tag+'-a@example.test'; $emailB=$tag+'-b@example.test'; $emailC=$tag+'-c@example.test'
$password='Aa9!'+[guid]::NewGuid().ToString('N')
$triggerName='TR_Sprint7_'+[guid]::NewGuid().ToString('N')
$passed=0
function Assert-Test($Condition,$Label) { if(!$Condition){throw ('FAIL: '+$Label)}; $script:passed++; Write-Host ('PASS: '+$Label) }
function Query($Database,$Sql,$Values=@{}) {
 $connection=[System.Data.SqlClient.SqlConnection]::new('Data Source=.\SQLEXPRESS;Initial Catalog='+$Database+';Integrated Security=True;')
 try{$connection.Open();$command=$connection.CreateCommand();$command.CommandText=$Sql
  foreach($key in $Values.Keys){$command.Parameters.AddWithValue('@'+$key,$Values[$key])|Out-Null}
  return $command.ExecuteScalar()
 }finally{$connection.Dispose()}
}
function Session { [Microsoft.PowerShell.Commands.WebRequestSession]::new() }
function Page($Path,$Session){$Session.Headers.Remove('X-Requested-With')|Out-Null;Invoke-WebRequest ($BaseUrl+$Path) -WebSession $Session -SkipHttpErrorCheck}
function Field($Html,$Name){$input=[regex]::Match($Html,'<input\b[^>]*\bname="'+[regex]::Escape($Name)+'"[^>]*>');[System.Net.WebUtility]::HtmlDecode([regex]::Match($input.Value,'\bvalue="([^"]*)"').Groups[1].Value)}
function Token($Page){Field $Page.Content '__RequestVerificationToken'}
function Text($Page){[System.Net.WebUtility]::HtmlDecode($Page.Content)}
function Count($Page){[int][regex]::Match($Page.Content,'data-count="(\d+)"').Groups[1].Value}
function Total($Page){[decimal]::Parse([regex]::Match($Page.Content,'data-total="([^"]+)"').Groups[1].Value,[System.Globalization.CultureInfo]::InvariantCulture)}
function FirstLine($Page){[regex]::Match($Page.Content,'<tr data-record-id="\d+">[\s\S]*?</tr>').Value}
function Post($Path,$Session,$Data,[bool]$Ajax=$false){
 $Session.Headers.Remove('X-Requested-With')|Out-Null;$headers=@{};if($Ajax){$headers['X-Requested-With']='XMLHttpRequest'}
 Invoke-WebRequest ($BaseUrl+$Path) -Method Post -WebSession $Session -Body $Data -Headers $headers -SkipHttpErrorCheck
}
function Cart($Session){Page '/ShoppingCart' $Session}
function Add($Session,$Album){$p=Page ('/Store/Details/'+$Album) $Session;Post '/ShoppingCart/AddToCart' $Session @{__RequestVerificationToken=(Token $p);albumId=$Album} $true}
function Qty($Session,$Amount){$p=Cart $Session;$line=FirstLine $p;Post '/ShoppingCart/UpdateQuantity' $Session @{__RequestVerificationToken=(Token $p);id=(Field $line 'id');quantity=$Amount;version=(Field $line 'version')} $true}
function Empty($Session){$p=Cart $Session;if((Count $p)-eq 0){return $p};$revision=[regex]::Match($p.Content,'data-revision="([^"]+)"').Groups[1].Value;Post '/ShoppingCart/EmptyCart' $Session @{__RequestVerificationToken=(Token $p);revision=$revision} $true}
function Register($Session,$Email,$Return=''){
 $p=Page ('/Account/Register?returnUrl='+[Uri]::EscapeDataString($Return)) $Session
 Post ('/Account/Register?returnUrl='+[Uri]::EscapeDataString($Return)) $Session @{__RequestVerificationToken=(Token $p);Email=$Email;Password=$password;ConfirmPassword=$password}
}
function Login($Session,$Email,$Return=''){
 $p=Page ('/Account/Login?returnUrl='+[Uri]::EscapeDataString($Return)) $Session
 Post ('/Account/Login?returnUrl='+[Uri]::EscapeDataString($Return)) $Session @{__RequestVerificationToken=(Token $p);Email=$Email;Password=$password}
}
function Logout($Session){$p=Page '/' $Session;Post '/Account/LogOff' $Session @{__RequestVerificationToken=(Token $p)}}
function CheckoutBody($Page,$Email,$Extra=@{}){
 $body=@{__RequestVerificationToken=(Token $Page);Ticket=(Field $Page.Content 'Ticket');FirstName='Prueba';LastName='Temporal';Address='Calle ficticia de prueba';City='Guatemala';State='Guatemala';PostalCode='01001';Country='Guatemala';Phone='+502 5555 0123';Email=$Email;PromoCode='FREE'}
 foreach($key in $Extra.Keys){$body[$key]=$Extra[$key]};return $body
}
function OrderId($Page){[int][regex]::Match($Page.BaseResponse.RequestMessage.RequestUri.AbsolutePath,'/Checkout/Complete/(\d+)').Groups[1].Value}
function ListedOrderIds($Page){@([regex]::Matches($Page.Content,'href="/Checkout/Complete/(\d+)"')|ForEach-Object{[int]$_.Groups[1].Value})}
function OrderCount($UserId){Query MusicStore 'SELECT COUNT(*) FROM dbo.Orders WHERE UserId=@id' @{id=$UserId}}
$a=Session;$b=Session;$c=Session;$guest=Session
$before=Query MusicStore 'SELECT (SELECT AlbumId,Title,Price,ArtistId,GenreId,AlbumArtUrl FROM dbo.Albums ORDER BY AlbumId FOR JSON PATH)'
$originalOrders=Query MusicStore 'SELECT COUNT(*) FROM dbo.Orders'
try{
 $seed=Query MusicStore @'
INSERT dbo.Artists(Name) VALUES(@tag);
DECLARE @artist int=SCOPE_IDENTITY();
INSERT dbo.Genres(Name,Description) VALUES(@tag,'Temporary test genre');
DECLARE @genre int=SCOPE_IDENTITY();
INSERT dbo.Albums(Title,ArtistId,GenreId,Price,AlbumArtUrl)
SELECT TOP(51) @tag+'-'+CONVERT(nvarchar(10),ROW_NUMBER() OVER(ORDER BY object_id)),@artist,@genre,12.34,'' FROM sys.all_objects;
SELECT (SELECT AlbumId FROM dbo.Albums WHERE Title LIKE @prefix ORDER BY AlbumId FOR JSON PATH);
'@ @{tag=$tag;prefix=($tag+'%')}
 $albums=@($seed|ConvertFrom-Json);$album1=[int]$albums[0].AlbumId;$album2=[int]$albums[1].AlbumId
 Query MusicStore 'UPDATE dbo.Albums SET Price=9.99 WHERE AlbumId=@id' @{id=$album2}|Out-Null
 foreach($route in @('/Checkout','/Checkout/AddressAndPayment','/Checkout/Orders','/Checkout/Complete/1')){
  $p=Page $route $guest;Assert-Test ($p.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login') ('Anonymous checkout/order route requires login: '+$route)
 }
 Add $a $album1|Out-Null;$p=Add $a $album1
 Assert-Test ((Count $p)-eq 2) 'Anonymous cart prepared without registration'
 $p=Register $a $emailA '/Checkout/AddressAndPayment'
 Assert-Test ($p.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login' -and $p.BaseResponse.RequestMessage.RequestUri.Query.Contains('returnUrl')) 'Registration preserves return to checkout'
 Assert-Test ((Count (Cart $a))-eq 2) 'Registration preserves guest products before login'
 $p=Login $a $emailA '/Checkout/AddressAndPayment'
 Assert-Test ($p.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Checkout/AddressAndPayment') 'Login after registration returns to checkout'
 Assert-Test ((Count (Cart $a))-eq 2) 'Login attaches anonymous products to account'
 $idA=Query MusicStoreAccounts 'SELECT Id FROM dbo.AspNetUsers WHERE Email=@email' @{email=$emailA}
 Register $b $emailB|Out-Null;Login $b $emailB|Out-Null
 $idB=Query MusicStoreAccounts 'SELECT Id FROM dbo.AspNetUsers WHERE Email=@email' @{email=$emailB}
 Assert-Test ((Count (Cart $b))-eq 0) 'Second account does not inherit first account cart'
 Add $b $album2|Out-Null;Logout $b|Out-Null
 Assert-Test ((Count (Cart $b))-eq 0) 'Logout hides saved account cart from anonymous browser'
 Login $b $emailB|Out-Null
 Assert-Test ((Count (Cart $b))-eq 1) 'Login restores same account cart after logout'
 $bDevice=Session;Login $bDevice $emailB|Out-Null
 Assert-Test ((Count (Cart $bDevice))-eq 1) 'Cart restored in independent browser session'
 Empty $bDevice|Out-Null
 Assert-Test ((Count (Cart $b))-eq 0) 'Same account shares current cart across devices'
 Add $b $album2|Out-Null
 Logout $a|Out-Null;Add $a $album1|Out-Null;Add $a $album2|Out-Null;Login $a $emailA|Out-Null
 $p=Cart $a
 Assert-Test ((Count $p)-eq 4 -and (Total $p)-eq [decimal]47.01) 'Saved and anonymous carts merge quantities and distinct products'
 Assert-Test (([regex]::Matches($p.Content,'<tr data-record-id=')).Count -eq 2) 'Merge keeps a single line per album'
 $savedLine=FirstLine $p
 $bCart=Cart $b
 $p=Post '/ShoppingCart/UpdateQuantity' $b @{__RequestVerificationToken=(Token $bCart);id=(Field $savedLine 'id');quantity='9';version=(Field $savedLine 'version')} $true
 Assert-Test ([int]$p.StatusCode -eq 404 -and (Count $p)-eq 1) 'Account cannot alter another account line'
 $p=Login $a $emailB
 Assert-Test ($p.Content.Contains($emailA) -and !(Text $p).Contains('Hola, '+$emailB)) 'Cannot silently switch accounts without logout'
 $aDevice=Session;Login $aDevice $emailA|Out-Null
 $stale=Cart $aDevice;Qty $a 4|Out-Null
 $line=FirstLine $stale
 $p=Post '/ShoppingCart/UpdateQuantity' $aDevice @{__RequestVerificationToken=(Token $stale);id=(Field $line 'id');quantity='9';version=(Field $line 'version')} $true
 Assert-Test ([int]$p.StatusCode -eq 409 -and (Count $p)-eq 5) 'Cross-device stale edits preserve first update'
 Qty $a 99|Out-Null
 $overflow=Session;Add $overflow $album1|Out-Null;$p=Login $overflow $emailA
 Assert-Test ($p.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login' -and (Text $p).Contains('99 unidades')) 'Over-limit merge is rejected with explanation'
 Assert-Test ((Count (Cart $overflow))-eq 1 -and (Count (Cart $a))-eq 100) 'Rejected merge preserves guest and saved carts without truncation'
 Empty $overflow|Out-Null;$p=Login $overflow $emailA
 Assert-Test ($p.BaseResponse.RequestMessage.RequestUri.AbsolutePath -ne '/Account/Login' -and (Count (Cart $overflow))-eq 100) 'Login succeeds after resolving merge limit'
 $authCookie=$a.Cookies.GetCookies([Uri]$BaseUrl)['MusicStore.Auth']
 $expiredSession=Session;$expiredSession.Cookies.Add([Uri]$BaseUrl,$authCookie)
 Assert-Test ((Count (Cart $expiredSession))-eq 100) 'Saved cart restored when server session is new but authentication remains'
 # An expired authentication cookie must not expose the old saved cart anonymously.
 $anonymousAgain=Session
 $sessionCookie=$a.Cookies.GetCookies([Uri]$BaseUrl)['ASP.NET_SessionId']
 $anonymousAgain.Cookies.Add([Uri]$BaseUrl,$sessionCookie)
 Assert-Test ((Count (Cart $anonymousAgain))-eq 0) 'Lost authentication does not expose saved cart through old session cookie'
 # Resolve the account again after the simulated anonymous access.
 Login $aDevice $emailA|Out-Null
 Empty $a|Out-Null
 $p=Page '/Checkout/AddressAndPayment' $a
 Assert-Test ($p.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/ShoppingCart') 'Empty-cart checkout redirects safely'
 Assert-Test ((OrderCount $idA)-eq 0) 'Empty checkout creates no order'
 Add $a $album1|Out-Null;Add $a $album1|Out-Null;Add $a $album2|Out-Null
 $form=Page '/Checkout/AddressAndPayment' $a
 Assert-Test ([int]$form.StatusCode -eq 200 -and $form.Content.Contains('34.67')) 'Checkout displays current authoritative total'
 foreach($case in @(@{FirstName='   '},@{FirstName='Luis123'},@{LastName='Perez7'},@{FirstName="Luis`nAndres"},@{City='12345'},@{Country='Guatemala7'},@{State='@@@'},@{Address='12345'},@{Address=('x'*201)},@{Email='not-an-email'},@{Phone='-------'},@{Phone='555+55555'},@{Phone='1234567890123456'},@{PromoCode='WRONG'},@{PostalCode='!'})){
  $p=Post '/Checkout/AddressAndPayment' $a (CheckoutBody $form $emailA $case)
  Assert-Test ([int]$p.StatusCode -eq 400 -and (OrderCount $idA)-eq 0 -and (Count (Cart $a))-eq 3) ('Invalid checkout preserves cart: '+($case.Keys -join ','))
 }
 $p=Post '/Checkout/AddressAndPayment' $a @{Ticket=(Field $form.Content 'Ticket');PromoCode='FREE'}
 Assert-Test ([int]$p.StatusCode -eq 400 -and (OrderCount $idA)-eq 0) 'Checkout requires antiforgery token'
 $p=Post '/Checkout/AddressAndPayment' $a (CheckoutBody $form $emailA @{Ticket='invalid'})
 Assert-Test ([int]$p.StatusCode -eq 400 -and (OrderCount $idA)-eq 0) 'Forged checkout ticket rejected'
 $p=Post '/Checkout/AddressAndPayment' $a (CheckoutBody $form $emailA @{Address='<script>alert(1)</script>'})
 Assert-Test ([int]$p.StatusCode -eq 400 -and (OrderCount $idA)-eq 0 -and !$p.Content.Contains('<script>alert(1)</script>')) 'HTML payload rejected without order creation or executable output'
 $foreignForm=Page '/Checkout/AddressAndPayment' $b
 $p=Post '/Checkout/AddressAndPayment' $a (CheckoutBody $form $emailA @{Ticket=(Field $foreignForm.Content 'Ticket')})
 Assert-Test ([int]$p.StatusCode -eq 400 -and (OrderCount $idA)-eq 0) 'Another account checkout ticket cannot be reused'
 Add $a $album1|Out-Null
 $p=Post '/Checkout/AddressAndPayment' $a (CheckoutBody $form $emailA)
 Assert-Test ([int]$p.StatusCode -eq 409 -and (OrderCount $idA)-eq 0 -and (Count (Cart $a))-eq 4) 'Changed cart requires review before order creation'
 $form=Page '/Checkout/AddressAndPayment' $a
 Query MusicStore 'UPDATE dbo.Albums SET Price=11.11 WHERE AlbumId=@id' @{id=$album2}|Out-Null
 $p=Post '/Checkout/AddressAndPayment' $a (CheckoutBody $form $emailA)
 Assert-Test ([int]$p.StatusCode -eq 409 -and $p.Content.Contains('48.13') -and (OrderCount $idA)-eq 0) 'Changed catalog price invalidates old confirmation'
 # Force a failure on only this test account's detail insert, proving transaction rollback.
 $safeId=$idA.Replace("'","''")
 Query MusicStore ("CREATE TRIGGER dbo."+$triggerName+" ON dbo.OrderDetails AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.Orders o ON o.OrderId=i.OrderId WHERE o.UserId=N'"+$safeId+"') THROW 51001,'Temporary rollback verification',1; END")|Out-Null
 $form=Page '/Checkout/AddressAndPayment' $a
 $body=CheckoutBody $form $emailA @{UserId=$idB;Total='0';OrderId='999';'OrderDetails[0].Quantity'='999'}
 $p=Post '/Checkout/AddressAndPayment' $a $body
 Assert-Test ([int]$p.StatusCode -eq 500 -and (OrderCount $idA)-eq 0 -and (Count (Cart $a))-eq 4) 'Database failure rolls back order and preserves entire cart'
 Assert-Test (!$p.Content.Contains('Temporary rollback verification') -and !$p.Content.Contains('SqlException')) 'Database failure hides technical details'
 Query MusicStore ('DROP TRIGGER dbo.'+$triggerName)|Out-Null
 $p=Post '/Checkout/AddressAndPayment' $a $body;$order1=OrderId $p
 Assert-Test ($order1 -gt 0 -and (OrderCount $idA)-eq 1) 'Valid checkout creates one order after transient failure'
 $saved=Query MusicStore 'SELECT (SELECT UserId,Total,CheckoutToken FROM dbo.Orders WHERE OrderId=@id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)' @{id=$order1}|ConvertFrom-Json
 Assert-Test ($saved.UserId -eq $idA -and [decimal]$saved.Total -eq [decimal]48.13) 'Order owner and total ignore overposted fields'
 $details=Query MusicStore 'SELECT SUM(Quantity) FROM dbo.OrderDetails WHERE OrderId=@id' @{id=$order1}
 Assert-Test ($details -eq 4 -and (Count (Cart $a))-eq 0) 'Order details persist and cart empties only after success'
 $p=Post '/Checkout/AddressAndPayment' $a $body
 Assert-Test ((OrderId $p)-eq $order1 -and (OrderCount $idA)-eq 1) 'Repeated POST returns original order without duplication'
 $p=Page ('/Checkout/Complete/'+$order1) $a
 Assert-Test ([int]$p.StatusCode -eq 200 -and (OrderCount $idA)-eq 1) 'Refreshing confirmation does not create another order'
 $p=Page '/Checkout/Orders' $a
 Assert-Test ((ListedOrderIds $p) -contains $order1) 'User can find own order in history'
 $p=Page ('/Checkout/Complete/'+$order1) $b
 Assert-Test ([int]$p.StatusCode -eq 404 -and !$p.Content.Contains('Calle ficticia de prueba')) 'Other user cannot read order details or personal information'
 $p=Page '/Checkout/Complete/2147483647' $b
 Assert-Test ([int]$p.StatusCode -eq 404) 'Unknown and foreign orders have same rejection'
 $p=Page '/Checkout/Complete/-1' $a
 Assert-Test ([int]$p.StatusCode -eq 400) 'Invalid order identifier rejected'
 $p=Page '/Checkout/Orders?page=-1' $a
 Assert-Test ([int]$p.StatusCode -eq 400) 'Invalid order-history page rejected'
 Query MusicStore 'UPDATE dbo.Albums SET Title=@title,Price=20 WHERE AlbumId=@id' @{id=$album1;title=($tag+'-renamed')}|Out-Null
 $snapshotPrice=Query MusicStore 'SELECT UnitPrice FROM dbo.OrderDetails WHERE OrderId=@order AND AlbumId=@album' @{order=$order1;album=$album1}
 $snapshotTitle=Query MusicStore 'SELECT AlbumTitle FROM dbo.OrderDetails WHERE OrderId=@order AND AlbumId=@album' @{order=$order1;album=$album1}
 Assert-Test ([decimal]$snapshotPrice -eq [decimal]12.34 -and $snapshotTitle -ne ($tag+'-renamed')) 'Order preserves purchased price and title after catalog edits'
 Add $a ([int]$albums[2].AlbumId)|Out-Null
 $p=Post '/Checkout/AddressAndPayment' $a $body
 Assert-Test ((OrderId $p)-eq $order1 -and (Count (Cart $a))-eq 1) 'Replaying completed ticket cannot empty a new cart'
 # Double click: same signed ticket in two concurrent requests.
 $form=Page '/Checkout/AddressAndPayment' $a;$body2=CheckoutBody $form $emailA
 $worker={param($Url,$Session,$Data) $r=Invoke-WebRequest $Url -Method Post -WebSession $Session -Body $Data -SkipHttpErrorCheck;[pscustomobject]@{Status=[int]$r.StatusCode;Path=$r.BaseResponse.RequestMessage.RequestUri.AbsolutePath}}
 $aParallel=Session;$aParallel.Cookies=$a.Cookies;$jobs=@()
 $jobs+=Start-ThreadJob -ScriptBlock $worker -ArgumentList ($BaseUrl+'/Checkout/AddressAndPayment'),$a,$body2
 $jobs+=Start-ThreadJob -ScriptBlock $worker -ArgumentList ($BaseUrl+'/Checkout/AddressAndPayment'),$aParallel,$body2
 try{$jobs|Wait-Job|Out-Null;$races=@($jobs|Receive-Job -ErrorAction Stop);Assert-Test (($races.Path|Select-Object -Unique).Count -eq 1 -and (OrderCount $idA)-eq 2) 'Concurrent duplicate submit creates exactly one additional order'}finally{$jobs|Remove-Job -Force}
 # Two different tickets for the same cart snapshot: only one can purchase it.
 Add $a ([int]$albums[3].AlbumId)|Out-Null
 $form1=Page '/Checkout/AddressAndPayment' $a;$form2=Page '/Checkout/AddressAndPayment' $a;$jobs=@()
 $jobs+=Start-ThreadJob -ScriptBlock $worker -ArgumentList ($BaseUrl+'/Checkout/AddressAndPayment'),$a,(CheckoutBody $form1 $emailA)
 $jobs+=Start-ThreadJob -ScriptBlock $worker -ArgumentList ($BaseUrl+'/Checkout/AddressAndPayment'),$aParallel,(CheckoutBody $form2 $emailA)
 try{$jobs|Wait-Job|Out-Null;$races=@($jobs|Receive-Job -ErrorAction Stop);Assert-Test ((OrderCount $idA)-eq 3 -and @($races|Where-Object Status -eq 409).Count -eq 1) 'Different concurrent forms cannot purchase the same cart twice'}finally{$jobs|Remove-Job -Force}
 # Persisted limit of 50 distinct products must not discard the next anonymous product.
 Add $c $album1|Out-Null;Register $c $emailC|Out-Null;Login $c $emailC|Out-Null;Empty $c|Out-Null
 $idC=Query MusicStoreAccounts 'SELECT Id FROM dbo.AspNetUsers WHERE Email=@email' @{email=$emailC}
 foreach($album in $albums|Select-Object -First 50){$p=Add $c ([int]$album.AlbumId);if([int]$p.StatusCode -ne 200){throw 'Cannot prepare merge limit test'}}
 $distinctGuest=Session;Add $distinctGuest ([int]$albums[50].AlbumId)|Out-Null;$p=Login $distinctGuest $emailC
 Assert-Test ($p.BaseResponse.RequestMessage.RequestUri.AbsolutePath -eq '/Account/Login' -and (Count (Cart $distinctGuest))-eq 1 -and (Count (Cart $c))-eq 50) 'Over-50 merge preserves both original carts without data loss'
 # An administrator has no special right to read someone else's order.
 & (Join-Path $PSScriptRoot 'AccountsAdmin.ps1') -Action promote -Email $emailC|Out-Null
 Login $c $emailC|Out-Null;$p=Page ('/Checkout/Complete/'+$order1) $c
 Assert-Test ([int]$p.StatusCode -eq 404) 'Administrator cannot view another user order'
 $p=Page '/Checkout/Orders' $c
 Assert-Test ([int]$p.StatusCode -eq 200 -and (ListedOrderIds $p) -notcontains $order1 -and (OrderCount $idC)-eq 0) 'Order history is filtered even for administrators'
 $after=Query MusicStore 'SELECT (SELECT AlbumId,Title,Price,ArtistId,GenreId,AlbumArtUrl FROM dbo.Albums WHERE Title NOT LIKE @prefix ORDER BY AlbumId FOR JSON PATH)' @{prefix=($tag+'%')}
 Assert-Test ($before -eq $after) 'Original catalog preserved exactly'
 $otherOrders=Query MusicStore 'SELECT COUNT(*) FROM dbo.Orders WHERE UserId NOT IN(SELECT Id FROM MusicStoreAccounts.dbo.AspNetUsers WHERE Email IN(@a,@b,@c))' @{a=$emailA;b=$emailB;c=$emailC}
 Assert-Test ($otherOrders -eq $originalOrders) 'Pre-existing orders preserved'
 Write-Host ('TOTAL: '+$passed+' passed')
}finally{
 Query MusicStore ("IF OBJECT_ID(N'dbo."+$triggerName+"',N'TR') IS NOT NULL DROP TRIGGER dbo."+$triggerName+';')|Out-Null
 Query MusicStore @'
DELETE d FROM dbo.OrderDetails d JOIN dbo.Orders o ON d.OrderId=o.OrderId JOIN MusicStoreAccounts.dbo.AspNetUsers u ON o.UserId=u.Id WHERE u.Email IN(@a,@b,@c);
DELETE o FROM dbo.Orders o JOIN MusicStoreAccounts.dbo.AspNetUsers u ON o.UserId=u.Id WHERE u.Email IN(@a,@b,@c);
DELETE c FROM dbo.Carts c JOIN dbo.UserCarts uc ON c.CartId=uc.CartId JOIN MusicStoreAccounts.dbo.AspNetUsers u ON uc.UserId=u.Id WHERE u.Email IN(@a,@b,@c);
DELETE uc FROM dbo.UserCarts uc JOIN MusicStoreAccounts.dbo.AspNetUsers u ON uc.UserId=u.Id WHERE u.Email IN(@a,@b,@c);
DELETE c FROM dbo.Carts c JOIN dbo.Albums a ON c.AlbumId=a.AlbumId WHERE a.Title LIKE @prefix;
DELETE FROM dbo.Albums WHERE Title LIKE @prefix;
DELETE FROM dbo.Artists WHERE Name=@tag;
DELETE FROM dbo.Genres WHERE Name=@tag;
'@ @{a=$emailA;b=$emailB;c=$emailC;prefix=($tag+'%');tag=$tag}|Out-Null
 Query MusicStoreAccounts 'DELETE ur FROM dbo.AspNetUserRoles ur JOIN dbo.AspNetUsers u ON ur.UserId=u.Id WHERE u.Email IN(@a,@b,@c);DELETE FROM dbo.AspNetUsers WHERE Email IN(@a,@b,@c);' @{a=$emailA;b=$emailB;c=$emailC}|Out-Null
}
