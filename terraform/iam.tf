data "aws_iam_policy_document" "ecs_task" {
  statement {
    actions = [
      "s3:ListBucket",
      "s3:HeadBucket"
    ]

    resources = ["*"]
  }

  statement {
    actions = [
      "s3:ListBucket",
      "s3:GetObject",
      "s3:DeleteObject",
      "s3:GetObjectAcl",
      "s3:PutObjectAcl",
      "s3:PutObject"
    ]

    resources = [
      data.aws_s3_bucket.uploads.arn,
      "${data.aws_s3_bucket.uploads.arn}/*"
    ]
  }
}

data "aws_iam_policy_document" "ecs_task_assume" {
  statement {
    actions = ["sts:AssumeRole"]

    principals {
      type        = "Service"
      identifiers = ["ecs-tasks.amazonaws.com"]
    }
  }
}

resource "aws_iam_role" "ecs_task_role" {
  name               = "${local.namespace}-ecs-task-role"
  assume_role_policy = data.aws_iam_policy_document.ecs_task_assume.json


  inline_policy {
    name   = "EcsTaskPolicy"
    policy = data.aws_iam_policy_document.ecs_task.json
  }
}
