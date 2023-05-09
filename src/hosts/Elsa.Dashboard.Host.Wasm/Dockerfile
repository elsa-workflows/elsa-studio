FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/Elsa.Dashboard.WebAssemblyApp/Elsa.Dashboard.WebAssemblyApp.csproj", "src/Elsa.Dashboard.WebAssemblyApp/"]
RUN dotnet restore "src/Elsa.Dashboard.WebAssemblyApp/Elsa.Dashboard.WebAssemblyApp.csproj"
COPY . .
WORKDIR "/src/src/Elsa.Dashboard.WebAssemblyApp"
RUN dotnet build "Elsa.Dashboard.WebAssemblyApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Elsa.Dashboard.WebAssemblyApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Elsa.Dashboard.WebAssemblyApp.dll"]
