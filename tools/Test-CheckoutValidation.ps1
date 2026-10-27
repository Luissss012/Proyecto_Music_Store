# Run with Windows PowerShell after building MusicStore. No database access.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.ComponentModel.DataAnnotations
$bin = Join-Path $PSScriptRoot '..\MusicStore\bin'
[void][Reflection.Assembly]::LoadFrom((Join-Path $bin 'System.Web.Mvc.dll'))
[void][Reflection.Assembly]::LoadFrom((Join-Path $bin 'MusicStore.dll'))
$form = New-Object MusicStore.Models.CheckoutForm
$cases = @(
    @('FirstName', 'María-José', $true),
    @('LastName', "O'Connor", $true),
    @('FirstName', 'José Luis', $true),
    @('FirstName', '李明', $true),
    @('FirstName', 'Luis123', $false),
    @('LastName', '123', $false),
    @('FirstName', "Luis`nAndres", $false),
    @('FirstName', '---', $false),
    @('Address', '4ta calle, casa 12 zona 1', $true),
    @('Address', '12345', $false),
    @('City', '16 de Septiembre', $true),
    @('City', '123', $false),
    @('State', '', $true),
    @('Country', 'Guatemala1', $false),
    @('Phone', '+502 5555 0123', $true),
    @('Phone', '35279561', $true),
    @('Phone', '555+55555', $false),
    @('Phone', '1234567890123456', $false),
    @('PostalCode', '01001', $true),
    @('Email', 'not-an-email', $false)
)
foreach ($case in $cases) {
    $context = New-Object System.ComponentModel.DataAnnotations.ValidationContext($form)
    $context.MemberName = $case[0]
    $errors = New-Object 'System.Collections.Generic.List[System.ComponentModel.DataAnnotations.ValidationResult]'
    $valid = [System.ComponentModel.DataAnnotations.Validator]::TryValidateProperty($case[1], $context, $errors)
    if ($valid -ne $case[2]) { throw ('Unexpected validation result: ' + $case[0] + ' / ' + $case[1]) }
}
Write-Host ($cases.Count.ToString() + ' checkout validation cases passed.')
