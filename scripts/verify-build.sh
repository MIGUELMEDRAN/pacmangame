#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Release}"

echo "==> dotnet --info"
dotnet --info

echo "==> dotnet restore"
dotnet restore

echo "==> dotnet build -c ${CONFIGURATION}"
BUILD_OUTPUT="$(dotnet build -c "${CONFIGURATION}" 2>&1)"
printf '%s\n' "$BUILD_OUTPUT"

BLOCKED_WARNINGS=(CS8600 CS8604 AVLN3001)
FOUND=()
for warning in "${BLOCKED_WARNINGS[@]}"; do
  if grep -q "$warning" <<<"$BUILD_OUTPUT"; then
    FOUND+=("$warning")
  fi
done

if [ "${#FOUND[@]}" -gt 0 ]; then
  echo "Build completed but blocked warnings were found: ${FOUND[*]}" >&2
  exit 1
fi

echo "Build verification passed without blocked warnings."
