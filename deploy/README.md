# Hosting Resumaire

This deployment targets Linux x86-64 with Docker Compose. It includes nginx HTTPS and SPA routing, a .NET 10 backend with Typst 0.15.1, PostgreSQL 17, and an EF migration bundle. The database and backend have no published ports. The provided Docker build is x86-64 because its Typst binary and migration bundle are pinned to that architecture.

## Prepare secrets

Copy `.env.example` to `.env` in this directory and set `PUBLIC_ORIGIN` to your exact HTTPS origin. Set `DATABASE_CONNECTION_STRING` to use `database:5432`, database/user `resumaire`, and a strong password. Use that same password in `secrets/database-password.txt` (one line). Compose passes the connection string to the backend environment; restrict access to the host, Docker daemon and `.env`, or map these values from your hosting platform's secret manager.

Create `deploy/secrets/` with:

- `database-password.txt`: PostgreSQL password.
- `data-protection.pfx`: RSA certificate containing a private key, used only to encrypt the application key ring. The default Compose setup expects a PFX without a password; protect the secret file and host. For a password-protected PFX, inject `DataProtection__CertificatePassword` through your hosting secret manager.
- `tls-fullchain.pem` and `tls-private-key.pem`: TLS certificate chain/key for your public domain. Obtain these from your certificate service and arrange renewal, followed by an nginx reload.

For example, using OpenSSL locally:

```sh
mkdir -p secrets
openssl req -x509 -newkey rsa:3072 -sha256 -nodes -days 3650 -subj '/CN=Resumaire Data Protection' -keyout secrets/protection.key -out secrets/protection.crt
openssl pkcs12 -export -out secrets/data-protection.pfx -inkey secrets/protection.key -in secrets/protection.crt -passout pass:
chmod 700 secrets
chmod 600 secrets/database-password.txt secrets/protection.key secrets/tls-private-key.pem
```

Compose mounts the PFX read-only in the backend running as the .NET `app` user. Ensure the mounted PFX can be read by that container user (UID 1654); on Linux use the host's matching group/file ownership or a secret manager that grants the container access. Avoid making private keys publicly readable. The PFX and key-ring volume must both be backed up; losing either makes saved AI keys and authentication tokens unreadable. Do not replace the protecting certificate without a plan to keep old keys decryptable (retain old certificates and configure decryption for them or rewrap the key ring).

`.env`, `secrets/`, and local smoke-test files are ignored by Git and excluded from Docker build context. Never put real credentials in image layers or the frontend's `VITE_*` variables.

## Build and deploy

Run from this `deploy/` directory:

```sh
docker compose --env-file .env build
docker compose --env-file .env up -d
docker compose --env-file .env ps
```

The database becomes healthy, `migrate` applies pending migrations and exits, then the API starts. The frontend serves HTTPS on 443 and redirects HTTP on 80. Existing users must configure personal AI settings; old backend-owned keys are not imported. No AI provider key belongs in this deployment's environment.

Production requires `DataProtection__KeyPath` and `DataProtection__CertificatePath`; defaults in Compose use persistent `/keys` and a mounted secret. A stable application name keeps protected credentials readable across restarts. All backend replicas must share the key ring, certificate, and database. Development uses the normal local Data Protection store; protect/back up that store if you want local saved connections to survive a machine replacement.

nginx has the explicitly trusted private IP `172.30.0.10`, configured as `Proxy__KnownProxies__0`. Its network uses `172.30.0.0/24`; change both settings if that overlaps another host network. Never trust arbitrary forwarded headers. HTTPS terminates in nginx, which overwrites forwarded scheme/client headers. For managed hosting, adapt the proxy trust list and secret/key persistence to the platform. Set production CORS origins explicitly if separating frontend and backend.

After rebuilding for an update, apply the bundle again before replacing the API:

```sh
docker compose --env-file .env run --rm migrate
docker compose --env-file .env up -d backend frontend
```

Check `https://your-domain/health`, refresh `/settings/ai` directly, register/sign in, and use a synthetic connection test before a real resume. Restart the backend and verify the connection still works. Exercise manual editing and PDF export. Provider errors should reveal neither keys nor provider response bodies.

## Backups and recovery

Back up PostgreSQL with `pg_dump` and restore to an isolated database to verify recovery. Use a shell that preserves binary stream output if redirecting a custom-format dump (older Windows PowerShell versions do not). For example on Linux:

```sh
docker compose --env-file .env exec -T database pg_dump -U resumaire -d resumaire -Fc > resumaire.dump
```

Back up the `protection_keys` volume and protecting PFX separately from the database, encrypt backups, and restrict access. Restore the database, key ring, PFX and original application name together. TLS certificate renewal is independent from the Data Protection certificate. Deleting saved credentials does not erase copies in existing database backups; define backup retention accordingly.

Usage records contain metadata only. Account deletion cascades settings and usage rows. Establish an appropriate retention policy for usage metadata and backups before a public launch. This change keeps existing Identity bearer sessions; public account abuse controls and email verification are deployment/product decisions rather than a change to authentication here.
