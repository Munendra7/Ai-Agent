resource "azurerm_container_registry" "main" {
  name                          = "acr${replace(local.prefix, "-", "")}${random_string.suffix.result}"
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  sku                           = "Premium"
  admin_enabled                 = false
  public_network_access_enabled = true
  tags                          = var.tags
}
