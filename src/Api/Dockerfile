# -----------------------------------
# 1. フロントエンド（React）ビルド用ステージ
# -----------------------------------
FROM node:20 AS frontend-build
WORKDIR /src
# 依存関係のインストール（キャッシュを効かせるためpackage.jsonを先にコピー）
COPY src/Web/package*.json ./Web/
WORKDIR /src/Web
RUN npm ci

# Reactのソースコードをコピーしてビルド
COPY src/Web/ ./
RUN npm run build

# -----------------------------------
# 2. バックエンド（.NET）ビルド用ステージ
# -----------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src

# .NETの依存関係をリストア
COPY src/Api/*.csproj ./Api/
WORKDIR /src/Api
RUN dotnet restore

# .NETのソースコードをコピー
COPY src/Api/ ./
COPY --from=frontend-build /src/Web/dist ./wwwroot

# リリースビルド（wwwrootも含めて出力される）
RUN dotnet publish -c Release -o /app/out

# -----------------------------------
# 3. 実行用ステージ
# -----------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=backend-build /app/out .

# アプリの起動
ENTRYPOINT ["dotnet", "CloudNativeApp.dll"]
