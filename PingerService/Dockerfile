FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
ARG TARGETARCH
WORKDIR /PingerService


#COPY *csproj .
# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore -a $TARGETARCH

# Build and publish a release
RUN dotnet publish -a $TARGETARCH --no-restore -o /app

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "PingerService.dll"]