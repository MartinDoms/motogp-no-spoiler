#!/bin/bash
source ~/.bashrc

pushd ~/src/motogp-no-spoiler

git pull origin master
#DOTNET_ROOT=/home/pi/dotnet-arm32/ /home/pi/dotnet-arm32/dotnet run

git add .
git commit -m "Automatic data update"
git push origin master

popd
