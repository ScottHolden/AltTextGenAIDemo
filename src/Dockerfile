FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY "AltTextGenAI.csproj" .
RUN dotnet restore "AltTextGenAI.csproj"
COPY . .
RUN dotnet build "AltTextGenAI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AltTextGenAI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AltTextGenAI.dll"]