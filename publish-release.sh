#!/usr/bin/env bash
# Publish Wendlemire Windows/macOS builds and upload them to GitHub Releases.
# Usage:
#   ./publish-release.sh
#   ./publish-release.sh --version 0.1 --platform all
#   ./publish-release.sh --version 0.1 --platform mac --skip-upload

set -euo pipefail

VERSION="0.1d"
PLATFORM="all"
SERVER_URL="http://5.78.232.9"
SKIP_UPLOAD=0

usage() {
  cat <<'EOF'
Usage: ./publish-release.sh [--version 0.1] [--platform all|windows|mac|current] [--server-url http://5.78.232.9] [--skip-upload]
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version)
      VERSION="${2:-}"
      shift 2
      ;;
    --platform)
      PLATFORM="${2:-}"
      shift 2
      ;;
    --server-url)
      SERVER_URL="${2:-}"
      shift 2
      ;;
    --skip-upload)
      SKIP_UPLOAD=1
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage
      exit 1
      ;;
  esac
done

if [[ ! "$PLATFORM" =~ ^(all|windows|mac|current)$ ]]; then
  echo "Invalid --platform: $PLATFORM" >&2
  usage
  exit 1
fi

PROJECT_ROOT="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$PROJECT_ROOT/Wendlemire/Wendlemire.Client.csproj"
RELEASE_DIR="$PROJECT_ROOT/RELEASE"
TAG="v$VERSION"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet is required on PATH." >&2
  exit 1
fi
if [[ "$SKIP_UPLOAD" -eq 0 ]] && ! command -v gh >/dev/null 2>&1; then
  echo "GitHub CLI (gh) is required on PATH, or pass --skip-upload." >&2
  exit 1
fi
if [[ ! -f "$PROJECT" ]]; then
  echo "Client project not found: $PROJECT" >&2
  exit 1
fi

targets=()

add_windows() { targets+=("win-x64|Windows x64"); }
add_mac() {
  targets+=("osx-arm64|macOS Apple Silicon")
  targets+=("osx-x64|macOS Intel")
}

case "$PLATFORM" in
  windows) add_windows ;;
  mac) add_mac ;;
  all)
    add_windows
    add_mac
    ;;
  current)
    uname_s="$(uname -s)"
    case "$uname_s" in
      Darwin) add_mac ;;
      MINGW*|MSYS*|CYGWIN*) add_windows ;;
      *)
        echo "Current OS is not Windows or macOS. Use --platform windows, mac, or all." >&2
        exit 1
        ;;
    esac
    ;;
esac

write_readme() {
  local publish_dir="$1"
  local rid="$2"

  if [[ "$rid" == win-* ]]; then
    cat > "$publish_dir/README.txt" <<EOF
Wendlemire $VERSION for Windows

Double-click Wendlemire.exe to play.

The client is already pointed at $SERVER_URL. You can change the Server field in the main menu, and toggle fullscreen there.

If Windows SmartScreen warns about an unknown app, choose More info > Run anyway.
EOF
  else
    cat > "$publish_dir/README.txt" <<EOF
Wendlemire $VERSION for macOS

From Terminal, in this folder:

  chmod +x Wendlemire
  ./Wendlemire

The client is already pointed at $SERVER_URL. You can change the Server field in the main menu, and toggle fullscreen there.

macOS may block unsigned apps. Allow it under System Settings > Privacy & Security, or run:

  xattr -cr .
  chmod +x Wendlemire
  ./Wendlemire
EOF
  fi
}

publish_target() {
  local rid="$1"
  local label="$2"
  local publish_dir="$RELEASE_DIR/build/$rid/Wendlemire"
  local zip_name="Wendlemire-$VERSION-$rid.zip"
  local zip_path="$RELEASE_DIR/$zip_name"

  echo
  echo "Publishing $label ($rid)..." >&2

  rm -rf "$RELEASE_DIR/build/$rid"

  dotnet publish "$PROJECT" \
    -c Release \
    -r "$rid" \
    --self-contained true \
    --nologo \
    -p:PublishSingleFile=false \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -o "$publish_dir"

  find "$publish_dir" -name '*.pdb' -delete
  write_readme "$publish_dir" "$rid"
  printf '{"ServerHost":"%s","FullScreen":true}' "${SERVER_URL%/}" > "$publish_dir/client.json"

  if [[ "$rid" == osx-* ]]; then
    chmod +x "$publish_dir/Wendlemire" || true
    find "$publish_dir" -name '*.dylib' -exec chmod +x {} \;
  fi

  rm -f "$zip_path"
  (
    cd "$RELEASE_DIR/build/$rid"
    if command -v zip >/dev/null 2>&1; then
      zip -r -y "$zip_path" Wendlemire >/dev/null
    else
      python3 - "$zip_path" <<'PY'
import sys, shutil
shutil.make_archive(sys.argv[1][:-4], "zip", ".", "Wendlemire")
PY
    fi
  )

  echo "Created $zip_name" >&2
  printf '%s\n' "$zip_path"
}

echo
echo "========================================"
echo "  Wendlemire $VERSION release"
echo "========================================"

mkdir -p "$RELEASE_DIR"
zips=()

for entry in "${targets[@]}"; do
  rid="${entry%%|*}"
  label="${entry#*|}"
  zip_path="$(publish_target "$rid" "$label" | tail -n 1)"
  zips+=("$zip_path")
done

if [[ "$SKIP_UPLOAD" -eq 0 ]]; then
  echo
  echo "Publishing GitHub release $TAG..."

  notes="$(cat <<EOF
Wendlemire $VERSION

Self-contained game builds. Unzip and run \`Wendlemire.exe\` on Windows, or \`./Wendlemire\` on macOS.
EOF
)"

  if gh release view "$TAG" >/dev/null 2>&1; then
    gh release upload "$TAG" "${zips[@]}" --clobber
    echo "Updated existing release $TAG"
  else
    gh release create "$TAG" "${zips[@]}" --title "Wendlemire $VERSION" --notes "$notes" --latest
    echo "Created GitHub release $TAG"
  fi

  echo
  echo "Release $TAG: https://github.com/zodeus/wendlewind/releases/tag/$TAG"
else
  echo
  echo "Skipped GitHub upload. Artifacts are in $RELEASE_DIR"
fi

echo
