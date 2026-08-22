FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CloneEbay.Web/CloneEbay.Web.csproj", "CloneEbay.Web/"]
RUN dotnet restore "CloneEbay.Web/CloneEbay.Web.csproj"
COPY . .
RUN dotnet publish "CloneEbay.Web/CloneEbay.Web.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN adduser --disabled-password --home /app --gecos "" appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CloneEbay.Web.dll"]
