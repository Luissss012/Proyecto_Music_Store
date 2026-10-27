# Adds fictional demo catalog only. Existing rows, orders, accounts and prices are preserved.
param([string]$Server='.\SQLEXPRESS')
$ErrorActionPreference='Stop'
$data=Get-Content (Join-Path $PSScriptRoot 'demo-catalog.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$connection=[System.Data.SqlClient.SqlConnection]::new('Data Source='+$Server+';Initial Catalog=MusicStore;Integrated Security=True;')
try {
 $connection.Open()
 $command=$connection.CreateCommand();$command.CommandText="SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'))"
 $backupFolder=[string]$command.ExecuteScalar()
 if(!$backupFolder){throw 'SQL Server no informa su carpeta de backups; no se importó nada.'}
 $stamp=Get-Date -Format 'yyyyMMdd_HHmmss_fff'
 foreach($database in @('MusicStore','MusicStoreAccounts')) {
  $path=Join-Path $backupFolder ($database+'_before_sprint8_'+$stamp+'.bak')
  $command=$connection.CreateCommand();$command.CommandTimeout=120
  $command.CommandText='BACKUP DATABASE ['+$database+'] TO DISK=@path WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM;'
  [void]$command.Parameters.AddWithValue('@path',$path);[void]$command.ExecuteNonQuery()
  Write-Host ('Backup verificado: '+$path)
 }
 $transaction=$connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
 try {
  $inserted=0
  foreach($row in $data) {
   $command=$connection.CreateCommand();$command.Transaction=$transaction
   $command.CommandText=@"
DECLARE @genreId int, @artistId int;
SELECT @genreId=GenreId FROM dbo.Genres WITH(UPDLOCK,HOLDLOCK) WHERE Name=@genre;
IF @genreId IS NULL BEGIN INSERT dbo.Genres(Name,Description) VALUES(@genre,N'Colección de demostración Music Store'); SET @genreId=SCOPE_IDENTITY(); END;
SELECT @artistId=ArtistId FROM dbo.Artists WITH(UPDLOCK,HOLDLOCK) WHERE Name=@artist;
IF @artistId IS NULL BEGIN INSERT dbo.Artists(Name) VALUES(@artist); SET @artistId=SCOPE_IDENTITY(); END;
IF NOT EXISTS(SELECT 1 FROM dbo.Albums WITH(UPDLOCK,HOLDLOCK) WHERE Title=@title AND ArtistId=@artistId)
BEGIN INSERT dbo.Albums(GenreId,ArtistId,Title,Price,AlbumArtUrl) VALUES(@genreId,@artistId,@title,@price,@cover); SELECT 1; END ELSE SELECT 0;
"@
   foreach($name in @('genre','artist','title','cover')){[void]$command.Parameters.AddWithValue('@'+$name,[string]$row.$name)}
   [void]$command.Parameters.AddWithValue('@price',[decimal]::Parse($row.price,[Globalization.CultureInfo]::InvariantCulture))
   $inserted += [int]$command.ExecuteScalar()
  }
  $transaction.Commit();Write-Host ('Álbumes agregados: '+$inserted+'; existentes conservados.')
 }catch{$transaction.Rollback();throw}finally{$transaction.Dispose()}
}finally{$connection.Dispose()}
