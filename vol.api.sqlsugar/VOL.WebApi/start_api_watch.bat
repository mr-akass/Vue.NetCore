@echo off
set ASPNETCORE_ENVIRONMENT=%1

if "%ASPNETCORE_ENVIRONMENT%"=="" (
    set ASPNETCORE_ENVIRONMENT=Development
    echo [WARNING] No environment specified, defaulting to Development
)

echo ==================== API Starting (Watch Mode) ====================
echo Environment: %ASPNETCORE_ENVIRONMENT%
echo Config files: appsettings.json + appsettings.%ASPNETCORE_ENVIRONMENT%.json
echo ===============================================================
echo.

set ASPNETCORE_ENVIRONMENT=%ASPNETCORE_ENVIRONMENT%

echo Using launch profile: VOL.WebApi.%ASPNETCORE_ENVIRONMENT%
dotnet watch run --launch-profile VOL.WebApi.%ASPNETCORE_ENVIRONMENT% --no-hot-reload

if %ERRORLEVEL% neq 0 (
    echo.
    echo [FALLBACK] Launch profile not found, starting with environment variable only...
    set ASPNETCORE_ENVIRONMENT=%ASPNETCORE_ENVIRONMENT%
    dotnet watch run --no-hot-reload
)

pause
