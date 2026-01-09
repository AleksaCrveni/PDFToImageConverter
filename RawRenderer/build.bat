@echo off
mkdir builds
pushd builds
cl -FC -Zi ..\main.cpp user32.lib Gdi32.lib
popd