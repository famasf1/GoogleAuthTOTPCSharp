@echo off
echo Testing Jekyll site locally...

REM Check if Ruby is installed
ruby --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Ruby is not installed. Please install Ruby first.
    echo Download from: https://rubyinstaller.org/
    pause
    exit /b 1
)

REM Check if bundler is installed
bundle --version >nul 2>&1
if %errorlevel% neq 0 (
    echo Installing bundler...
    gem install bundler
)

REM Install dependencies
echo Installing Jekyll dependencies...
bundle install

REM Build and serve the site
echo Starting Jekyll server...
echo Site will be available at: http://localhost:4000
echo Press Ctrl+C to stop the server
bundle exec jekyll serve --watch --drafts

pause