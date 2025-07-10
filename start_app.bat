@echo off
cd /d %~dp0

start /B backend\backend.exe

timeout /t 2 /nobreak > NUL

start "" ML_mnist.exe