# Resumaire

Resumaire is a split application:

- `frontend/`: React 19 + TypeScript + Vite SPA
- `backend/`: ASP.NET Core 10 Web API
- `tests/`: backend integration tests
- `openspec/`: project specs, design, and task tracking

The app uses PostgreSQL for local development and ASP.NET Identity for authentication.

## Prerequisites

- Node.js 22+
- npm 11+
- .NET SDK 10.0.x
- Docker Desktop, or another PostgreSQL 17-compatible local database

If `dotnet ef` is not installed:

```powershell
dotnet tool install --global dotnet-ef
```

## First-Time Setup

Run this section once on a new machine or after cloning the repo. After this is done, use the shorter "Start Development Next Time" section.

### 1. Install frontend dependencies

```powershell
cd frontend
npm install
cd ..
```

### 2. Configure frontend API URL

Copy the frontend env example:

```powershell
Copy-Item frontend/.env.example frontend/.env.local
```

The default local API URL is:

```env
VITE_API_BASE_URL=http://localhost:5194
```

### 3. Configure backend secrets

```powershell
dotnet user-secrets set --project backend "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=resumaire;Username=postgres;Password=postgres"
dotnet user-secrets set --project backend "OpenAI:ApiKey" "<your-key>"
```

`OpenAI:ApiKey` is only needed once AI features are used.

### 4. Start PostgreSQL

If the `resumaire-postgres` container does not exist yet:

```powershell
docker run --name resumaire-postgres `
  -e POSTGRES_DB=resumaire `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=postgres `
  -p 5432:5432 `
  -d postgres:17
```

If the container already exists but is stopped:

```powershell
docker start resumaire-postgres
```

Verify the port is open:

```powershell
Test-NetConnection localhost -Port 5432
```

### 5. Apply backend migrations

```powershell
dotnet ef database update --project backend --startup-project backend
```

## Start Development Next Time

Run this section whenever you come back to work on the app after first-time setup is complete.

### 1. Start PostgreSQL

If the database container is already running, this command may say it is already started.

```powershell
docker start resumaire-postgres
```

### 2. Start the app

Use two terminals.

#### Terminal 1: Backend API

```powershell
dotnet run --project backend
```

#### Terminal 2: Frontend SPA

```powershell
cd frontend
npm run dev
```

Then open:

- `http://localhost:5173`

## Local URLs

### Backend API

The backend runs on:

- `http://localhost:5194`
- `https://localhost:7263`

Local endpoints:

- API status: `http://localhost:5194/`
- Health check: `http://localhost:5194/health`
- OpenAPI: `http://localhost:5194/openapi/v1.json`
- Auth endpoints: `http://localhost:5194/api/auth/*`

If you run only the HTTP profile and see `Failed to determine the https port for redirect`, it is not the database/login failure. The frontend is configured to use `http://localhost:5194` locally.

### Frontend SPA

The frontend runs on:

- `http://localhost:5173`

Open `http://localhost:5173` in the browser.

## Common Development Commands

### Frontend

```powershell
cd frontend
npm run dev        # start Vite dev server
npm test           # run Vitest once
npm run test:watch # run Vitest in watch mode
npm run lint       # run ESLint
npm run build      # type-check and build production assets
npm run preview    # preview the built frontend
```

### Backend

```powershell
dotnet restore Resumaire.slnx
dotnet build Resumaire.slnx
dotnet run --project backend
dotnet test Resumaire.slnx
dotnet ef database update --project backend --startup-project backend
```

## Troubleshooting

### Sign-in fails with `Failed to connect to 127.0.0.1:5432`

PostgreSQL is not running on the port configured in backend user secrets. Start the local database:

```powershell
docker start resumaire-postgres
```

Then verify:

```powershell
Test-NetConnection localhost -Port 5432
```

If the container does not exist, create it with the command in the PostgreSQL setup section.

### Database schema errors after starting Postgres

Apply migrations:

```powershell
dotnet ef database update --project backend --startup-project backend
```

### Frontend cannot reach the backend

Confirm the backend is running on `http://localhost:5194`, then check `frontend/.env.local`:

```env
VITE_API_BASE_URL=http://localhost:5194
```

Restart `npm run dev` after changing `.env.local`.
