resource "aws_db_instance" "replica" {
  instance_class          = "db.m7g.xlarge"
  skip_final_snapshot     = true
  backup_retention_period = 7
  replicate_source_db     = aws_db_instance.main.identifier
}
