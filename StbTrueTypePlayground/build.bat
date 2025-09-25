@echo off
mkdir build
pushd build
cl -FC -Zi ..\mainBasicExample.c
popd