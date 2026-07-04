param (
    [string]$version = "1.0.0",
    [string]$apikey = ""
)

dotnet pack src\AccountService.Contract\AccountService.Contract.csproj -c Release /property:Version=$version
dotnet pack src\AccountService.Client\AccountService.Client.csproj -c Release /property:Version=$version

if ($apikey -ne "") {
    dotnet nuget push bin\.Release\VKhil.AccountService.Contract.$version.nupkg --api-key $apikey --source https://api.nuget.org/v3/index.json
    dotnet nuget push bin\.Release\VKhil.AccountService.Client.$version.nupkg --api-key $apikey --source https://api.nuget.org/v3/index.json
}
