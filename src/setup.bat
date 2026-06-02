@echo off
echo ================================
echo  AutoWash Pro - Solution Setup
echo ================================

cd /d "%~dp0"

echo [1/6] Creating solution...
dotnet new sln -n AutoWash

echo [2/6] Creating projects...
dotnet new webapi -n API --output API
dotnet new classlib -n Application --output Application
dotnet new classlib -n Domain --output Domain
dotnet new classlib -n Infrastructure --output Infrastructure

echo [3/6] Adding projects to solution...
dotnet sln AutoWash.sln add API/API.csproj
dotnet sln AutoWash.sln add Application/Application.csproj
dotnet sln AutoWash.sln add Domain/Domain.csproj
dotnet sln AutoWash.sln add Infrastructure/Infrastructure.csproj

echo [4/6] Adding project references...
dotnet add API reference Application
dotnet add API reference Infrastructure
dotnet add Application reference Domain
dotnet add Infrastructure reference Application

echo [5/6] Installing NuGet packages...
dotnet add API package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add API package Swashbuckle.AspNetCore
dotnet add Infrastructure package Microsoft.EntityFrameworkCore
dotnet add Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add Infrastructure package BCrypt.Net-Next

echo [6/6] Restoring...
dotnet restore AutoWash.sln

echo ================================
echo  Done! Open AutoWash.sln in IDE
echo ================================
pause
