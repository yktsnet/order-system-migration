# 1. ビルド用ステージ
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# プロジェクトファイルをコピーしてリストア
COPY *.csproj ./
RUN dotnet restore

# 全ファイルをコピーしてリリースビルド
COPY . ./
RUN dotnet publish -c Release -o out

# 2. 実行用ステージ
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# アプリの起動
ENTRYPOINT ["dotnet", "CloudNativeApp.dll"]
