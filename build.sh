#bin/bash
rm -f $PWD/drop
dotnet pack src/yunit -c Release -o $PWD/drop
dotnet test -c Release
dotnet test test/yunit.nuget.test -c Release
if [ ! -f $PWD/test/yunit.nuget.test/bin/Release/netcoreapp3.0/foo ]; then
    echo "yunit.nuget.test failed" 1>&2
    exit 1
fi