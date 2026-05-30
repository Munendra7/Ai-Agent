# AKS Manual Deployment Guide

This document explains the purpose of each YAML manifest in `infra/k8s/base` and provides a manual step-by-step plan to create an AKS cluster, push container images, and run the solution.

## 1. What each `infra/k8s/base` YAML file does

- `namespace.yaml`

  - Creates the Kubernetes namespace `ai-agent`.
  - Adds the label `azure.workload.identity/use: "true"`, which is required for Azure Workload Identity in AKS.
- `serviceaccount.yaml`

  - Creates a service account named `ai-agent-sa` in the `ai-agent` namespace.
  - Annotates it with `azure.workload.identity/client-id`, a placeholder for the Azure Managed Identity client ID.
  - The backend deployment uses this service account to access Azure Key Vault via workload identity.
- `secret-provider-class.yaml`

  - Configures the Secrets Store CSI driver for Azure Key Vault.
  - Defines an Azure Key Vault provider named `kv-ai-agent-secrets` in namespace `ai-agent`.
  - Maps Key Vault secrets into a Kubernetes secret named `app-secrets`.
  - Uses placeholders: `__WORKLOAD_IDENTITY_CLIENT_ID__`, `__KEYVAULT_NAME__`, and `__TENANT_ID__`.
- `configmap.yaml`

  - Creates `backend-config` with application settings for the backend service.
  - Includes runtime environment values, service URLs, JWT issuer and audience, and endpoint URLs for internal services.
- `backend.yaml`

  - Deploys the .NET backend service with 2 replicas.
  - Uses `ai-agent-sa` service account and attaches Key Vault secrets with CSI volume mount.
  - Reads configuration from `backend-config` and `app-secrets`.
  - Exposes a `backend-service` ClusterIP on port 80 targeting container port 8080.
- `frontend.yaml`

  - Deploys the frontend React/Vite application with 2 replicas.
  - Exposes `frontend-service` ClusterIP on port 80 targeting container port 5173.
- `docx.yaml`

  - Deploys the document conversion microservice as `docx-pdf-service`.
  - Exposes a ClusterIP service on port 3000.
- `ffmpeg.yaml`

  - Deploys the audio/video conversion microservice as `ffmpeg-node-service`.
  - Exposes a ClusterIP service on port 5000.
- `hpa.yaml`

  - Creates Horizontal Pod Autoscalers for backend and frontend.
  - Backend scales from 2 to 8 replicas based on 70% CPU utilization.
  - Frontend scales from 2 to 6 replicas based on 70% CPU utilization.
- `ingress.yaml`

  - Creates an Ingress resource named `ai-agent-ingress`.
  - Routes `/` traffic to `frontend-service` and `/api` to `backend-service`.
  - Uses `ingressClassName: azure-application-gateway`, so the cluster must be configured with Azure Application Gateway Ingress Controller.
- `kustomization.yaml`

  - Bundles all base manifests together.
  - Allows you to apply the whole stack via `kubectl apply -k infra/k8s/base`.

## 2. Manual plan to deploy this app on AKS

### Prerequisites

- Azure subscription with owner or contributor rights.
- Azure CLI installed and logged in.
- `kubectl` installed.
- `az aks` extension and Azure Key Vault provider tools if needed.
- Docker or another container build tool.
- Access to `docx-to-pdf-node-service`, `ffmpeg-node-service`, backend, and frontend source code.

### Step 1: Create Azure resource group and ACR

1. Create a resource group:

   ```bash
   az group create --name ai-agent-rg --location eastus
   ```
2. Create an Azure Container Registry:

   ```bash
   az acr create --resource-group ai-agent-rg --name aiagentacr --sku Standard
   ```
3. Log in to ACR:

   ```bash
   az acr login --name aiagentacr
   ```
4. Save the ACR login server:

   ```bash
   ACR_LOGIN_SERVER=$(az acr show --name aiagentacr --query loginServer -o tsv)
   echo $ACR_LOGIN_SERVER
   ```

### Step 2: Create the AKS cluster

1. Create an AKS cluster with managed identity and OIDC issuer enabled:

   ```bash
   az aks create \
     --resource-group ai-agent-rg \
     --name ai-agent-aks \
     --node-count 3 \
     --enable-managed-identity \
     --enable-oidc-issuer \
     --enable-azure-rbac \
     --attach-acr aiagentacr \
     --network-plugin azure \
     --vm-set-type VirtualMachineScaleSets
   ```
2. Connect kubectl to the cluster:

   ```bash
   az aks get-credentials --resource-group ai-agent-rg --name ai-agent-aks
   ```

### Step 3: Install AKS add-ons and dependencies

1. Install the Secrets Store CSI driver and Azure Key Vault provider.

   - If using Helm:
     ```bash
     helm repo add secrets-store-csi-driver https://kubernetes-sigs.github.io/secrets-store-csi-driver/charts
     helm repo update
     helm install csi-secrets-store secrets-store-csi-driver/secrets-store-csi-driver --namespace kube-system
     ```
   - Install Azure Key Vault provider:
     ```bash
     helm repo add azure-secrets-store-csi-driver-provider https://azure.github.io/secrets-store-csi-driver-provider-azure/charts
     helm repo update
     helm install csi-secrets-store-provider-azure azure-secrets-store-csi-driver-provider/csi-secrets-store-provider-azure --namespace kube-system
     ```
2. Install Application Gateway Ingress Controller (if you will use Azure Application Gateway):

   - Create or choose an Application Gateway.
   - Follow Azure docs to enable `azure-application-gateway` ingress class on AKS.
   - If not using AGIC, update `ingress.yaml` to use `nginx` or another ingress controller.

### Step 4: Create Azure Key Vault and secrets

1. Create Key Vault:

   ```bash
   az keyvault create --resource-group ai-agent-rg --name aiagent-kv --location eastus
   ```
2. Add secrets used by `secret-provider-class.yaml`:

   ```bash
   az keyvault secret set --vault-name aiagent-kv --name sql-connection-string --value "<your-sql-connection>"
   az keyvault secret set --vault-name aiagent-kv --name blob-connection-string --value "<your-blob-connection>"
   az keyvault secret set --vault-name aiagent-kv --name jwt-secret --value "<your-jwt-secret>"
   az keyvault secret set --vault-name aiagent-kv --name application-insights-connection-string --value "<your-appinsights-connection>"
   az keyvault secret set --vault-name aiagent-kv --name azure-openai-api-key --value "<your-azure-openai-key>"
   az keyvault secret set --vault-name aiagent-kv --name google-search-api-key --value "<your-google-search-key>"
   az keyvault secret set --vault-name aiagent-kv --name weather-api-key --value "<your-weather-key>"
   az keyvault secret set --vault-name aiagent-kv --name speech-to-text-subscription-key --value "<your-speech-key>"
   ```

### Step 5: Configure Azure workload identity for AKS

1. Create a user-assigned managed identity:

   ```bash
   az identity create --resource-group ai-agent-rg --name ai-agent-identity
   ```
2. Grant the identity access to Key Vault secrets:

   ```bash
   CLIENT_ID=$(az identity show --resource-group ai-agent-rg --name ai-agent-identity --query clientId -o tsv)
   PRINCIPAL_ID=$(az identity show --resource-group ai-agent-rg --name ai-agent-identity --query principalId -o tsv)
   az keyvault set-policy --name aiagent-kv --secret-permissions get list --object-id $PRINCIPAL_ID
   ```
3. Associate this identity with the AKS service account:

   - In AKS, annotate `ai-agent-sa` using the identity client ID.
   - The placeholder `__WORKLOAD_IDENTITY_CLIENT_ID__` in `serviceaccount.yaml` should be replaced with `$CLIENT_ID`.
4. If the cluster requires an issuer URL or OIDC setup, ensure it is enabled by the AKS create command.

### Step 6: Build and push container images

This repository includes four services:

- Backend: `backend/SemanticKernel.AIAgentBackend/SemanticKernel.AIAgentBackend.csproj`
- Frontend: `frontend/ai-agent`
- Docx PDF service: `docx-to-pdf-node-service`
- FFMPEG service: `ffmpeg-node-service`

1. Build the backend image:

   ```bash
   cd "d:/Code Files/Learning/Semantic kernel/AI-Agent/backend/SemanticKernel.AIAgentBackend"
   docker build -t $ACR_LOGIN_SERVER/ai-agent-backend:latest .
   ```
2. Build the frontend image:

   ```bash
   cd "d:/Code Files/Learning/Semantic kernel/AI-Agent/frontend/ai-agent"
   docker build -t $ACR_LOGIN_SERVER/ai-agent-frontend:latest .
   ```
3. Build the docx PDF service image:

   ```bash
   cd "d:/Code Files/Learning/Semantic kernel/AI-Agent/docx-to-pdf-node-service"
   docker build -t $ACR_LOGIN_SERVER/ai-agent-docx:latest .
   ```
4. Build the ffmpeg service image:

   ```bash
   cd "d:/Code Files/Learning/Semantic kernel/AI-Agent/ffmpeg-node-service"
   docker build -t $ACR_LOGIN_SERVER/ai-agent-ffmpeg:latest .
   ```
5. Push all images to ACR:

   ```bash
   docker push $ACR_LOGIN_SERVER/ai-agent-backend:latest
   docker push $ACR_LOGIN_SERVER/ai-agent-frontend:latest
   docker push $ACR_LOGIN_SERVER/ai-agent-docx:latest
   docker push $ACR_LOGIN_SERVER/ai-agent-ffmpeg:latest
   ```

### Step 7: Update YAML placeholders

Edit the following placeholders in the base manifests before applying them:

- `__ACR_LOGIN_SERVER__` → your ACR login server, e.g. `aiagentacr.azurecr.io`
- `__IMAGE_TAG__` → image tag, e.g. `latest`
- `__WORKLOAD_IDENTITY_CLIENT_ID__` → the user-assigned identity client ID
- `__KEYVAULT_NAME__` → your Key Vault name `aiagent-kv`
- `__TENANT_ID__` → your Azure tenant ID

Example replacements:

- `backend.yaml` backend image path
- `frontend.yaml` frontend image path
- `docx.yaml` docx service image path
- `ffmpeg.yaml` ffmpeg service image path
- `serviceaccount.yaml` workload identity client ID
- `secret-provider-class.yaml` identity client ID, key vault name, and tenant ID

### Step 8: Apply Kubernetes manifests

1. Create the namespace and resources via Kustomize:

   ```bash
   kubectl apply -k infra/k8s/base
   ```
2. Confirm the namespace exists:

   ```bash
   kubectl get namespaces
   ```
3. Check deployments and pods:

   ```bash
   kubectl get deployments -n ai-agent
   kubectl get pods -n ai-agent
   kubectl get services -n ai-agent
   kubectl get ingress -n ai-agent
   ```
4. Validate the secret mount and Key Vault integration:

   ```bash
   kubectl describe pod -n ai-agent <backend-pod-name>
   kubectl get secret app-secrets -n ai-agent
   ```

### Step 9: Validate the application

1. Confirm backend service is reachable inside the cluster:

   ```bash
   kubectl port-forward svc/backend-service 8080:80 -n ai-agent
   curl http://localhost:8080/api/health
   ```
2. Confirm frontend service is reachable:

   ```bash
   kubectl port-forward svc/frontend-service 5173:80 -n ai-agent
   curl http://localhost:5173/
   ```
3. Confirm ingress is working (replace with your public ingress host):

   ```bash
   kubectl get ingress -n ai-agent
   ```

### Optional adjustments

- If you do not want to use Azure Application Gateway:

  - Change `ingressClassName` in `ingress.yaml` to your chosen ingress controller.
  - Or expose `frontend-service` and `backend-service` as `LoadBalancer`.
- If the Key Vault CSI driver or workload identity is not ready, first verify the pods in `kube-system`.

## 3. Important notes

- `secret-provider-class.yaml` does not create the actual Key Vault secrets; you must create them manually in Azure Key Vault.
- `app-secrets` is created by the CSI driver when the pod starts and the Key Vault access succeeds.
- The backend accesses internal services via fixed URLs from the config map: `ffmpeg-node-service` and `docx-pdf-service`.
- The ingress routes `/api` traffic to the backend. Ensure the frontend knows the backend route if the app is configured to use relative paths.

## 4. Troubleshooting tips

- If pods stay in `Pending`, check ACR image pull credentials and node resources.
- If backend pod fails due to secret mount errors, verify the CSI driver and Key Vault permissions.
- If ingress is not created, check the ingress controller installation and `kubectl describe ingress ai-agent-ingress -n ai-agent`.
- Use `kubectl logs -n ai-agent <pod-name>` for application logs.

---

This guide is intended for manual end-to-end deployment of the AKS-based solution using the base YAML manifests in `infra/k8s/base`.
