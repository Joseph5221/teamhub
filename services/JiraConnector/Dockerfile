FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["JiraConnector/JiraConnector.csproj", "JiraConnector/"]
RUN dotnet restore "JiraConnector/JiraConnector.csproj"
COPY . .
WORKDIR "/src/JiraConnector"
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "JiraConnector.dll"]