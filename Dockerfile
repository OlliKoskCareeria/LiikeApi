FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
USER app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Liike/Liike.csproj", "Liike/"]
COPY ["Liike.Application/Liike.Application.csproj", "Liike.Application/"]
COPY ["Liike.Domain/Liike.Domain.csproj", "Liike.Domain/"]
COPY ["Liike.Infrastructure/Liike.Infrastructure.csproj", "Liike.Infrastructure/"]
RUN dotnet restore "Liike/Liike.csproj"
COPY . .
WORKDIR /src/Liike
RUN dotnet publish "Liike.csproj" --configuration Release --output /app/publish --no-restore

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Liike.dll"]