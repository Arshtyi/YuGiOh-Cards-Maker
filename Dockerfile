# Use the official .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0

# Install system dependencies
# jq is used by script/extract_all_ids.sh
# curl, tar, xz-utils are used by script/download_resources.sh
RUN apt-get update && apt-get install -y \
    jq \
    curl \
    tar \
    xz-utils \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy project files
COPY . .

# Ensure scripts are executable
RUN chmod +x YuGiOh-Cards-Maker.sh script/*.sh entrypoint.sh

# Create a non-root user for security
RUN useradd -m -u 1000 yugioh && \
    chown -R yugioh:yugioh /app

# Switch to non-root user
USER yugioh

# Set entrypoint
ENTRYPOINT ["./entrypoint.sh"]
