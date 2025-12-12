# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

# Copy solution and project files
COPY ["Ceiba.sln", "./"]
COPY ["src/Ceiba.Web/Ceiba.Web.csproj", "src/Ceiba.Web/"]
COPY ["src/Ceiba.Core/Ceiba.Core.csproj", "src/Ceiba.Core/"]
COPY ["src/Ceiba.Application/Ceiba.Application.csproj", "src/Ceiba.Application/"]
COPY ["src/Ceiba.Infrastructure/Ceiba.Infrastructure.csproj", "src/Ceiba.Infrastructure/"]
COPY ["src/Ceiba.Shared/Ceiba.Shared.csproj", "src/Ceiba.Shared/"]
COPY ["Ceiba.ServiceDefaults/Ceiba.ServiceDefaults.csproj", "Ceiba.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "Ceiba.sln"

# Copy only source directories (exclude sensitive files via .dockerignore)
# Note: .dockerignore must exclude: .git, .env*, secrets, **/bin, **/obj, etc.
COPY ["src/", "src/"]
COPY ["Ceiba.ServiceDefaults/", "Ceiba.ServiceDefaults/"]

# Build and publish
WORKDIR "/src/src/Ceiba.Web"
RUN dotnet publish "Ceiba.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app

# Install Pandoc for document conversion (RT-006 Mitigation)
# Using Ubuntu noble base image, so use apt instead of dnf
# --no-install-recommends avoids installing unnecessary packages
RUN apt-get update && \
    apt-get install -y --no-install-recommends pandoc && \
    pandoc --version && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Create non-root user for security
RUN groupadd -r ceiba && useradd -r -g ceiba ceiba
RUN chown -R ceiba:ceiba /app
USER ceiba

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Ceiba.Web.dll"]
