# Resumaire

Resumaire is a split application:

- `frontend/`: React 19 + TypeScript + Vite SPA
- `backend/`: ASP.NET Core 10 Web API
- `tests/`: backend integration tests
- `openspec/`: project specs, design, and task tracking

The UI ships light and dark themes. It follows the system preference by default; the theme toggle in the navigation rail overrides it and is remembered on that device.

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
```

AI credentials are configured by each signed-in user in **Settings → AI**. There is no shared backend API-key fallback. Existing `OpenAI:ApiKey`, `OpenAI:BaseUrl`, and `OpenAI:Model` developer settings are ignored by personal AI connections; remove those old secrets if they are no longer needed. See [Personal AI connections](#personal-ai-connections) below.

### 3b. Install Typst (PDF export)

Resume preview and PDF export are rendered with the [Typst](https://typst.app) CLI. The backend runs `typst compile` and needs the binary on `PATH` (or set `Typst:Executable` to its full path):

```powershell
winget install --id Typst.Typst
typst --version
```

On Linux or in a container, download the release binary from <https://github.com/typst/typst/releases> and put it on `PATH`. The template uses the bundled New Computer Modern font, so no font install is needed. The backend logs a clear error when the binary cannot be started, and export requests then return `502`.

Optional settings (defaults shown): `Typst:Executable=typst`, `Typst:TimeoutSeconds=20`, `Typst:MaxConcurrentRenders=2`, `Typst:CacheMinutes=10`.

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

## Personal AI connections

Open **Settings → AI**, select OpenAI or OpenCode Zen, enter your provider key, and connect to load models. Choose a supported model, explicitly test it, then save within ten minutes. Testing uses synthetic input and may incur a provider charge. Users pay their provider directly; no Resumaire-owned AI key is used.

The provider catalog is fetched automatically and cached for ten minutes per account and credential version. Only models supported by the Responses/structured-output integration are selectable. Unknown models are displayed as unsupported until their capabilities are added to `backend/Ai/AiProviderRegistry.cs`. A missing or forbidden catalog endpoint offers a curated fallback; a successful selected-model test verifies actual access. Discovery does not prove available credit or future quota.

Saved keys are encrypted and scoped to the authenticated account. The browser receives only their final four characters. Keys are never persisted in browser storage. Replacing a key requires a new test; removing it disables future AI calls while preserving manual tailoring. Contributor models disclose training use and require explicit acknowledgement before testing or saving. In-flight calls may complete after a key is removed; revoke a credential at the provider if it must stop working immediately.

Both tests and tailoring count against durable per-user limits: five attempts per UTC minute and fifty per UTC day by default. Failed/cancelled attempts count because providers can still charge them. One provider request per user can run at a time; an abandoned lease expires after 150 seconds. Limits survive restarts and credential replacement/removal. The separate short-term settings throttle permits twenty requests/minute per instance. There are no automatic paid retries.

Optional backend tuning: `OpenAI__MaxOutputTokens` (1000–16000, default 4000), `OpenAI__TimeoutSeconds` (5–120, default 60), `AiTailoring__RequestsPerMinute`, and `AiTailoring__RequestsPerDay`. Provider address, key and model come from the user's trusted connection, and temperature is omitted for model portability.

Structured contact fields, personal links, project URLs and certification identifiers are excluded from AI inputs. Professional resume text and job descriptions still go to the chosen provider, including anything users put into free-text fields. Requests set `store:false`; that does not override provider retention/training policies. Usage records contain operation, provider/model, result, duration and available token counts, not prompts or keys.

Authenticated settings API (standard `{ data, message }` response envelope):

| Endpoint | Purpose |
|---|---|
| `GET /api/settings/ai/` | Masked account connection and supported providers |
| `POST /api/settings/ai/models` | Discover models; body includes `providerId`, optional draft `apiKey`, and `refresh` |
| `POST /api/settings/ai/test` | Synthetic selected-model test; adds `modelId` and `trainingAcknowledged` when needed |
| `PUT /api/settings/ai/` | Save the tested selection; adds `validationToken` and current `expectedVersion` |
| `DELETE /api/settings/ai/key` | Remove the current account's connection |

An omitted draft key reuses the saved key only for the same provider. Test receipts bind the user, key fingerprint, provider and model. Settings responses use `Cache-Control: no-store`; errors include a safe `code` for actionable UI states.

## Hosting

See [deployment instructions](deploy/README.md) for the HTTPS frontend, .NET/Typst backend, PostgreSQL, migrations, protected persistent encryption keys, and backup/restore. Production frontend builds use the same-origin API by default; only set `VITE_API_BASE_URL` when deliberately hosting the API separately.

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
