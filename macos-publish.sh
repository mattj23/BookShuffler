#!/bin/bash

dotnet publish -r osx-x64 --configuration Release -p:UseAppHost=true

APP_NAME="./publish/BookShuffler.app"
PUBLISH_OUTPUT_DIRECTORY="./BookShuffler/bin/Release/net5.0/osx-x64/publish/."
INFO_PLIST="./Info.plist"
ICON_FILE="./BookShuffler/Assets/avalonia-logo.ico"

if [ -d "$APP_NAME" ]
then
    rm -rf "$APP_NAME"
fi

mkdir "$APP_NAME"

mkdir "$APP_NAME/Contents"
mkdir "$APP_NAME/Contents/MacOS"
mkdir "$APP_NAME/Contents/Resources"

cp "$INFO_PLIST" "$APP_NAME/Contents/Info.plist"
cp "$ICON_FILE" "$APP_NAME/Contents/Resources/avalonia-logo.ico"
cp -a "$PUBLISH_OUTPUT_DIRECTORY" "$APP_NAME/Contents/MacOS"

cd publish
tar cvzf BookShuffler.app.tar.gz BookShuffler.app/
cd ..

