dotnet build -c Release
dotnet pack -c Release -p:PackageVersion=1.0.1 IdentityMongoDb/Identity.MongoDb.csproj  -o ./

nuget push *.nupkg -ApiKey pu8ntw4uin1t4g2s1tt7lm2e -Source https://ci.appveyor.com/nuget/salda8-0h4n5nbbutro/api/v2/package
nuget push *.nupkg -ApiKey o3yk865auhn9olmkxtmn2v49 -Source https://www.myget.org/F/salda/api/v2/package	

Invoke-WebRequest -Uri 'https://codecov.io/bash' -OutFile codecov.sh
.\codecov.sh -f "coverage.xml"