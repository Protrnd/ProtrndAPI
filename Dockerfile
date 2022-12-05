FROM mcr.microsoft.com/dotnet/core/aspnet
LABEL author="Protrnd"

ENV ASPNETCORE_URLS=http://*:8080
ENV ASPNETCORE_ENVIRONMENT="production"

EXPOSE 8080
WORKDIR /app
COPY ./dist . 
ENTRYPOINT ["dotnet", "ProtrndWebAPI.dll"]