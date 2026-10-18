param([ValidateSet('setup','promote')][string]$Action = 'setup', [string]$Email)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$appBin = Join-Path $projectRoot 'MusicStore/bin'
$compiler = Join-Path $projectRoot 'packages/Microsoft.CodeDom.Providers.DotNetCompilerPlatform.2.0.1/tools/RoslynLatest/csc.exe'
if (!(Test-Path $compiler)) { $compiler = Join-Path $projectRoot 'MusicStore/bin/roslyn/csc.exe' }
$adminExe = Join-Path $appBin 'AccountsAdmin.exe'
$assemblyNames = @('MusicStore','EntityFramework','Microsoft.AspNet.Identity.Core','Microsoft.AspNet.Identity.EntityFramework')
$compileArgs = @('/nologo',('/out:' + $adminExe))
foreach ($assembly in $assemblyNames) { $compileArgs += '/reference:' + (Join-Path $appBin ($assembly + '.dll')) }
$compileArgs += Join-Path $PSScriptRoot 'AccountsAdmin.cs'
& $compiler @compileArgs
if ($LASTEXITCODE -ne 0) { throw 'No se pudo compilar la herramienta. Compila primero MusicStore.' }
Copy-Item (Join-Path $projectRoot 'MusicStore/Web.config') ($adminExe + '.config')
if ($Action -eq 'promote') {
    if ([string]::IsNullOrWhiteSpace($Email)) { $Email = Read-Host 'Correo de la cuenta registrada que será administradora' }
    & $adminExe promote $Email
} else { & $adminExe setup }
if ($LASTEXITCODE -ne 0) { throw 'La operación de cuentas no terminó correctamente.' }
