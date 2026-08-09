# Home Server Deployment

Deploy the app as two containers (**API** + **Web**) behind a **Caddy** reverse proxy, orchestrated
by Docker Compose. Caddy terminates TLS using a Cloudflare Origin Certificate so the app is served
over HTTPS. Because the images are built **from source inside Docker** (or pulled pre-built from
GHCR), the server needs only Docker — no .NET SDK, no Aspire tooling, and no manual publish step.

---

## Choose a deployment method

| Method | When to use |
| --- | --- |
| [Build from source](#build-from-source) | You want to deploy from a branch/commit, or customize the build |
| [Pull from GHCR](#deploy-from-pre-built-images-ghcr) | You want the fastest path — just pull and run |
| [Offline from tarballs](#offline-deployment-from-release-tarballs) | Server has no internet access; sideload images |

All three methods use the same `docker-compose.yml`, volumes, and Caddy configuration. They differ
only in how the container images are obtained.

---

## Prerequisites

The server can run any OS that supports Docker (Linux is recommended for a headless home server;
Windows and macOS work too). Install:

- **Docker Engine + Docker Compose v2** — the only required dependency.

  **Linux (Debian/Ubuntu):**

  ```bash
  curl -fsSL https://get.docker.com | sh
  sudo usermod -aG docker $USER   # then log out/in so `docker` runs without sudo
  ```

  This installs Docker Engine and the `docker compose` plugin. Verify with `docker --version`
  and `docker compose version`.

  **Windows / macOS:** install [Docker Desktop](https://www.docker.com/products/docker-desktop/),
  which includes Compose.

No other runtime is needed on the server — .NET, EF Core, and every NuGet package are compiled and
bundled into the images during the build.

---

## Common setup (all methods)

These steps apply regardless of how you obtain the images.

### 1. Configure authentication secrets

Create a `.env` file in the repository root on the server (next to `docker-compose.yml`):

```bash
cat > .env << 'EOF'
JWT_SIGNING_KEY=<a-random-string-at-least-32-characters>
GOOGLE_CLIENT_ID=<your-google-oauth-client-id>
GOOGLE_CLIENT_SECRET=<your-google-oauth-client-secret>
EOF
chmod 600 .env
```

Docker Compose automatically reads `.env` and substitutes the values into the service environment
variables. The containers will refuse to start if any of these are missing.

> **Important:** Never commit `.env` to source control — it is already in `.gitignore`. On the
> server, restrict file permissions (`chmod 600`) so only the deploying user can read it.

### 2. Set up the Cloudflare Origin Certificate

The domain `mealplanner.cameronmckay.ca` is managed by Cloudflare with proxy mode (orange cloud)
enabled. Cloudflare terminates TLS for visitors; the Origin Certificate secures the
Cloudflare → origin connection.

1. In the Cloudflare dashboard, go to **SSL/TLS → Origin Server → Create Certificate**.
2. Choose **RSA (2048)**, enter `mealplanner.cameronmckay.ca` as the hostname, and set the
   validity (up to 15 years).
3. Save the **certificate** as `certs/mealplanner.cameronmckay.ca.pem` and the **private key** as
   `certs/mealplanner.cameronmckay.ca.key` in the repository root on the server.

```bash
mkdir -p certs
# paste/copy the cert and key files into this directory
ls certs/
# cloudflare-origin-pull-ca.pem  mealplanner.cameronmckay.ca.key  mealplanner.cameronmckay.ca.pem
```

> The `certs/` directory is gitignored — private keys must never be committed.

Finally, enable **Authenticated Origin Pulls** in the Cloudflare dashboard so that only
Cloudflare can reach your origin:

1. Go to **SSL/TLS → Origin Server → Authenticated Origin Pulls**.
2. Toggle it **On**.

The repo includes Cloudflare's public Origin Pull CA certificate
(`certs/cloudflare-origin-pull-ca.pem`). Caddy is configured to require and verify a client
certificate signed by this CA on every TLS connection — requests that don't come through
Cloudflare are rejected at the handshake.

---

## Build from source

### Copy the repository to the server

The Docker build needs the **whole repository** (the build context is the repo root so the shared
`Directory.Build.props` / `Directory.Packages.props` and all projects are available during restore).

- **Git (recommended):** clone the repo directly on the server so updates are just `git pull`.

  ```bash
  git clone <your-repo-url> mealplanner
  cd mealplanner
  ```

- **SCP / rsync from your dev machine** (skip build output to keep the copy small):

  ```powershell
  # from the repository root on your PC (PowerShell)
  scp -r . <user>@<server-ip>:/home/<user>/mealplanner
  ```

  ```bash
  # or with rsync (Linux/macOS/WSL), excluding local build artifacts
  rsync -av --exclude 'bin/' --exclude 'obj/' --exclude 'data/mealplanner.db' \
    ./ <user>@<server-ip>:/home/<user>/mealplanner/
  ```

You do **not** need to copy `bin/`, `obj/`, or any locally built database — those are rebuilt or
created on the server. Named Docker volumes (not files in the repo) hold the live data.

### Build and run

From the repository root on the server:

```bash
docker compose up -d --build
```

This builds both images and starts the containers detached. To tag the build with a version number:

```bash
VERSION=1.2.3 docker compose up -d --build
```

### Update from source

To deploy a new version after pulling code changes, rebuild and recreate in place — the data
volumes are untouched:

```bash
git pull                          # or re-copy the repo
VERSION=1.2.3 docker compose up -d --build   # versioned build
```

---

## Deploy from pre-built images (GHCR)

Images are published to **GitHub Container Registry** on every
[release](https://github.com/cam96/MealPlanner/releases). Pull the latest images and start the
stack:

```bash
docker compose pull               # pulls ghcr.io/cam96/mealplanner-api:latest and mealplanner-web:latest
docker compose up -d              # starts containers from the pulled images
```

To pin a specific version:

```bash
VERSION=1.0.0 docker compose pull
docker compose up -d
```

### Update to the latest release

When a new release is published, pull the updated images and recreate the containers in place —
existing data volumes are preserved:

```bash
docker compose pull               # fetches the newest :latest images from GHCR
docker compose up -d              # recreates only containers whose image changed
```

This is zero-downtime for the data: named volumes (`mealplanner-data`, `mealplanner-backups`) are
**not** removed. The API applies any pending EF Core migrations on startup (after backing up the
database). If you want to force-recreate both containers even when the image hasn't changed:

```bash
docker compose up -d --force-recreate
```

---

## Offline deployment from release tarballs

Download the image tarballs and deployment files from a
[GitHub Release](https://github.com/cam96/MealPlanner/releases) for offline deployment:

```bash
gunzip MealPlanner-Api-1.0.0.tar.gz MealPlanner-Web-1.0.0.tar.gz
docker load -i MealPlanner-Api-1.0.0.tar
docker load -i MealPlanner-Web-1.0.0.tar
docker compose up -d
```

Each release also includes the latest `docker-compose.yml` and `Caddyfile`, so you can update
your deployment files without cloning the repo.

---

## After startup

On first start the API creates the database directory, applies EF Core migrations, and enables WAL
mode. The API mounts named volumes for the database (`/data`) and rotating pre-migration backups
(`/backups`) so your household data survives container rebuilds and updates; both services use
`restart: unless-stopped`, so they come back automatically after a reboot or crash.

Browse the app at **`https://mealplanner.cameronmckay.ca`** (Cloudflare proxied). Only **Caddy**
publishes a host port (443); the **Web** and **API** containers are internal-only on the Docker
network and cannot be reached directly from outside.

### Demo data

To load representative **demo data** on a fresh install, set `MealPlanner__SeedDemoData: "true"` on
the `api` service in [docker-compose.yml](../docker-compose.yml) before the first `up` (seeding
runs only when the database is empty). Leave it `"false"` to start clean.

### Canadian Nutrient File (optional)

If the CNF CSV files are present in `data/cnf/` at build time, they are bundled into the API image
so the deployment is fully self-contained. When absent, the image still builds and CNF search is
simply hidden in the UI.

---

## Manage and troubleshoot

```bash
docker compose ps                 # container status and health
docker compose logs -f            # follow logs (add a service name to scope: api / web)
docker compose down               # stop and remove containers (named volumes/data are kept)
```

### Versioning

The `VERSION` variable is baked into the published assemblies and displayed in the web UI (bottom of
the sidebar). Omit it for a local dev build (`0.0.0-dev`).

> `aspire publish` can also generate an equivalent Compose project; the checked-in
> [docker-compose.yml](../docker-compose.yml) is the maintained home-deploy artifact.
