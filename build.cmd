@echo off
rem Builds a self-contained Windows x64 CueAudacity.exe into .\build
cd /d "%~dp0"
dotnet publish CueAudacity\CueAudacity.csproj -c Release -o build
