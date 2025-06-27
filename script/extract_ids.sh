#!/bin/bash
INPUT_FILE="/media/arshtyi/Data/Program/Project/YuGiOh-Cards-Maker/res/other.json"
OUTPUT_FILE="/media/arshtyi/Data/Program/Project/YuGiOh-Cards-Maker/dev/debug.txt"
if command -v jq &> /dev/null; then
    jq -r 'keys[]' "$INPUT_FILE" > "$OUTPUT_FILE"
fi
