dotnet build
.\packages\OpenCover\tools\OpenCover.Console.exe -target:"C:\Program Files\dotnet\dotnet.exe" -targetargs:"test ./IdentityMongoDb.Tests" -output:".\coverage.xml" -filter:"+[Identity.MongoDb*]* -[Identity.MongoDb.Tests]*" -oldStyle -register:user

