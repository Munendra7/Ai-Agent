locals {
  prefix = "${var.project_name}-${var.environment}"
}

resource "random_string" "suffix" {
  length  = 5
  special = false
  upper   = false
}

resource "random_password" "sql_admin_password" {
  length           = 24
  special          = true
  override_special = "_%@"
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${local.prefix}"
  location = var.location
  tags     = var.tags
}
