Remove-Item drop -Force -Recurse -ErrorAction Ignore
dotnet pack src/yunit -c Release -o $PSScriptRoot/drop
dotnet test -c Release
dotnet test test/yunit.nuget.test -c NuGetTest
if (-not (Test-Path -Path "$PSScriptRoot\test\yunit.nuget.test\bin\Release\net6.0\foo")) {
    throw 'yunit.nuget.test failed'
}