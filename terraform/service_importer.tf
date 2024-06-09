module "ecs_importer" {
  source = "./modules/ecs-service"

  depends_on = [
    module.ecs_cluster
  ]

  name         = "rezultatevot-importer-${var.env}"
  cluster_name = module.ecs_cluster.cluster_name
  min_capacity = 0
  max_capacity = 0

  image_repo = local.images.importer.image
  image_tag  = local.images.importer.tag

  use_load_balancer = false

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
      name  = "IMPORT_ENABLED"
      value = tostring(false)
    },
    {
      name  = "IMPORT_SCHEDULE"
      value = "*/5 * * * *"
    },
    {
      name  = "DB_DATABASE"
      value = "importer"
    },
  ]

  secrets = [
    {
      name      = "APP_KEY"
      valueFrom = aws_secretsmanager_secret.app_key_importer.arn
    },
    {
      name      = "SENTRY_DSN"
      valueFrom = aws_secretsmanager_secret.sentry_dsn_importer.arn
    },
    {
      name      = "DB_CONNECTION"
      valueFrom = "${aws_secretsmanager_secret.rds_importer.arn}:engine::"
    },
    {
      name      = "DB_HOST"
      valueFrom = "${aws_secretsmanager_secret.rds_importer.arn}:host::"
    },
    {
      name      = "DB_PORT"
      valueFrom = "${aws_secretsmanager_secret.rds_importer.arn}:port::"
    },
    {
      name      = "DB_USERNAME"
      valueFrom = "${aws_secretsmanager_secret.rds_importer.arn}:username::"
    },
    {
      name      = "DB_PASSWORD"
      valueFrom = "${aws_secretsmanager_secret.rds_importer.arn}:password::"
    },
    {
      name      = "IMPORT_DB_CONNECTION"
      valueFrom = "${aws_secretsmanager_secret.rds_importer.arn}:engine::"
    },
    {
      name      = "IMPORT_DB_HOST"
      valueFrom = "${aws_secretsmanager_secret.rds_importer.arn}:host::"
    },
    {
      name      = "IMPORT_DB_PORT"
      valueFrom = "${aws_secretsmanager_secret.rds_importer.arn}:port::"
    },
    {
      name      = "IMPORT_DB_DATABASE"
      valueFrom = "${aws_secretsmanager_secret.rds_importer.arn}:database::"
    },
    {
      name      = "IMPORT_DB_USERNAME"
      valueFrom = "${aws_secretsmanager_secret.rds_importer.arn}:username::"
    },
    {
      name      = "IMPORT_DB_PASSWORD"
      valueFrom = "${aws_secretsmanager_secret.rds_importer.arn}:password::"
    },
  ]

  allowed_secrets = [
    aws_secretsmanager_secret.sentry_dsn_importer.arn,
    aws_secretsmanager_secret.app_key_importer.arn,
    aws_secretsmanager_secret.rds_importer.arn,
  ]
}

resource "aws_secretsmanager_secret" "app_key_importer" {
  name = "${local.namespace}-secret_key-${random_string.secrets_suffix.result}"
}

resource "aws_secretsmanager_secret_version" "app_key_importer" {
  secret_id     = aws_secretsmanager_secret.app_key_importer.id
  secret_string = random_password.app_key_importer.result
}

resource "random_password" "app_key_importer" {
  length  = 32
  special = true

  lifecycle {
    ignore_changes = [
      length,
      special
    ]
  }
}


resource "aws_secretsmanager_secret" "sentry_dsn_importer" {
  name = "${local.namespace}-sentry_dsn_importer-${random_string.secrets_suffix.result}"
}

resource "aws_secretsmanager_secret_version" "sentry_dsn_importer" {
  secret_id     = aws_secretsmanager_secret.sentry_dsn_importer.id
  secret_string = var.sentry_dsn_importer
}
