# Washu Todo

Washu Todo is a server-rendered task management application and foundational architecture blueprint built with .NET 10 and Blazor Interactive SSR. It provides task scheduling, goal dates, and state tracking backed by PostgreSQL, running entirely inside a single executable host.

## Tech Stack

* **Framework:** .NET 10 / ASP.NET Core (Blazor Interactive Server)
* **UI Suite:** RadzenBlazor
* **Database & ORM:** PostgreSQL via Entity Framework Core (`IDbContextFactory`)
* **Authentication:** ASP.NET Core Identity (External Google OAuth exclusively—no local password storage)
* **Architecture:** Central Package Management (`Directory.Packages.props`), shared infrastructure library

## Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/) (see `global.json`)
* A local or remote PostgreSQL database instance

## Local Setup

1. **Configure Secrets & Connection Strings:**
   Google OAuth is required for sign-in. Set your database connection string and Google credentials via `dotnet user-secrets`:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=washu_todo;Username=postgres;Password=your_password" --project Washu.Todo
dotnet user-secrets set "ExternalProviders:Google:ClientId" "your-google-client-id" --project Washu.Todo
dotnet user-secrets set "ExternalProviders:Google:ClientSecret" "your-google-client-secret" --project Washu.Todo

```


2. **Restore Tools & Packages:**
```bash
dotnet tool restore
dotnet restore

```


3. **Apply Database Migrations & Run:**
```bash
dotnet ef database update --project Washu.Todo
dotnet run --project Washu.Todo

```


The app will launch locally on `https://localhost:7062` and `http://localhost:5265`. Pending EF Core migrations apply on startup automatically.

## Project Structure

| Path | Purpose |
| --- | --- |
| `Washu.Todo` | Primary web application host, Blazor pages/components, domain models, and EF Core context. |
| `Washu.Framework` | Core shared infrastructure (Google OAuth integration, background channels, custom routing, and UI baseline). |
| `Directory.Packages.props` | Solution-wide Central Package Management configuration. |
| `dotnet-tools.json` | Manifest for local CLI tools (`dotnet-ef`). |

## Common CLI Commands

```bash
# Build the solution
dotnet build

# Apply EF Core migrations manually
dotnet ef database update --project Washu.Todo

# Run the app
dotnet run --project Washu.Todo

```

## Authentication Model

This application relies **exclusively on external Google OAuth**. There are no local user registration forms, password hashing routines, or JWT management pipelines. Ensure your Google Cloud Console project has `https://localhost:7062/signin-google` configured as an authorized redirect URI for local development.

# ⚠️WARNING
This project is still in beta and may contain issues. Use with caution