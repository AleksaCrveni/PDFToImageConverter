@echo off
mkdir build
pushd build
cl -FC -Zi ..\mainBasicExample.c
cl -FC -Zi ..\main.c
popd