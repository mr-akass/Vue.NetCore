@echo off
setlocal

echo.
echo ==================== VOL Launcher - Select Environment ====================
echo.
echo 1. Development (appsettings.Development.json)
echo 2. Staging     (appsettings.Staging.json)
echo 3. Production  (appsettings.Production.json)
echo 4. Show current environment variable
echo.

set /p env_choice=Enter a number to select environment (default: 1):

if "%env_choice%"=="" set env_choice=1

if "%env_choice%"=="1" (
    set ASPNETCORE_ENVIRONMENT=Development
    echo [OK] Environment set to: Development
    echo      Config files: appsettings.json + appsettings.Development.json
) else if "%env_choice%"=="2" (
    set ASPNETCORE_ENVIRONMENT=Staging
    echo [OK] Environment set to: Staging
    echo      Config files: appsettings.json + appsettings.Staging.json
) else if "%env_choice%"=="3" (
    set ASPNETCORE_ENVIRONMENT=Production
    echo [OK] Environment set to: Production
    echo      Config files: appsettings.json + appsettings.Production.json
) else if "%env_choice%"=="4" (
    echo Current environment variable: %ASPNETCORE_ENVIRONMENT%
    if "%ASPNETCORE_ENVIRONMENT%"=="" (
        echo Environment variable not set, defaulting to Development
        set ASPNETCORE_ENVIRONMENT=Development
    )
) else (
    echo [WARNING] Invalid choice, defaulting to Development
    set ASPNETCORE_ENVIRONMENT=Development
)

echo.
echo ===============================================================

set "currentDir=%cd%"
echo Current directory: %currentDir%

:: Start frontend
set "webPath=%currentDir%\vol.web"
cd /d "%webPath%"
echo Starting frontend (vol.web)...
start cmd /k "npm run dev"

:: Start backend API (with environment variable)
set "apiPath=%currentDir%\vol.api.sqlsugar\VOL.WebApi"
cd /d "%apiPath%"
echo Starting backend API, environment: %ASPNETCORE_ENVIRONMENT%
start cmd /k "start_api_watch.bat %ASPNETCORE_ENVIRONMENT%"

echo.
echo [SUCCESS] Application started! Environment: %ASPNETCORE_ENVIRONMENT%
echo   - Frontend and backend are running in separate windows
echo   - The backend window prints its environment config on startup
echo.
echo Press Enter to exit....
pause

exit
