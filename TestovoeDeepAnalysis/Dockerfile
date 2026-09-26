FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet restore TestovoeDeepAnalysis/TestovoeDeepAnalysis.csproj && dotnet build TestovoeDeepAnalysis/TestovoeDeepAnalysis.csproj && dotnet run --project TestovoeDeepAnalysis/TestovoeDeepAnalysis.csproj --no-build --urls http://0.0.0.0:8080"]