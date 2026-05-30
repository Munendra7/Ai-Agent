resource "azurerm_mssql_server" "main" {
  name                          = "sql-${replace(local.prefix, "-", "")}-${random_string.suffix.result}"
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  version                       = "12.0"
  administrator_login           = var.sql_admin_username
  administrator_login_password  = random_password.sql_admin_password.result
  minimum_tls_version           = "1.2"
  public_network_access_enabled = var.enable_private_networking ? false : true
  tags                          = var.tags
}

resource "azurerm_mssql_database" "main" {
  name         = "sqldb-${local.prefix}"
  server_id    = azurerm_mssql_server.main.id
  sku_name     = "GP_S_Gen5_2"
  max_size_gb  = 32
  zone_redundant = false
  tags = var.tags
}

resource "null_resource" "sql_ad_admin" {
  provisioner "local-exec" {
    command = <<EOT
az sql server ad-admin create \
  --resource-group ${azurerm_resource_group.main.name} \
  --server ${azurerm_mssql_server.main.name} \
  --display-name "${var.sql_admin_group_name}" \
  --object-id ${var.sql_admin_object_id}
EOT
  }

  depends_on = [azurerm_mssql_server.main]
}

resource "azurerm_mssql_firewall_rule" "azure_services" {
  count            = var.enable_private_networking ? 0 : 1
  name             = "AllowAzureServices"
  server_id        = azurerm_mssql_server.main.id
  start_ip_address = "0.0.0.0"
  end_ip_address   = "0.0.0.0"
}
