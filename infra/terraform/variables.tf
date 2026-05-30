variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
}

variable "tenant_id" {
  description = "Azure tenant ID"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "centralindia"
}

variable "environment" {
  description = "Environment name"
  type        = string
  default     = "prod"
}

variable "project_name" {
  description = "Project short name"
  type        = string
  default     = "aiagent"
}

variable "sql_admin_username" {
  description = "SQL admin username"
  type        = string
  default     = "sqladminuser"
}

variable "sql_admin_object_id" {
  description = "Azure AD object ID for SQL Entra admin"
  type        = string
}

variable "sql_admin_group_name" {
  description = "Display name for Entra SQL admin"
  type        = string
  default     = "sql-admins"
}

variable "k8s_namespace" {
  description = "Kubernetes namespace"
  type        = string
  default     = "ai-agent"
}

variable "enable_private_networking" {
  description = "Enable private endpoints and disable public network access for SQL/Storage/KeyVault."
  type        = bool
  default     = true
}

variable "enable_waf_ingress" {
  description = "Enable Application Gateway WAF and AKS AGIC integration."
  type        = bool
  default     = true
}

variable "jwt_secret_value" {
  description = "JWT signing secret value."
  type        = string
  sensitive   = true
}

variable "azure_openai_api_key" {
  description = "Azure OpenAI API key."
  type        = string
  sensitive   = true
  default     = ""
}

variable "google_search_api_key" {
  description = "Google Search API key."
  type        = string
  sensitive   = true
  default     = ""
}

variable "weather_api_key" {
  description = "Weather API key."
  type        = string
  sensitive   = true
  default     = ""
}

variable "speech_to_text_subscription_key" {
  description = "Azure Speech-to-Text subscription key."
  type        = string
  sensitive   = true
  default     = ""
}

variable "tags" {
  description = "Common tags"
  type        = map(string)
  default = {
    owner       = "platform-team"
    managed-by  = "terraform"
    application = "ai-agent"
  }
}
