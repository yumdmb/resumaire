# Resumaire

Resumaire is being built as a split application:

- `frontend/`: React 19 + TypeScript + Vite SPA
- `backend/`: ASP.NET Core 10 Web API

As of May 10, 2026, the backend targets `.NET 10` because it is the current stable LTS release.

## Prerequisites

- Node.js 22+
- npm 11+
- .NET SDK 10.0.x
- Docker Desktop or another local PostgreSQL option

## Repo Layout

- `frontend/` contains the SPA shell, routing, linting, and Vitest setup.
- `backend/` contains the ASP.NET Core API host and development OpenAPI endpoint.
- `openspec/` contains the change proposal, specs, design, and task tracking.
- `Resumaire.slnx` keeps the backend project in a solution for CLI workflows.

## Environment Setup

### Frontend

1. Copy `frontend/.env.example` to `frontend/.env.local`.
2. Set `VITE_API_BASE_URL` to the backend origin you want the SPA to call.

### Backend

`backend/.env.example` documents the environment variable names the API will expect as more features land. ASP.NET Core already reads environment variables, and the project is configured with a `UserSecretsId` for development secrets.

Recommended development flow:

```powershell
cd backend
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=resumaire;Username=postgres;Password=postgres"
dotnet user-secrets set "OpenAI:ApiKey" "<your-key>"
```

For non-secret local configuration you can also set variables in your shell before running `dotnet run`.

## Run Locally

### Frontend

```powershell
cd frontend
npm install
npm run dev
```

The Vite dev server runs on `http://localhost:5173` by default.

### Backend

```powershell
dotnet restore Resumaire.slnx
dotnet run --project backend
```

The ASP.NET Core launch profiles expose:

- `https://localhost:7263`
- `http://localhost:5194`

In development, OpenAPI is available at `https://localhost:7263/openapi/v1.json`.

### Database

The application does not consume PostgreSQL until task `2.2`, but you can start a compatible local database now:

```powershell
docker run --name resumaire-postgres `
  -e POSTGRES_DB=resumaire `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=postgres `
  -p 5432:5432 `
  -d postgres:17
```

Use the matching connection string from `backend/.env.example` or development user secrets.

## Quality Checks

### Frontend

```powershell
cd frontend
npm run lint
npm run test
npm run build
```

### Backend

There is not yet a dedicated backend test project. For the current setup phase, verify the backend with:

```powershell
dotnet build Resumaire.slnx
```

Backend integration tests are scheduled in task `2.5`.
