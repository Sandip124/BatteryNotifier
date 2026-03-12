#!/usr/bin/env bash
set -euo pipefail

# Usage: ./scripts/bump-version.sh <major|minor|patch> [--tag]
#
# Examples:
#   ./scripts/bump-version.sh patch          # 3.2.0 → 3.2.1
#   ./scripts/bump-version.sh minor          # 3.2.0 → 3.3.0
#   ./scripts/bump-version.sh major          # 3.2.0 → 4.0.0
#   ./scripts/bump-version.sh patch --tag    # bump + git commit + git tag
#
# Single source of truth: BatteryNotifier.Avalonia.csproj <Version>
# Info.plist is patched automatically at build time by MSBuild.
# Constants.ApplicationVersion reads from assembly metadata at runtime.

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
CSPROJ="$ROOT_DIR/BatteryNotifier.Avalonia/BatteryNotifier.Avalonia.csproj"

if [[ $# -lt 1 ]]; then
    echo "Usage: $0 <major|minor|patch> [--tag]"
    exit 1
fi

BUMP_TYPE="$1"
DO_TAG="${2:-}"

# Extract current version from .csproj
CURRENT=$(grep -oP '(?<=<Version>)[^<]+' "$CSPROJ")
if [[ -z "$CURRENT" ]]; then
    echo "Error: Could not find <Version> in $CSPROJ"
    exit 1
fi

IFS='.' read -r MAJOR MINOR PATCH <<< "$CURRENT"

case "$BUMP_TYPE" in
    major) MAJOR=$((MAJOR + 1)); MINOR=0; PATCH=0 ;;
    minor) MINOR=$((MINOR + 1)); PATCH=0 ;;
    patch) PATCH=$((PATCH + 1)) ;;
    *)
        echo "Error: Invalid bump type '$BUMP_TYPE'. Use major, minor, or patch."
        exit 1
        ;;
esac

NEW_VERSION="$MAJOR.$MINOR.$PATCH"
echo "Bumping version: $CURRENT → $NEW_VERSION"

# Update .csproj (the single source of truth)
sed -i'' -e "s|<Version>$CURRENT</Version>|<Version>$NEW_VERSION</Version>|" "$CSPROJ"
echo "  Updated: BatteryNotifier.Avalonia.csproj"

# Update Info.plist (also updated at build time, but keep in sync for git)
PLIST="$ROOT_DIR/BatteryNotifier.Avalonia/Info.plist"
if [[ -f "$PLIST" ]]; then
    sed -i'' -e "s|<string>$CURRENT</string>|<string>$NEW_VERSION</string>|g" "$PLIST"
    echo "  Updated: Info.plist"
fi

echo ""
echo "Version is now $NEW_VERSION"
echo "Constants.ApplicationVersion will read it from assembly metadata at runtime."

if [[ "$DO_TAG" == "--tag" ]]; then
    echo ""
    cd "$ROOT_DIR"
    git add "$CSPROJ" "$PLIST"
    git commit -m "release: v$NEW_VERSION"
    git tag "v$NEW_VERSION"
    echo "Created commit and tag: v$NEW_VERSION"
    echo "Run 'git push origin master --tags' to trigger the release workflow."
fi
