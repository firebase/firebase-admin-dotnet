param(
    [String]$sdkVersion,
    [String]$nugetKey
)

[Environment]::SetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", 1)
[Environment]::SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", 1)

git clone https://github.com/firebase/firebase-admin-dotnet.git
cd firebase-admin-dotnet
dotnet pack -c Release FirebaseAdmin/FirebaseAdmin
cd FirebaseAdmin/FirebaseAdmin/bin/Release
echo "Pushing FirebaseAdmin.$sdkVersion.nupkg to nuget.org"
dotnet nuget push FirebaseAdmin.$sdkVersion.nupkg -k $nugetKey -s https://api.nuget.org/v3/index.json
