#Requires -Modules MicrosoftPowerBIMgmt.Reports
param(
    [Parameter(Mandatory)]
    [string]$Username,

    [Parameter(Mandatory)]
    [string]$Pwd,

    [Parameter(Mandatory)]
    [string]$Path,

    [Parameter]
    [string]$Name
)

$PBpwd = ConvertTo-SecureString $Pwd -AsPlainText -Force
$PBCred = New-Object System.Management.Automation.PSCredential ($Username, $PBpwd)

$reportName = $Path.Split(".")[0]
if($PSBoundParameters.ContainsKey("Name")) {
    $ReportName = $Name
}

Write-Verbose "Connectiong to PowerBI Service"
Connect-PowerBIServiceAccount -Credential $PBCred

Write-Verbose "Upload file"
New-PowerBIReport -Path $Path -Name $name -ConflictAction CreateOrOverwrite
