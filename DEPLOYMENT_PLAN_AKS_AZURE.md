# Azure AKS Deployment Plan for AI-Agent

## What this setup creates

- AKS with system + user node pools
- ACR for container images
- Key Vault for secrets with Workload Identity access
- Azure SQL + Storage Account
- Private Endpoints + Private DNS for SQL, Storage Blob, and Key Vault
- Application Gateway WAF v2 integrated with AKS ingress
- Azure monitoring stack:
  - Log Analytics
  - Azure Monitor Workspace (managed Prometheus)
  - Managed Grafana
  - Application Insights
  - Diagnostic settings on AKS, ACR, SQL, Key Vault
- Kubernetes manifests for frontend, backend, ffmpeg service, docx service, HPA, ingress, and Key Vault CSI
- GitHub Actions deploy and destroy pipelines

## Architecture flow

1. GitHub Actions authenticates to Azure via OIDC.
2. Terraform uses remote Azure backend state and provisions all resources.
3. AKS pulls images from ACR with `AcrPull` role.
4. Workload Identity (`ai-agent-sa`) accesses Key Vault secrets without pod-managed secrets.
5. Secrets Store CSI syncs Key Vault secrets to `app-secrets` Kubernetes secret.
6. App Gateway WAF ingress routes external traffic:
   - `/` -> frontend
   - `/api` -> backend
7. Observability:
   - Logs/diagnostics -> Log Analytics
   - Metrics -> Azure Monitor managed Prometheus
   - Dashboards -> Managed Grafana
   - Traces/requests/dependencies -> Application Insights + OpenTelemetry

## Best-practice follow-ups implemented

- Remote Terraform state backend enabled (`infra/terraform/backend.tf`) and workflow `terraform init` uses backend configs.
- Private networking implemented and enabled by default (`enable_private_networking = true`).
- WAF ingress implemented and enabled by default (`enable_waf_ingress = true`) via App Gateway + AKS integration.
- Placeholder secrets replaced with pipeline-driven Key Vault secret injection from GitHub secrets.
- .NET OpenTelemetry integration added for App Insights export.

## Files added/updated

- Terraform:
  - `infra/terraform/backend.tf`
  - `infra/terraform/backend.hcl.example`
  - `infra/terraform/private-endpoints.tf`
  - `infra/terraform/appgw-waf.tf`
  - plus updates across existing tf files
- Kubernetes:
  - updated `infra/k8s/base/ingress.yaml` to App Gateway class
  - updated `infra/k8s/base/secret-provider-class.yaml`
  - updated `infra/k8s/base/backend.yaml` env secret bindings
- CI/CD:
  - `.github/workflows/deploy-azure-aks.yml`
  - `.github/workflows/destroy-azure-infra.yml`
- Backend telemetry:
  - `backend/SemanticKernel.AIAgentBackend/SemanticKernel.AIAgentBackend/SemanticKernel.AIAgentBackend.csproj`
  - `backend/SemanticKernel.AIAgentBackend/SemanticKernel.AIAgentBackend/Program.cs`

## GitHub repository configuration

Create these repository secrets:

- Azure auth + infra:
  - `AZURE_CLIENT_ID`
  - `AZURE_TENANT_ID`
  - `AZURE_SUBSCRIPTION_ID`
  - `SQL_ADMIN_OBJECT_ID`
- Terraform remote state:
  - `TFSTATE_RESOURCE_GROUP`
  - `TFSTATE_STORAGE_ACCOUNT`
  - `TFSTATE_CONTAINER`
  - `TFSTATE_KEY`
- App runtime secrets:
  - `JWT_SECRET_VALUE`
  - `AZURE_OPENAI_API_KEY`
  - `GOOGLE_SEARCH_API_KEY`
  - `WEATHER_API_KEY`
  - `SPEECH_TO_TEXT_SUBSCRIPTION_KEY`

Also configure Azure Entra OIDC federated credential for this repo/workflow branch.

## Run setup (step-by-step)

1. Prepare remote state storage account/container (one-time).
2. Add all GitHub secrets listed above.
3. Optional local bootstrap:
   - copy `infra/terraform/terraform.tfvars.example` to `infra/terraform/terraform.tfvars`
   - set non-secret structural values (region, names, toggles).
4. Trigger `deploy-azure-aks` workflow (manual or push to `main`).
5. Pipeline actions:
   - Terraform init/apply (remote state)
   - Set Key Vault runtime secrets
   - Build and push all images to ACR
   - Render and apply k8s manifests
   - Wait for rollout success
6. Validate:
   - `kubectl get pods -n ai-agent`
   - check App Gateway public IP from Terraform output
   - check Azure Monitor, Grafana, and App Insights data flow

## Destroy on demand

1. Run workflow `destroy-azure-infra`.
2. Type exact confirmation value: `DESTROY`.
3. Workflow runs Terraform destroy against the same remote state.

## Summary

This setup now runs as secure-by-default production infra with private endpoints, WAF ingress, centralized observability, workload identity-based secret access, and full CI/CD + controlled teardown from GitHub Actions.
