#!/bin/bash
PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
INPUT_FILE="$PROJECT_ROOT/res/token.json"
OUTPUT_FILE="$PROJECT_ROOT/dev/debug.txt"
if command -v jq &> /dev/null; then
    jq -r 'keys[]' "$INPUT_FILE" > "$OUTPUT_FILE"
fi
