# Runs every scenario (*.txt in this folder) in order and writes what the program printed to .\output.
# Scenarios 02 and 04 load the saves made by 01 and 03, so the saves file is cleared only once, at the start.
$solution = Split-Path $PSScriptRoot
$project = Join-Path $solution "ClassSystems.Cli"
$output = Join-Path $PSScriptRoot "output"

dotnet build $project -nologo -v q
New-Item -ItemType Directory -Force $output | Out-Null

Push-Location $output
try {
    Remove-Item saves.json, notes.md -ErrorAction SilentlyContinue
    foreach ($scenario in Get-ChildItem $PSScriptRoot -Filter *.txt | Sort-Object Name) {
        $result = Join-Path $output $scenario.Name
        cmd /c "dotnet run --project `"$project`" --no-build -- --data saves.json < `"$($scenario.FullName)`" > `"$result`""
        Write-Host "$($scenario.Name) -> output\$($scenario.Name)"
    }
}
finally {
    Pop-Location
}
