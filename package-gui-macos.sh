#!/bin/bash
set -euo pipefail

APP_NAME="Severity Beacon"
APP_BUNDLE_NAME="${APP_NAME}.app"
EXECUTABLE_NAME="SeverityBeacon.Gui"
RUNTIME_IDENTIFIER="${RUNTIME_IDENTIFIER:-osx-arm64}"
CONFIGURATION="${CONFIGURATION:-Release}"
PROJECT_PATH="SeverityBeacon.Gui/SeverityBeacon.Gui.csproj"
PUBLISH_DIR="SeverityBeacon.Gui/bin/${CONFIGURATION}/net8.0/${RUNTIME_IDENTIFIER}/publish"
DIST_DIR="dist"
APP_DIR="${DIST_DIR}/${APP_BUNDLE_NAME}"
CONTENTS_DIR="${APP_DIR}/Contents"
MACOS_DIR="${CONTENTS_DIR}/MacOS"
RESOURCES_DIR="${CONTENTS_DIR}/Resources"
ICON_SOURCE="SeverityBeacon.Gui/tray-roundal.png"
ICONSET_DIR="${DIST_DIR}/SeverityBeacon.iconset"
ICON_NAME="SeverityBeacon.icns"
TIFF_PATH="${DIST_DIR}/SeverityBeacon-icon.tiff"
ZIP_PATH="${DIST_DIR}/${APP_NAME// /-}-${RUNTIME_IDENTIFIER}.zip"

echo "-- Publishing ${APP_NAME} (${RUNTIME_IDENTIFIER}) --"
dotnet publish "${PROJECT_PATH}" \
  -c "${CONFIGURATION}" \
  -r "${RUNTIME_IDENTIFIER}" \
  --self-contained true \
  /p:PublishSingleFile=false \
  /p:UseAppHost=true

echo "-- Creating app bundle --"
rm -rf "${APP_DIR}" "${ICONSET_DIR}" "${ZIP_PATH}" "${TIFF_PATH}"
mkdir -p "${MACOS_DIR}" "${RESOURCES_DIR}"
cp -R "${PUBLISH_DIR}/." "${MACOS_DIR}/"

cat > "${CONTENTS_DIR}/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "https://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleDisplayName</key>
  <string>${APP_NAME}</string>
  <key>CFBundleExecutable</key>
  <string>${EXECUTABLE_NAME}</string>
  <key>CFBundleIconFile</key>
  <string>${ICON_NAME}</string>
  <key>CFBundleIdentifier</key>
  <string>com.sgeventservices.severitybeacon</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundleName</key>
  <string>${APP_NAME}</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>3.1</string>
  <key>CFBundleVersion</key>
  <string>3.1</string>
  <key>LSMinimumSystemVersion</key>
  <string>12.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

echo "APPL????" > "${CONTENTS_DIR}/PkgInfo"

if [[ -f "${ICON_SOURCE}" ]] && command -v sips >/dev/null 2>&1 && command -v tiff2icns >/dev/null 2>&1; then
  echo "-- Building app icon --"
  sips -s format tiff -z 1024 1024 "${ICON_SOURCE}" --out "${TIFF_PATH}" >/dev/null

  if tiff2icns "${TIFF_PATH}" "${RESOURCES_DIR}/${ICON_NAME}" >/dev/null 2>&1; then
    rm -f "${TIFF_PATH}"
  else
    echo "Icon conversion failed, continuing without .icns asset."
  fi
fi

chmod +x "${MACOS_DIR}/${EXECUTABLE_NAME}"

if command -v codesign >/dev/null 2>&1; then
  echo "-- Ad-hoc signing app bundle --"
  codesign --force --deep --sign - "${APP_DIR}" >/dev/null
fi

echo "-- Zipping app bundle --"
ditto -c -k --sequesterRsrc --keepParent "${APP_DIR}" "${ZIP_PATH}"

echo
echo "App bundle created:"
echo "  ${APP_DIR}"
echo
echo "Archive created:"
echo "  ${ZIP_PATH}"
