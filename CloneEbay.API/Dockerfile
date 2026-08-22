FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CloneEbay.Domain/CloneEbay.Domain.csproj", "CloneEbay.Domain/"]
COPY ["CloneEbay.Application/CloneEbay.Application.csproj", "CloneEbay.Application/"]
COPY ["CloneEbay.Infrastructure/CloneEbay.Infrastructure.csproj", "CloneEbay.Infrastructure/"]
COPY ["CloneEbay.API/CloneEbay.API.csproj", "CloneEbay.API/"]
RUN dotnet restore "CloneEbay.API/CloneEbay.API.csproj"
COPY . .
RUN dotnet publish "CloneEbay.API/CloneEbay.API.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN adduser --disabled-password --home /app --gecos "" appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CloneEbay.API.dll"]
