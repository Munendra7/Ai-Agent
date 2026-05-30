resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-${local.prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = var.tags
}

resource "azurerm_monitor_workspace" "main" {
  name                = "amw-${local.prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = var.tags
}

resource "azurerm_dashboard_grafana" "main" {
  name                              = "grafana-${local.prefix}"
  resource_group_name               = azurerm_resource_group.main.name
  location                          = azurerm_resource_group.main.location
  grafana_major_version             = 9
  api_key_enabled                   = false
  deterministic_outbound_ip_enabled = true
  public_network_access_enabled     = true
  azure_monitor_workspace_integrations {
    resource_id = azurerm_monitor_workspace.main.id
  }
  tags = var.tags
}

resource "azurerm_application_insights" "main" {
  name                = "appi-${local.prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.main.id
  application_type    = "web"
  tags                = var.tags
}

resource "azurerm_kubernetes_cluster" "main" {
  name                = "aks-${local.prefix}"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name
  dns_prefix          = "aks-${local.prefix}"
  kubernetes_version  = "1.30"

  sku_tier = "Standard"

  oidc_issuer_enabled       = true
  workload_identity_enabled = true
  azure_policy_enabled      = true

  default_node_pool {
    name                 = "system"
    vm_size              = "Standard_D4s_v5"
    auto_scaling_enabled = true
    min_count            = 2
    max_count            = 6
    max_pods             = 50
    os_disk_type         = "Managed"
    os_disk_size_gb      = 128
    vnet_subnet_id       = azurerm_subnet.aks.id
    node_labels = {
      workload = "system"
    }
  }

  identity {
    type = "SystemAssigned"
  }

  oms_agent {
    log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
  }

  monitor_metrics {
    annotations_allowed = null
    labels_allowed      = null
  }

  key_vault_secrets_provider {
    secret_rotation_enabled  = true
    secret_rotation_interval = "2m"
  }

  dynamic "ingress_application_gateway" {
    for_each = var.enable_waf_ingress ? [1] : []
    content {
      gateway_id = azurerm_application_gateway.main[0].id
    }
  }

  network_profile {
    network_plugin    = "azure"
    network_policy    = "azure"
    load_balancer_sku = "standard"
    outbound_type     = "loadBalancer"
  }

  tags = var.tags
}

resource "azurerm_kubernetes_cluster_node_pool" "user" {
  name                  = "user"
  kubernetes_cluster_id = azurerm_kubernetes_cluster.main.id
  vm_size               = "Standard_D4s_v5"
  auto_scaling_enabled  = true
  min_count             = 2
  max_count             = 10
  mode                  = "User"
  max_pods              = 50
  vnet_subnet_id        = azurerm_subnet.aks.id
  orchestrator_version  = azurerm_kubernetes_cluster.main.kubernetes_version
  node_labels = {
    workload = "app"
  }
  tags = var.tags
}

resource "azurerm_role_assignment" "aks_to_acr" {
  scope                = azurerm_container_registry.main.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_kubernetes_cluster.main.kubelet_identity[0].object_id
}

resource "azurerm_monitor_data_collection_rule" "prometheus" {
  name                = "dcr-${local.prefix}-prom"
  location            = azurerm_resource_group.main.location
  resource_group_name = azurerm_resource_group.main.name

  destinations {
    monitor_account {
      monitor_account_id = azurerm_monitor_workspace.main.id
      name               = "azureMonitorAccount"
    }
  }

  data_flow {
    streams      = ["Microsoft-PrometheusMetrics"]
    destinations = ["azureMonitorAccount"]
  }

  data_sources {
    prometheus_forwarder {
      name    = "prometheusDataSource"
      streams = ["Microsoft-PrometheusMetrics"]
    }
  }

  kind = "Linux"
}

resource "azurerm_monitor_data_collection_rule_association" "prometheus" {
  name                    = "dcra-${local.prefix}-prom"
  target_resource_id      = azurerm_kubernetes_cluster.main.id
  data_collection_rule_id = azurerm_monitor_data_collection_rule.prometheus.id
}
