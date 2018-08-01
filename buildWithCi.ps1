dotnet build
Invoke-WebRequest -Uri 'https://codecov.io/bash' -OutFile codecov.sh
.\codecov.sh -f "coverage.xml"