#!/usr/bin/env bash
# CI_CMD unique (commands.ci) : appele par ci-branch.yml, qa et deploy. Memes etapes que release.yml.
# En local, dotnet est une install utilisateur hors PATH : detecte (Git Bash puis WSL) ou surcharger via DOTNET=.
set -euo pipefail
cd "$(dirname "$0")/.."

if [ -z "${DOTNET:-}" ]; then
  if command -v dotnet >/dev/null 2>&1; then DOTNET=dotnet
  else
    for c in "$HOME/AppData/Local/Microsoft/dotnet/dotnet.exe" /mnt/c/Users/*/AppData/Local/Microsoft/dotnet/dotnet.exe; do
      [ -x "$c" ] && { DOTNET="$c"; break; }
    done
  fi
fi
: "${DOTNET:?dotnet introuvable : definir DOTNET=}"
PYTHON="${PYTHON:-$(command -v python3 || command -v python)}"

echo "== Generation de la page de config"
"$PYTHON" gen_config_page.py

echo "== Build Release"
"$DOTNET" build src/EmbyBadges/EmbyBadges.csproj --configuration Release --output dist/

test -s dist/EmbyBadges.dll || { echo "dist/EmbyBadges.dll absent ou vide"; exit 1; }
echo "OK : dist/EmbyBadges.dll ($(wc -c < dist/EmbyBadges.dll) octets)"
