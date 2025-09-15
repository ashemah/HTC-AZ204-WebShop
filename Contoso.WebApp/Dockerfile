FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /App
 
# Copy everything
COPY . ./
# Restore as distinct layers
ADD zscaler.crt /usr/local/share/ca-certificates/zscaler.crt
RUN chmod 644 /usr/local/share/ca-certificates/zscaler.crt && update-ca-certificates

RUN dotnet restore
# Build and publish a release
RUN dotnet publish -o out
 
# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build /App/out .
    ENTRYPOINT ["dotnet", "DotNet.Docker.dll"]
