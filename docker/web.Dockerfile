FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["CatshrediasNews.Client/CatshrediasNews.Client.csproj", "CatshrediasNews.Client/"]
RUN dotnet restore "CatshrediasNews.Client/CatshrediasNews.Client.csproj"

COPY . .
RUN dotnet publish "CatshrediasNews.Client/CatshrediasNews.Client.csproj" -c Release -o /app/publish

FROM nginx:stable-alpine AS final
WORKDIR /usr/share/nginx/html

COPY --from=build /app/publish/wwwroot ./
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
