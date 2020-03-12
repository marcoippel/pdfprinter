FROM mcr.microsoft.com/azure-functions/dotnet:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["PdfPrinter/PdfPrinter.csproj", "PdfPrinter/"]
RUN dotnet restore "PdfPrinter/PdfPrinter.csproj"
COPY . .
WORKDIR "/src/PdfPrinter"
RUN dotnet build "PdfPrinter.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PdfPrinter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/app