@echo off
SETLOCAL

REM ===========================
REM GitHub Settings
REM ===========================
SET GITHUB_USER=VictorAlvesFarias
SET NUGET_TOKEN=
SET GITHUB_FEED_URL=https://nuget.pkg.github.com/%GITHUB_USER%/index.json
SET GITHUB_FEED_NAME=Glafyros

REM ===========================
REM 1. List existing NuGet feeds
REM ===========================
echo Listing current NuGet sources...
dotnet nuget list source
echo.

REM ===========================
REM 2. Remove old GitHub feeds
REM ===========================
echo Attempting to remove any old GitHub feeds...
dotnet nuget remove source "%GITHUB_FEED_NAME%"
echo.

REM ===========================
REM 3. Add GitHub feed again
REM ===========================
echo Adding GitHub feed...
dotnet nuget add source "%GITHUB_FEED_URL%" --name "%GITHUB_FEED_NAME%" --username "%GITHUB_USER%" --password "%NUGET_TOKEN%" --store-password-in-clear-text
echo Feed added successfully!
echo.

REM ===========================
REM 4. Restore packages
REM ===========================
echo Restoring packages...
dotnet restore --source "%GITHUB_FEED_URL%" --interactive
echo Packages restored successfully!

ENDLOCAL
pause
