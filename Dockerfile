FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM base AS final
WORKDIR /app
COPY build .
ENTRYPOINT ["dotnet", "ShotYourPet.Timeline.dll"]