module "ecs_hangfire" {
  source = "./modules/ecs-service"

  depends_on = [
    module.ecs_cluster
  ]

  name         = "rezultatevot-hangfire-${var.env}"
  cluster_name = module.ecs_cluster.cluster_name
  min_capacity = 1
  max_capacity = 1

  image_repo = local.images.hangfire.image
  image_tag  = local.images.hangfire.tag

  use_load_balancer       = var.use_load_balancer
  lb_dns_name             = aws_lb.main.dns_name
  lb_zone_id              = aws_lb.main.zone_id
  lb_vpc_id               = aws_vpc.main.id
  lb_listener_arn         = aws_lb_listener.https.arn
  lb_hosts                = [var.hangfire_domain_name]
  lb_domain_zone_id       = data.aws_route53_zone.main.zone_id
  lb_health_check_enabled = true
  lb_path                 = "/health"

  container_memory_soft_limit = 256
  container_memory_hard_limit = 768

  log_group_name                 = module.ecs_cluster.log_group_name
  service_discovery_namespace_id = module.ecs_cluster.service_discovery_namespace_id

  container_port          = 80
  network_mode            = "awsvpc"
  network_security_groups = [aws_security_group.ecs.id]
  network_subnets         = [aws_subnet.private.0.id]

  task_role_arn          = aws_iam_role.ecs_task_role.arn
  enable_execute_command = var.enable_execute_command

  # predefined_metric_type = "ECSServiceAverageCPUUtilization"
  # target_value           = 65

  ordered_placement_strategy = [
    {
      type  = "spread"
      field = "instanceId"
    },
    {
      type  = "binpack"
      field = "memory"
    }
  ]

  environment = [
    {
      name  = "ASPNETCORE__ENVIRONMENT"
      value = var.env
    },
    {
      name  = "HangfireDashboard__IsSecured"
      value = tostring(true)
    },
    {
      name  = "Crawler__ApiUrl"
      value = "https://prezenta.roaep.ro"
    },
    {
      name  = "Crawler__ElectionRounds__0__Category"
      value = "Local"
    },
    {
      name  = "Crawler__ElectionRounds__0__CronExpression"
      value = "*/10 * * * *"
    },
    {
      name  = "Crawler__ElectionRounds__0__ElectionRoundId"
      value = "50"
    },
    {
      name  = "Crawler__ElectionRounds__0__HasDiaspora"
      value = tostring(false)
    },
    {
      name  = "Crawler__ElectionRounds__0__Key"
      value = "locale27092020"
    },
  ]

  secrets = [
    {
      name      = "ConnectionStrings__DefaultConnection"
      valueFrom = aws_secretsmanager_secret.rds.arn
    },
    {
      name      = "HangfireDashboard__Username"
      valueFrom = "${aws_secretsmanager_secret.hangfire_credentials.arn}:username::"
    },
    {
      name      = "HangfireDashboard__Password"
      valueFrom = "${aws_secretsmanager_secret.hangfire_credentials.arn}:password::"
    }
  ]

  allowed_secrets = [
    aws_secretsmanager_secret.rds.arn,
    aws_secretsmanager_secret.hangfire_credentials.arn,
  ]
}


# Hangfire credentials
resource "random_password" "hangfire_password" {
  length  = 20
  special = false

  lifecycle {
    ignore_changes = [
      length,
      special
    ]
  }
}

resource "aws_secretsmanager_secret" "hangfire_credentials" {
  name = "${local.namespace}-hangfire_credentials-${random_string.secrets_suffix.result}"
}

resource "aws_secretsmanager_secret_version" "hangfire_credentials" {
  secret_id = aws_secretsmanager_secret.hangfire_credentials.id
  secret_string = jsonencode({
    username = "admin"
    password = random_password.hangfire_password.result
  })
}