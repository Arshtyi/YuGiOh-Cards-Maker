#!/usr/bin/env bash
set -euo pipefail
cd /app || exit 1
if [ -f ./YuGiOh-Cards-Maker.sh ] && [ ! -x ./YuGiOh-Cards-Maker.sh ]; then
  chmod +x ./YuGiOh-Cards-Maker.sh || true
fi
if [ "$#" -eq 0 ]; then
  exec ./YuGiOh-Cards-Maker.sh
else
  exec ./YuGiOh-Cards-Maker.sh "$@"
fi
