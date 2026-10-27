param([string]$BaseUrl='http://localhost:54834')
$ErrorActionPreference='Stop';$passed=0
function Assert-Test($condition,$label){if(!$condition){throw ('FAIL: '+$label)};$script:passed++;Write-Host ('PASS: '+$label)}
$session=[Microsoft.PowerShell.Commands.WebRequestSession]::new()
function Page($path){Invoke-WebRequest ($BaseUrl+$path) -WebSession $session -SkipHttpErrorCheck}
$landingPage=Page '/'
Assert-Test ([int]$landingPage.StatusCode -eq 200 -and $landingPage.Content.Contains('Hay un disco')) 'Home loads the new design'
Assert-Test ($landingPage.Content.Contains('data-global-cart-count') -and $landingPage.Content.Contains('data-global-cart-total')) 'Global summary shows count and total'
Assert-Test (($landingPage.Headers.'Cache-Control' -join ',').Contains('no-store')) 'Personal cart summary cannot be stored in shared caches'
Assert-Test ($landingPage.Content.Contains('aria-label="Géneros musicales"')) 'Global genre navigation has an accessible label'
Assert-Test ($landingPage.Content.Contains('lang="es"') -and $landingPage.Content.Contains('Saltar al contenido')) 'Spanish document and skip link'
$db=[System.Data.SqlClient.SqlConnection]::new('Data Source=.\SQLEXPRESS;Initial Catalog=MusicStore;Integrated Security=True;')
try {
 $db.Open();$cmd=$db.CreateCommand();$cmd.CommandText='SELECT TOP(6) a.AlbumId FROM dbo.Albums a JOIN dbo.OrderDetails d ON a.AlbumId=d.AlbumId GROUP BY a.AlbumId ORDER BY SUM(d.Quantity) DESC,a.AlbumId'
 $reader=$cmd.ExecuteReader();$expected=@();while($reader.Read()){$expected += [int]$reader[0]};$reader.Close()
 if($expected.Count -gt 0){
  $actual=@([regex]::Matches($landingPage.Content,'data-album-id="([0-9]+)"')|Select-Object -First $expected.Count|ForEach-Object{[int]$_.Groups[1].Value})
  Assert-Test (($expected -join ',') -eq ($actual -join ',')) 'Top selling uses summed quantities and deterministic tie breaks'
 }else{Assert-Test ($landingPage.Content.Contains('Recién llegados')) 'No fabricated bestseller ranking before first sale'}
 $cmd=$db.CreateCommand();$cmd.CommandText='SELECT Name FROM dbo.Genres ORDER BY Name';$reader=$cmd.ExecuteReader();$genres=@();while($reader.Read()){$genres += [string]$reader[0]};$reader.Close()
 foreach($genre in $genres){$p=Page ('/Store/Browse?genre='+[Uri]::EscapeDataString($genre));Assert-Test ([int]$p.StatusCode -eq 200) ('Genre loads: '+$genre)}
 foreach($row in (Get-Content (Join-Path $PSScriptRoot 'demo-catalog.json') -Raw -Encoding UTF8|ConvertFrom-Json)){
  $p=Page ($row.cover.Replace('~/','/'));Assert-Test ([int]$p.StatusCode -eq 200 -and $p.Content.Contains('<svg')) ('Local cover: '+$row.title)
 }
 foreach($path in @('/Store/Details/2147483647','/Store/Browse?genre=does-not-exist','/page-that-does-not-exist')){
  $p=Page $path;Assert-Test ([int]$p.StatusCode -eq 404 -and $p.Content.Contains('Volver a la colección') -and !$p.Content.Contains('Stack Trace')) ('Controlled 404: '+$path)
 }
 $p=Page '/Navigation/Genres';Assert-Test ([int]$p.StatusCode -ge 400 -and !$p.Content.Contains('InvalidOperationException')) 'Child action cannot expose internal exception details'
 $p=Page '/Account/Login';Assert-Test ($p.Content.Contains('data-global-cart-count') -and $p.Content.Contains('Géneros musicales')) 'Global navigation remains available on login'
 $p=Page '/Content/storefront.css';Assert-Test ([int]$p.StatusCode -eq 200 -and $p.Content.Contains('prefers-reduced-motion')) 'Responsive styles and reduced motion are served'
 Write-Host ('TOTAL: '+$passed+' passed')
}finally{$db.Dispose()}
