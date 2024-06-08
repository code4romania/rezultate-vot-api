variable "env" {
  description = "Environment"
  type        = string

  validation {
    condition     = contains(["production", "staging", "development"], var.env)
    error_message = "Allowed values for env are \"production\", \"staging\" or \"development\"."
  }
}

variable "region" {
  description = "Region"
  type        = string
  default     = "eu-central-1"
}

variable "domain_name" {
  description = "Domain name used by the application. Must belong to the Route 53 zone defined in `route_53_zone_id`."
  type        = string
}

variable "hangfire_domain_name" {
  description = "Domain name used by the application. Must belong to the Route 53 zone defined in `route_53_zone_id`."
  type        = string
}

variable "route_53_zone_id" {
  type = string
}

variable "bastion_public_key" {
  description = "Public SSH key used to connect to the bastion"
  type        = string
}

variable "sentry_dsn" {
  description = "Sentry DSN"
  type        = string
  default     = null
}

variable "use_load_balancer" {
  type    = bool
  default = true
}

variable "create_iam_service_linked_role" {
  description = "Whether to create `AWSServiceRoleForECS` service-linked role. Set it to `false` if you already have an ECS cluster created in the AWS account and AWSServiceRoleForECS already exists."
  type        = bool
  default     = true
}

variable "enable_execute_command" {
  description = "Enable aws ecs execute_command"
  type        = bool
  default     = false
}

variable "uploads_bucket" {
  description = "S3 bucket used to store uploaded files"
  type        = string
}

variable "crawler_election_round_uuid" {
  description = "Election round UUID for crwal election results from MV"
  type        = string
  
}

variable "crawler_api_key" {
  description = "API key for crawler from MV"
  type        = string
}
