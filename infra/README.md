# MealPlanner — Azure Infrastructure

Infrastructure-as-Code (Bicep) for deploying MealPlanner to Azure Container Apps.

## Architecture

```mermaid
flowchart TB
    subgraph Internet
        CF[Cloudflare CDN/WAF]
    end

    subgraph Azure["Azure (canadacentral)"]
        subgraph RG["Resource Group: rg-mealplanner-{env}"]
            subgraph VNet["VNet: vnet-mealplanner-{env}-cc"]
                subgraph CAE["Container Apps Environment: cae-mealplanner-{env}-cc"]
                    Web["ca-mealplanner-web-{env}<br/>(external ingress)"]
                    API["ca-mealplanner-api-{env}<br/>(internal ingress)"]
                end
            end
            ACR["crmealplanner<br/>(shared ACR)"]
            KV["kv-mealplanner-{env}<br/>(Key Vault)"]
            ST["stmealplanner{env}<br/>(Storage Account)"]
            LOG["log-mealplanner-{env}<br/>(Log Analytics)"]
        end
    end

    CF -->|HTTPS| Web
    Web -->|HTTP internal| API
    API -->|Azure Files| ST
    ACR -.->|image pull| Web
    ACR -.->|image pull| API
    KV -.->|secrets| Web
    KV -.->|secrets| API
    LOG -.->|logs| CAE
```

## Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (v2.60+)
- An Azure subscription
- Bicep CLI (bundled with Azure CLI)
- Docker images pushed to ACR (see [release workflow](../.github/workflows/release.yml))

## Environments

| Environment | Resource Group | Purpose |
|-------------|---------------|---------|
| `uat` | `rg-mealplanner-uat` | User acceptance testing |
| `prd` | `rg-mealplanner-prd` | Production |

## Deployment

### 1. Create the resource group

```bash
# UAT
az group create --name rg-mealplanner-uat --location canadacentral

# PRD
az group create --name rg-mealplanner-prd --location canadacentral
```

### 2. Deploy infrastructure

```bash
# UAT
az deployment group create \
  --resource-group rg-mealplanner-uat \
  --template-file infra/main.bicep \
  --parameters @infra/environments/uat.bicepparam \
  --parameters jwtSigningKey='<your-jwt-key>' \
               googleClientId='<your-google-client-id>' \
               googleClientSecret='<your-google-client-secret>' \
               apiImageTag='1.0.0' \
               webImageTag='1.0.0'

# PRD
az deployment group create \
  --resource-group rg-mealplanner-prd \
  --template-file infra/main.bicep \
  --parameters @infra/environments/prd.bicepparam \
  --parameters jwtSigningKey='<your-jwt-key>' \
               googleClientId='<your-google-client-id>' \
               googleClientSecret='<your-google-client-secret>' \
               apiImageTag='1.0.0' \
               webImageTag='1.0.0'
```

### 3. Push images to ACR

```bash
az acr login --name crmealplanner

docker tag mealplanner-api:1.0.0 crmealplanner.azurecr.io/mealplanner-api:1.0.0
docker tag mealplanner-web:1.0.0 crmealplanner.azurecr.io/mealplanner-web:1.0.0

docker push crmealplanner.azurecr.io/mealplanner-api:1.0.0
docker push crmealplanner.azurecr.io/mealplanner-web:1.0.0
```

### 4. Configure Cloudflare DNS

After deployment, the output `webFqdn` provides the external FQDN of the web container app.
Point your Cloudflare DNS CNAME record for `mealplanner.cameronmckay.ca` to this FQDN.

## Naming Convention

All resources follow the [Azure Cloud Adoption Framework naming convention](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming):

**Pattern**: `{abbreviation}-{workload}-{component?}-{environment}[-{region}]`

| Resource | Example (PRD) |
|----------|---------------|
| Resource group | `rg-mealplanner-prd` |
| Virtual network | `vnet-mealplanner-prd-cc` |
| Subnet | `snet-mealplanner-prd-cc` |
| Container Apps Environment | `cae-mealplanner-prd-cc` |
| Container App (API) | `ca-mealplanner-api-prd` |
| Container App (Web) | `ca-mealplanner-web-prd` |
| Container Registry | `crmealplanner` (shared) |
| Storage Account | `stmealplannerprd` |
| Key Vault | `kv-mealplanner-prd` |
| Log Analytics | `log-mealplanner-prd` |

## Tagging Strategy

All resources are tagged with:

| Tag | Description |
|-----|-------------|
| `app` | `mealplanner` |
| `environment` | `uat` or `prd` |
| `region` | `canadacentral` |
| `managedBy` | `bicep` |
| `repo` | `cam96/MealPlanner` |
| `createdBy` | Person/tool deploying |
| `createdDate` | `YYYY-MM-DD` |
| `purpose` | Per-resource description |

## Updating Deployments

To deploy a new version:

```bash
az deployment group create \
  --resource-group rg-mealplanner-uat \
  --template-file infra/main.bicep \
  --parameters @infra/environments/uat.bicepparam \
  --parameters apiImageTag='1.1.0' webImageTag='1.1.0' \
               jwtSigningKey='<key>' googleClientId='<id>' googleClientSecret='<secret>'
```

## Security Notes

- **Cloudflare IP restrictions**: The web container app only accepts traffic from Cloudflare IPv4
  ranges. All other traffic is denied. Update the IP list in `container-app-web.bicep` if
  Cloudflare publishes new ranges.
- **Managed Identity**: Container Apps use system-assigned managed identity for ACR pull
  (no stored credentials).
- **Key Vault**: Secrets are stored in Key Vault with RBAC access (Key Vault Secrets User role).
- **Internal API**: The API container app has internal-only ingress — it's not reachable from
  the internet.

## CI/CD Pipeline

Infrastructure changes are validated and deployed automatically via GitHub Actions.

### Pipeline Flow

```mermaid
flowchart LR
    PR["PR opened"] --> Lint["Bicep Lint"]
    PR --> Checkov["Security Scan"]
    PR --> Validate["Validate"]
    Validate --> WhatIfUAT["What-If UAT"]
    Validate --> WhatIfPRD["What-If PRD"]
    WhatIfUAT --> PRComment["PR Comment"]
    WhatIfPRD --> PRComment

    Merge["Merge to main"] --> Lint2["Lint + Validate"]
    Lint2 --> WhatIf2["What-If"]
    WhatIf2 --> DeployUAT["Deploy UAT"]
    DeployUAT --> Approval{"Manual Approval"}
    Approval --> DeployPRD["Deploy PRD"]
```

### Workflows

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `bicep-unit-tests.yml` | PR + push to main (infra changes) | Lint, validate, security scan (Checkov) |
| `bicep-whatif-deploy.yml` | PR + push to main (infra changes) | What-if preview, deploy UAT → PRD |

### PR Experience

When a PR modifies `infra/` files:
1. **Lint** — ensures Bicep compiles without warnings
2. **Validate** — confirms the template is valid against the real resource group
3. **Security Scan** — Checkov scans for misconfigurations (uploaded to GitHub Advanced Security)
4. **What-If** — previews changes for both UAT and PRD, posted as PR comments

### Deployment Strategy

On merge to `main`:
1. **UAT deploys automatically** (GitHub Environment: `uat`)
2. **PRD requires manual approval** (GitHub Environment: `prd` with protection rules)

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `AZURE_CLIENT_ID` | Service principal app/client ID (OIDC) |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Target Azure subscription |
| `UAT_JWT_SIGNING_KEY` | JWT key for UAT |
| `UAT_GOOGLE_CLIENT_ID` | Google OAuth client ID for UAT |
| `UAT_GOOGLE_CLIENT_SECRET` | Google OAuth client secret for UAT |
| `PRD_JWT_SIGNING_KEY` | JWT key for PRD |
| `PRD_GOOGLE_CLIENT_ID` | Google OAuth client ID for PRD |
| `PRD_GOOGLE_CLIENT_SECRET` | Google OAuth client secret for PRD |

### Required GitHub Environments

Configure in **Settings → Environments**:
- **`uat`** — no protection rules (auto-deploys on merge)
- **`prd`** — required reviewers (manual approval before deploy)

### Azure OIDC Setup

The workflows use OIDC (federated credentials) — no stored client secrets:

```bash
# Create federated credential for main branch
az ad app federated-credential create \
  --id <APP_OBJECT_ID> \
  --parameters '{
    "name": "github-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:cam96/MealPlanner:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for PRs
az ad app federated-credential create \
  --id <APP_OBJECT_ID> \
  --parameters '{
    "name": "github-pr",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:cam96/MealPlanner:pull_request",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for UAT environment
az ad app federated-credential create \
  --id <APP_OBJECT_ID> \
  --parameters '{
    "name": "github-env-uat",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:cam96/MealPlanner:environment:uat",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for PRD environment
az ad app federated-credential create \
  --id <APP_OBJECT_ID> \
  --parameters '{
    "name": "github-env-prd",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:cam96/MealPlanner:environment:prd",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

