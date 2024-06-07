module "ecs_api" {
  source = "./modules/ecs-service"

  depends_on = [
    module.ecs_cluster
  ]

  name         = "rezultatevot-api-${var.env}"
  cluster_name = module.ecs_cluster.cluster_name
  min_capacity = 1
  max_capacity = 1

  image_repo = local.images.api.image
  image_tag  = local.images.api.tag

  use_load_balancer       = var.use_load_balancer
  lb_dns_name             = aws_lb.main.dns_name
  lb_zone_id              = aws_lb.main.zone_id
  lb_vpc_id               = aws_vpc.main.id
  lb_listener_arn         = aws_lb_listener.http.arn
  lb_hosts                = [var.domain_name]
  lb_health_check_enabled = true
  lb_path                 = "/health"

  container_memory_soft_limit = 1024
  container_memory_hard_limit = 2048

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
      name  = "ASPNETCORE_URLS",
      value = "http://+:80"
    },
    {
      name  = "S3Bucket__BucketName"
      value = data.aws_s3_bucket.uploads.bucket
    },
  ]

  secrets = [
    {
      name      = "ConnectionStrings__DefaultConnection"
      valueFrom = aws_secretsmanager_secret.rds.arn
    },
    {
      name      = "S3Bucket__AccessKeyId"
      valueFrom = "${aws_secretsmanager_secret.iam_hotfix.arn}:access_key::"
    },
    {
      name      = "S3Bucket__AccessKeySecret"
      valueFrom = "${aws_secretsmanager_secret.iam_hotfix.arn}:secret_key::"
    },
  ]

  allowed_secrets = [
    aws_secretsmanager_secret.rds.arn,
    aws_secretsmanager_secret.iam_hotfix.arn,
  ]
}
