FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY backend/src/Wishlist.Api.csproj backend/src/
RUN dotnet restore backend/src/Wishlist.Api.csproj

COPY backend/src backend/src
RUN dotnet publish backend/src/Wishlist.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Wishlist.Api.dll"]
