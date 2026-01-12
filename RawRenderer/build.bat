@echo off
mkdir builds
pushd builds
cl -FC -Zi ..\main.cpp user32.lib Gdi32.lib /EHsc
cl -FC -Zi ..\rasterizerVisualizer.cpp user32.lib Gdi32.lib /EHsc
popd