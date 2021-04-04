param(
    [Parameter(Position = 0)]
    [string]
    $Main = 'Program'
)

function Exec
{
    param(
        [Parameter(Mandatory, Position = 0)]
        [string]
        $Cmd,

        [Parameter(ValueFromRemainingArguments)]
        [string[]]
        $Arguments
    )

    $output = & $Cmd @Arguments

    if (-not $?)
    {
        Write-Host $output
        throw "Command '$Cmd $Arguments' failed"
    }
}

Exec dotnet clean
Exec dotnet build "/p:StartupObject=psapi.$Main"
dotnet run