FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["CatshrediasNewsAPI/CatshrediasNewsAPI.csproj", "CatshrediasNewsAPI/"]
RUN dotnet restore "CatshrediasNewsAPI/CatshrediasNewsAPI.csproj"

COPY . .
RUN dotnet publish "CatshrediasNewsAPI/CatshrediasNewsAPI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CatshrediasNewsAPI.dll"]
