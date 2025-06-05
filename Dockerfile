# 1. Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Projeyi kopyala ve restore et
COPY . . 
RUN dotnet restore ./BurnAnalysisApp.csproj

# Uygulamayı publish et
RUN dotnet publish ./BurnAnalysisApp.csproj -c Release -o out

# 2. Runtime aşaması
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/out .

# Uygulamayı çalıştır
ENTRYPOINT ["dotnet", "BurnAnalysisApp.dll"]
