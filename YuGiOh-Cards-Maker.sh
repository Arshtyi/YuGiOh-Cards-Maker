#!/bin/bash

set -e  # Exit on error

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored messages
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Check for debug flag
DEBUG_MODE=false
for arg in "$@"; do
    if [ "$(echo "$arg" | tr '[:upper:]' '[:lower:]')" == "--debug" ]; then
        DEBUG_MODE=true
        break
    fi
done

# Check C# environment
print_info "Checking C# environment..."

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    print_error "dotnet CLI is not installed. Please install .NET 8.0 SDK or later."
    exit 1
fi

# Check dotnet version
DOTNET_VERSION=$(dotnet --version)
print_info "Found .NET SDK version: $DOTNET_VERSION"

# Verify target framework from csproj
REQUIRED_FRAMEWORK=$(grep -oP '<TargetFramework>\K[^<]+' YuGiOh-Cards-Maker.csproj || echo "net8.0")
print_info "Required framework: $REQUIRED_FRAMEWORK"

# Check if the required SDK is available
if dotnet --list-sdks | grep -q "^8\."; then
    print_info "Compatible .NET SDK found"
else
    print_warning "Required .NET 8.0 SDK may not be installed"
fi

# Download resources
print_info "Checking and downloading resources..."
if [ -f "script/download_resources.sh" ]; then
    chmod +x script/download_resources.sh
    ./script/download_resources.sh
else
    print_error "script/download_resources.sh not found!"
    exit 1
fi

# Build the project
print_info "Building the C# project..."
if ! dotnet build -c Release; then
    print_error "Failed to build the project"
    exit 1
fi

# Run the C# program with optional parameters
print_info "Running the C# program..."
print_info "Arguments: $@"

if [ "$DEBUG_MODE" = true ]; then
    print_info "Debug mode enabled. Skipping cleanup of temporary files."
else
    print_info "Cleaning up..."
    rm -rf tmp/figure
    rm -rf obj
    rm -rf bin
    rm -rf asset
fi

print_info "Execution completed successfully!"

# Cleanup
print_info "Cleaning up..."
rm -rf tmp/figure
rm -rf obj
rm -rf bin
rm -rf asset

if [ -f "log/failure.txt" ] && [ ! -s "log/failure.txt" ]; then
    rm -rf log
fi
