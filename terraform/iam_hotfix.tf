resource "aws_iam_user" "iam_hotfix" {
  name = "${local.namespace}-user"
}

resource "aws_iam_access_key" "iam_hotfix" {
  user = aws_iam_user.iam_hotfix.name
}

resource "aws_secretsmanager_secret" "iam_hotfix" {
  name = "${local.namespace}-iam_hotfix-${random_string.secrets_suffix.result}"
}

resource "aws_secretsmanager_secret_version" "iam_hotfix" {
  secret_id = aws_secretsmanager_secret.iam_hotfix.id
  secret_string = jsonencode({
    access_key = aws_iam_access_key.iam_hotfix.id,
    secret_key = aws_iam_access_key.iam_hotfix.secret
  })
}

resource "aws_iam_user_policy" "iam_hotfix" {
  name   = "iam_hotfix"
  user   = aws_iam_user.iam_hotfix.name
  policy = data.aws_iam_policy_document.ecs_task.json
}
