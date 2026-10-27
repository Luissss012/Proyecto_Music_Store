# Build and publish a local Release copy. Does not deploy to a remote server.
param([string]$MSBuild='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe')
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project=Join-Path $projectRoot 'MusicStore\MusicStore.csproj'
$output=Join-Path $projectRoot '.artifacts\sprint8\site'
if(!(Test-Path -LiteralPath $MSBuild)){throw 'Indica la ruta de MSBuild de tu Visual Studio con -MSBuild.'}
& $MSBuild $project /t:Build /p:Configuration=Release /p:MvcBuildViews=true /verbosity:minimal /nologo
if($LASTEXITCODE -ne 0){throw 'La compilación Release falló; no se publica.'}
& $MSBuild $project /t:WebPublish /p:Configuration=Release /p:WebPublishMethod=FileSystem ('/p:PublishUrl='+$output) /p:DeleteExistingFiles=false /verbosity:quiet /nologo
if($LASTEXITCODE -ne 0){throw 'La publicación local falló.'}
Write-Host ('Publicación Release: '+$output)
