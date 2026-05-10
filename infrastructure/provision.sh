#!/bin/bash
set -euo pipefail

# ── Config ────────────────────────────────────────────────────────────────────
RESOURCE_GROUP="schoolms-rg"
LOCATION="canadacentral"
SUBSCRIPTION="e58781e6-1114-43ea-ba2e-1671d7aad932"

APP_NAME="schoolms-api"
SWA_NAME="schoolms-frontend"
APP_SERVICE_PLAN="schoolms-plan"

DB_SERVER="schoolms-db"
DB_NAME="schoolms"
DB_USER="schoolms_admin"

REDIS_NAME="schoolms-cache"
STORAGE_NAME="schoolmsfiles"
KEYVAULT_NAME="schoolms-kv"
APP_INSIGHTS_NAME="schoolms-insights"
LOG_ANALYTICS="schoolms-logs"
SWA_LOCATION="eastus2"             # Static Web Apps not available in canadacentral

KV_SCOPE="/subscriptions/$SUBSCRIPTION/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEYVAULT_NAME"
KV_URI="https://$KEYVAULT_NAME.vault.azure.net/"

step() { echo; echo "── $1"; }
ok()   { echo "   ✓ $1"; }
skip() { echo "   · $1 already exists, skipping"; }

echo "──────────────────────────────────────────────────────"
echo "  SchoolMS — Azure Provisioning"
echo "  Resource Group : $RESOURCE_GROUP"
echo "  Location        : $LOCATION"
echo "──────────────────────────────────────────────────────"

az account show > /dev/null 2>&1 || { echo "Run 'az login' first."; exit 1; }
az account set --subscription "$SUBSCRIPTION"

# ── Fetch values from already-provisioned resources ───────────────────────────
# Steps 1-6 are already done. We only read their output values here.
step "Reading existing resource values..."

APP_INSIGHTS_CONN=$(az monitor app-insights component show \
  --app "$APP_INSIGHTS_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query connectionString -o tsv)
ok "App Insights connection string"

# Retrieve DB password — stored in Key Vault from first run
DB_PASS=$(az keyvault secret show \
  --vault-name "$KEYVAULT_NAME" \
  --name "Db--Password" \
  --query value -o tsv 2>/dev/null || echo "")

if [ -z "$DB_PASS" ]; then
  # Not in Key Vault yet — ask for the existing Postgres admin password
  # (generating a new one here would not match the already-provisioned server)
  echo ""
  echo "   ⚠  DB password not found in Key Vault."
  echo "      Enter the PostgreSQL admin password for '$DB_USER' on '$DB_SERVER':"
  read -rs DB_PASS
  echo ""
  if [ -z "$DB_PASS" ]; then
    echo "ERROR: DB password cannot be empty." >&2
    exit 1
  fi
else
  ok "DB password retrieved from Key Vault"
fi

DB_CONN="Host=$DB_SERVER.postgres.database.azure.com;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASS;SslMode=Require"

REDIS_KEY=$(az redis list-keys \
  --resource-group "$RESOURCE_GROUP" \
  --name "$REDIS_NAME" \
  --query primaryKey -o tsv)
REDIS_CONN="$REDIS_NAME.redis.cache.windows.net:6380,password=$REDIS_KEY,ssl=True,abortConnect=False"
ok "Redis key"

STORAGE_CONN=$(az storage account show-connection-string \
  --resource-group "$RESOURCE_GROUP" \
  --name "$STORAGE_NAME" \
  --query connectionString -o tsv)
STORAGE_BASE_URL="https://$STORAGE_NAME.blob.core.windows.net"
ok "Storage connection string"

# ── [7/12] Key Vault ──────────────────────────────────────────────────────────
step "[7/12] Key Vault"
if ! az keyvault show \
     --resource-group "$RESOURCE_GROUP" \
     --name "$KEYVAULT_NAME" \
     --output none 2>/dev/null; then
  az keyvault create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$KEYVAULT_NAME" \
    --location "$LOCATION" \
    --sku standard \
    --output none
  ok "Key Vault created"
else
  skip "Key Vault"
fi

# Grant current CLI user Secrets Officer access.
# az role assignment create has a Python bug in Azure CLI where it crashes with
# IndexError instead of returning a clean error when the assignment already exists.
# We suppress both stdout and stderr, then only re-raise if it's not an exists error.
MY_OID=$(az ad signed-in-user show --query id -o tsv)
set +e
ROLE_OUT=$(az role assignment create \
  --role "Key Vault Secrets Officer" \
  --assignee-object-id "$MY_OID" \
  --assignee-principal-type User \
  --scope "$KV_SCOPE" \
  --output none 2>&1)
ROLE_EXIT=$?
set -e
if [ $ROLE_EXIT -ne 0 ] && ! echo "$ROLE_OUT" | grep -q "RoleAssignmentExists"; then
  echo "$ROLE_OUT" >&2
  exit 1
fi
ok "Key Vault Secrets Officer role assigned (or already existed)"

# Preserve JWT secret across re-runs
JWT_SECRET=$(az keyvault secret show \
  --vault-name "$KEYVAULT_NAME" \
  --name "Jwt--Secret" \
  --query value -o tsv 2>/dev/null \
  || openssl rand -base64 64 | tr -d '\n')

az keyvault secret set --vault-name "$KEYVAULT_NAME" --name "Db--Password"                          --value "$DB_PASS"           --output none
az keyvault secret set --vault-name "$KEYVAULT_NAME" --name "ConnectionStrings--Default"             --value "$DB_CONN"           --output none
az keyvault secret set --vault-name "$KEYVAULT_NAME" --name "Redis--Connection"                      --value "$REDIS_CONN"        --output none
az keyvault secret set --vault-name "$KEYVAULT_NAME" --name "Jwt--Secret"                            --value "$JWT_SECRET"        --output none
az keyvault secret set --vault-name "$KEYVAULT_NAME" --name "AzureStorage--ConnectionString"         --value "$STORAGE_CONN"      --output none
az keyvault secret set --vault-name "$KEYVAULT_NAME" --name "AzureStorage--BaseUrl"                  --value "$STORAGE_BASE_URL"  --output none
az keyvault secret set --vault-name "$KEYVAULT_NAME" --name "ApplicationInsights--ConnectionString"  --value "$APP_INSIGHTS_CONN" --output none
ok "All secrets stored in Key Vault"

# ── [8/12] App Service Plan ───────────────────────────────────────────────────
step "[8/12] App Service Plan"
if ! az appservice plan show \
     --resource-group "$RESOURCE_GROUP" \
     --name "$APP_SERVICE_PLAN" \
     --output none 2>/dev/null; then
  az appservice plan create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$APP_SERVICE_PLAN" \
    --location "$LOCATION" \
    --sku P1V3 \
    --is-linux \
    --output none
  ok "App Service Plan created"
else
  skip "App Service Plan"
fi

# ── [9/12] App Service (API) ─────────────────────────────────────────────────
step "[9/12] App Service (API)"
if ! az webapp show \
     --resource-group "$RESOURCE_GROUP" \
     --name "$APP_NAME" \
     --output none 2>/dev/null; then
  az webapp create \
    --resource-group "$RESOURCE_GROUP" \
    --plan "$APP_SERVICE_PLAN" \
    --name "$APP_NAME" \
    --runtime "DOTNETCORE:10.0" \
    --output none
  ok "App Service created"
else
  skip "App Service"
fi

# Enable managed identity (idempotent)
az webapp identity assign \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --output none

APP_IDENTITY=$(az webapp identity show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --query principalId -o tsv)

# Grant managed identity read access to Key Vault via RBAC.
# The vault uses --enable-rbac-authorization, so set-policy is not allowed.
# "Key Vault Secrets User" grants get + list on secrets.
set +e
MI_ROLE_OUT=$(az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee-object-id "$APP_IDENTITY" \
  --assignee-principal-type ServicePrincipal \
  --scope "$KV_SCOPE" \
  --output none 2>&1)
MI_ROLE_EXIT=$?
set -e
if [ $MI_ROLE_EXIT -ne 0 ] && ! echo "$MI_ROLE_OUT" | grep -q "RoleAssignmentExists"; then
  echo "$MI_ROLE_OUT" >&2
  exit 1
fi
ok "Managed identity granted Key Vault Secrets User role"

# App settings — non-secret config only; secrets load from Key Vault at runtime
az webapp config appsettings set \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    KeyVault__Uri="$KV_URI" \
    Jwt__Issuer=SchoolMS \
    Jwt__Audience=SchoolMS \
    Jwt__ExpiryMinutes=60 \
    AzureStorage__ContainerName=schoolms-files \
    "Cors__AllowedOrigins__0=https://$SWA_NAME.azurestaticapps.net" \
  --output none
ok "App settings configured"

# ── [10/11] Static Web App (frontend) ────────────────────────────────────────
step "[10/11] Static Web App (frontend)"
if ! az staticwebapp show \
     --resource-group "$RESOURCE_GROUP" \
     --name "$SWA_NAME" \
     --output none 2>/dev/null; then
  az staticwebapp create \
    --resource-group "$RESOURCE_GROUP" \
    --name "$SWA_NAME" \
    --location "$SWA_LOCATION" \
    --sku Standard \
    --output none
  ok "Static Web App created"
else
  skip "Static Web App"
fi

# ── [11/11] Monitor alert ─────────────────────────────────────────────────────
step "[11/11] Monitor alert"
APP_ID=$(az webapp show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --query id -o tsv | tr -d '\r')

if ! az monitor metrics alert show \
     --resource-group "$RESOURCE_GROUP" \
     --name "schoolms-5xx-alert" \
     --output none 2>/dev/null; then
  az monitor metrics alert create \
    --resource-group "$RESOURCE_GROUP" \
    --name "schoolms-5xx-alert" \
    --scopes "$APP_ID" \
    --condition "total 'Http5xx' > 10" \
    --window-size 5m \
    --evaluation-frequency 1m \
    --severity 1 \
    --description "SchoolMS API returning 5xx errors" \
    --output none
  ok "Alert created"
else
  skip "Alert"
fi

# ── Summary ───────────────────────────────────────────────────────────────────
SWA_URL=$(az staticwebapp show \
  --resource-group "$RESOURCE_GROUP" \
  --name "$SWA_NAME" \
  --query defaultHostname -o tsv 2>/dev/null || echo "$SWA_NAME.azurestaticapps.net")

echo ""
echo "──────────────────────────────────────────────────────"
echo "  ✅  Provisioning complete"
echo ""
echo "  API URL   : https://$APP_NAME.azurewebsites.net"
echo "  Frontend  : https://$SWA_URL"
echo "  Key Vault : $KV_URI"
echo ""
echo "  GitHub secrets needed for CI/CD:"
echo ""
echo "  AZURE_WEBAPP_PUBLISH_PROFILE:"
echo "    az webapp deployment list-publishing-profiles \\"
echo "      --resource-group $RESOURCE_GROUP --name $APP_NAME --xml"
echo ""
echo "  AZURE_STATIC_WEB_APPS_API_TOKEN:"
echo "    az staticwebapp secrets list \\"
echo "      --resource-group $RESOURCE_GROUP --name $SWA_NAME --query properties.apiKey -o tsv"
echo ""
echo "  Next: push to main to trigger the first deployment"
echo "──────────────────────────────────────────────────────"
