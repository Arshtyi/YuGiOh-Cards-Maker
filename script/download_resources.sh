#!/bin/bash

set -e  # Exit on error

# Get the script directory (assuming this script is in script/ folder)
# We need to go up one level to get to the project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
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

# Function to download file with checksum verification
download_and_verify() {
    local url=$1
    local checksum_url=$2
    local output_path=$3

    print_info "Downloading $(basename $url)..."

    # Download the file
    if ! curl -L -o "$output_path" "$url"; then
        print_error "Failed to download $url"
        return 1
    fi

    # Download checksum
    local checksum_file="${output_path}.sha256"
    if ! curl -L -o "$checksum_file" "$checksum_url"; then
        print_error "Failed to download checksum from $checksum_url"
        return 1
    fi

    # Verify checksum
    print_info "Verifying checksum for $(basename $output_path)..."
    cd "$(dirname "$output_path")"
    if sha256sum -c "$(basename "$checksum_file")" --ignore-missing; then
        print_info "Checksum verified for $(basename $output_path)"
        rm "$(basename "$checksum_file")"
        cd "$SCRIPT_DIR"
        return 0
    else
        print_error "Checksum verification failed for $(basename $output_path)"
        cd "$SCRIPT_DIR"
        return 1
    fi
}

# Create necessary directories
print_info "Creating necessary directories..."
mkdir -p asset
mkdir -p tmp/figure

# Download and extract card templates
TEMPLATE_URL="https://github.com/Arshtyi/Card-Templates-Of-YuGiOh/releases/download/1-11/yugioh-card-template.tar.xz"
TEMPLATE_CHECKSUM="https://github.com/Arshtyi/Card-Templates-Of-YuGiOh/releases/download/1-11/yugioh-card-template.tar.xz.sha256"
TEMPLATE_FILE="tmp/yugioh-card-template.tar.xz"

if [ ! -d "asset/yugioh-card-template" ] || [ ! "$(ls -A asset/yugioh-card-template 2>/dev/null)" ]; then
    download_and_verify "$TEMPLATE_URL" "$TEMPLATE_CHECKSUM" "$TEMPLATE_FILE"
    print_info "Extracting card templates to asset/..."
    tar -xf "$TEMPLATE_FILE" -C asset/
    rm "$TEMPLATE_FILE"
else
    print_info "Card templates already exist, skipping download..."
fi

# Download cards.json
CARDS_JSON_URL="https://github.com/Arshtyi/YuGiOh-Cards-Asset/releases/download/latest/cards.json"
CARDS_JSON_CHECKSUM="https://github.com/Arshtyi/YuGiOh-Cards-Asset/releases/download/latest/cards.json.sha256"
CARDS_JSON_FILE="tmp/cards.json"

if [ ! -f "$CARDS_JSON_FILE" ]; then
    download_and_verify "$CARDS_JSON_URL" "$CARDS_JSON_CHECKSUM" "$CARDS_JSON_FILE"
else
    print_info "cards.json already exists, skipping download..."
fi

# Download and extract card images (part 0)
IMAGES_0_URL="https://github.com/Arshtyi/YuGiOh-Cards-Asset/releases/download/latest/card-images-0.tar.xz"
IMAGES_0_CHECKSUM="https://github.com/Arshtyi/YuGiOh-Cards-Asset/releases/download/latest/card-images-0.tar.xz.sha256"
IMAGES_0_FILE="tmp/card-images-0.tar.xz"

if [ ! -f "$IMAGES_0_FILE" ] && [ -z "$(ls -A tmp/figure 2>/dev/null)" ]; then
    download_and_verify "$IMAGES_0_URL" "$IMAGES_0_CHECKSUM" "$IMAGES_0_FILE"
    print_info "Extracting card images (part 0) to tmp/figure/..."
    tar -xf "$IMAGES_0_FILE" -C tmp/figure/
    rm "$IMAGES_0_FILE"
else
    print_info "Card images (part 0) already downloaded or figure directory not empty, skipping..."
fi

# Download and extract card images (part 1)
IMAGES_1_URL="https://github.com/Arshtyi/YuGiOh-Cards-Asset/releases/download/latest/card-images-1.tar.xz"
IMAGES_1_CHECKSUM="https://github.com/Arshtyi/YuGiOh-Cards-Asset/releases/download/latest/card-images-1.tar.xz.sha256"
IMAGES_1_FILE="tmp/card-images-1.tar.xz"

if [ ! -f "$IMAGES_1_FILE" ]; then
    download_and_verify "$IMAGES_1_URL" "$IMAGES_1_CHECKSUM" "$IMAGES_1_FILE"
    print_info "Extracting card images (part 1) to tmp/figure/..."
    tar -xf "$IMAGES_1_FILE" -C tmp/figure/
    rm "$IMAGES_1_FILE"
else
    print_info "Card images (part 1) already downloaded, skipping..."
fi
