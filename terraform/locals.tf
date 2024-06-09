locals {
  namespace         = "rezultatevot-${var.env}"
  availability_zone = data.aws_availability_zones.current.names[0]

  images = {
    api = {
      image = "code4romania/rezultate-vot-api"
      tag   = "1.1.0-rc.5"
    }

    hangfire = {
      image = "code4romania/rezultate-vot-hangfire"
      tag   = "1.1.0-rc.5"
    }
  }

  ecs = {
    instance_types = {
      "t3a.medium" = ""
    }
  }

  db = {
    name           = "rezultatevot"
    instance_class = var.env == "production" ? "db.t4g.large" : "db.t4g.micro"
  }

  networking = {
    cidr_block = "10.0.0.0/16"

    public_subnets = [
      "10.0.1.0/24",
      "10.0.2.0/24",
      "10.0.3.0/24"
    ]

    private_subnets = [
      "10.0.4.0/24",
      "10.0.5.0/24",
      "10.0.6.0/24"
    ]
  }
}
