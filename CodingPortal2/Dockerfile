FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["CodingPortal2/CodingPortal2.csproj", "CodingPortal2/"]
RUN dotnet restore "CodingPortal2/CodingPortal2.csproj"
COPY . .
WORKDIR "/src/CodingPortal2"
RUN dotnet build "CodingPortal2.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CodingPortal2.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodingPortal2.dll"]
