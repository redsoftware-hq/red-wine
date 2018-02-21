ApiKey=$1
Source=$2

nuget pack Red.Wine.csproj -properties Configuration=Release

nuget push ./Red.Wine.*.nupkg -ApiKey $ApiKey -Source $Source