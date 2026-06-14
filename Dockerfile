FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Talabat.sln", "./"]
COPY ["Talabat.Core/Talabat.Core.csproj", "Talabat.Core/"]
COPY ["Talabat.Repository/Talabat.Repository.csproj", "Talabat.Repository/"]
COPY ["Talabat.Service/Talabat.Service.csproj", "Talabat.Service/"]
COPY ["Talabat.APIs/Talabat.APIs.csproj", "Talabat.APIs/"]
COPY ["tests/Talabat.Tests/Talabat.Tests.csproj", "tests/Talabat.Tests/"]

RUN dotnet restore "Talabat.sln"

COPY . .
WORKDIR /src/Talabat.APIs
RUN dotnet publish "Talabat.APIs.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Talabat.APIs.dll"]
