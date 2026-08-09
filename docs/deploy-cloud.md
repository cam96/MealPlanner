# Cloud / VPS Deployment

MealPlanner is designed as a **home-network** app, but the same Docker Compose stack runs on any
cloud VM or VPS with minimal changes. This guide covers the differences from a
[home-server deployment](deploy-home-server.md).

---

## Overview

The Docker Compose stack (`docker-compose.yml` + `Caddyfile`) works on any Linux VM. The only
differences from a home-server setup are:

1. **No Cloudflare Origin Certificate** needed if you use a different TLS strategy.
2. **Caddy can auto-provision TLS** with Let's Encrypt instead of using an Origin Certificate.
3. **Firewall rules** must allow inbound 443 (and optionally 80 for HTTP→HTTPS redirect).

---

## Option A: Cloud VM with Cloudflare (same as home server)

If you put the VM behind Cloudflare (orange-cloud proxy), the deployment is **identical** to the
[home-server guide](deploy-home-server.md). Point your Cloudflare DNS at the VM's public IP and
follow the same Cloudflare Origin Certificate setup.

---

## Option B: Cloud VM with Let's Encrypt (automatic HTTPS)

If you're not using Cloudflare, Caddy can provision and renew TLS certificates automatically.

### 1. Set up the VM

Provision a Linux VM (any provider: DigitalOcean, Linode, Hetzner, Azure, AWS EC2, etc.) and
install Docker:

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
```

### 2. Point DNS at the VM

Create an **A record** for your domain (e.g. `meals.example.com`) pointing to the VM's public IP.
Wait for propagation.

### 3. Replace the Caddyfile

Replace the checked-in `Caddyfile` with one that uses automatic HTTPS:

```caddyfile
meals.example.com {
    reverse_proxy web:8080
}
```

Caddy will automatically obtain a Let's Encrypt certificate for the domain. No `tls` block or
certificate files are needed.

### 4. Expose port 80 and 443

Update `docker-compose.yml` to also publish port 80 (required for the ACME HTTP-01 challenge):

```yaml
caddy:
  ports:
    - "80:80"
    - "443:443"
```

### 5. Configure secrets and run

Follow the same `.env` setup from the [home-server guide](deploy-home-server.md#1-configure-authentication-secrets),
then:

```bash
docker compose up -d --build
```

---

## Security considerations for cloud

- **Firewall**: restrict inbound traffic to ports 80 and 443 only (use your cloud provider's
  security group or `ufw`).
- **SSH hardening**: use key-based auth, disable password login, change the default SSH port.
- **Updates**: keep the VM OS patched (`unattended-upgrades` on Debian/Ubuntu).
- **Backups**: the `mealplanner-data` volume holds the SQLite database. Snapshot the volume or
  copy the DB file to offsite storage on a schedule.

---

## Differences from home server

| Aspect | Home server | Cloud VM |
| --- | --- | --- |
| TLS | Cloudflare Origin Certificate | Let's Encrypt (auto) or Cloudflare |
| Network | LAN only (Cloudflare tunnel or port-forward) | Public IP, firewall rules |
| Access | Household only | Internet-accessible (add auth / IP allowlist if needed) |
| Cost | Free (existing hardware) | Monthly VM cost |

For most users of this app (a two-person household), the home-server deployment is recommended.
Cloud is useful if you don't have a server at home or want access when away without a VPN.
