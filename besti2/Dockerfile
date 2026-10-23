# The API image. Built from the repository root: docker build -f besti2/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Project files first: the restore is cached until a package changes
COPY besti2/besti2.csproj besti2/
COPY besti2.Application/besti2.Application.csproj besti2.Application/
COPY besti2.Domain/besti2.Domain.csproj besti2.Domain/
COPY besti2.Infrastructure/besti2.Infrastructure.csproj besti2.Infrastructure/
RUN dotnet restore besti2/besti2.csproj
COPY besti2/ besti2/
COPY besti2.Application/ besti2.Application/
COPY besti2.Domain/ besti2.Domain/
COPY besti2.Infrastructure/ besti2.Infrastructure/
RUN dotnet publish besti2/besti2.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Photos and the login keys live on volumes; the API runs as the image's non-root user "app"
RUN mkdir -p /app/uploads /keys && chown app:app /app/uploads /keys
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "besti2.dll"]
