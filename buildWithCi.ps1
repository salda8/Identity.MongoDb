dotnet build -c Release
Invoke-WebRequest -Uri 'https://codecov.io/bash' -OutFile codecov.sh
.\codecov.sh -f "coverage.xml"