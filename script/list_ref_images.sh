#!/bin/bash
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
REF_DIR="${ROOT_DIR}/ref"
OUTPUT_FILE="${ROOT_DIR}/dev/debug.txt"
if [[ ! -d "${REF_DIR}" ]]; then
  echo "ref directory not found: ${REF_DIR}" >&2
  exit 1
fi
mkdir -p "$(dirname "${OUTPUT_FILE}")"
find "${REF_DIR}" -type f \( \
  -iname '*.png' -o \
  -iname '*.jpg' \
\) -print0 |
while IFS= read -r -d '' file; do
  filename="$(basename "${file}")"
  echo "${filename%.*}"
done |
sort -u > "${OUTPUT_FILE}"
