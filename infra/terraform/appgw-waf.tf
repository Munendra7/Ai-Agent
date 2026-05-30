resource "azurerm_public_ip" "appgw" {
  count               = var.enable_waf_ingress ? 1 : 0
  name                = "pip-appgw-${local.prefix}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  allocation_method   = "Static"
  sku                 = "Standard"
  tags                = var.tags
}

resource "azurerm_web_application_firewall_policy" "main" {
  count               = var.enable_waf_ingress ? 1 : 0
  name                = "waf-${local.prefix}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  policy_settings {
    enabled                     = true
    mode                        = "Prevention"
    request_body_check          = true
    file_upload_limit_in_mb     = 100
    max_request_body_size_in_kb = 128
  }

  managed_rules {
    managed_rule_set {
      type    = "OWASP"
      version = "3.2"
    }
  }

  tags = var.tags
}

resource "azurerm_application_gateway" "main" {
  count               = var.enable_waf_ingress ? 1 : 0
  name                = "agw-${local.prefix}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location

  sku {
    name = "WAF_v2"
    tier = "WAF_v2"
  }

  autoscale_configuration {
    min_capacity = 2
    max_capacity = 6
  }

  gateway_ip_configuration {
    name      = "appGatewayIpConfig"
    subnet_id = azurerm_subnet.appgw[0].id
  }

  frontend_port {
    name = "port_80"
    port = 80
  }

  frontend_ip_configuration {
    name                 = "appGatewayFrontendIP"
    public_ip_address_id = azurerm_public_ip.appgw[0].id
  }

  backend_address_pool {
    name = "defaultPool"
  }

  backend_http_settings {
    name                  = "defaultSetting"
    cookie_based_affinity = "Disabled"
    port                  = 80
    protocol              = "Http"
    request_timeout       = 30
  }

  http_listener {
    name                           = "defaultListener"
    frontend_ip_configuration_name = "appGatewayFrontendIP"
    frontend_port_name             = "port_80"
    protocol                       = "Http"
  }

  request_routing_rule {
    name                       = "defaultRule"
    rule_type                  = "Basic"
    http_listener_name         = "defaultListener"
    backend_address_pool_name  = "defaultPool"
    backend_http_settings_name = "defaultSetting"
    priority                   = 100
  }

  firewall_policy_id = azurerm_web_application_firewall_policy.main[0].id
  tags               = var.tags
}
