resource "aws_cloudfront_distribution" "main" {
  price_class     = "PriceClass_100"
  enabled         = true
  is_ipv6_enabled = true
  http_version    = "http2and3"
  aliases = [
    var.domain_name
  ]

  origin {
    domain_name = aws_lb.main.dns_name
    origin_id   = aws_lb.main.dns_name

    custom_origin_config {
      http_port              = 80
      https_port             = 443
      origin_protocol_policy = "http-only"
      origin_ssl_protocols   = ["TLSv1.2"]
    }
  }

  default_cache_behavior {
    allowed_methods          = ["GET", "HEAD", "OPTIONS"]
    cached_methods           = ["GET", "HEAD", "OPTIONS"]
    target_origin_id         = aws_lb.main.dns_name
    cache_policy_id          = aws_cloudfront_cache_policy.default.id
    origin_request_policy_id = aws_cloudfront_origin_request_policy.default.id
    viewer_protocol_policy   = "redirect-to-https"
    compress                 = true
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  viewer_certificate {
    acm_certificate_arn      = aws_acm_certificate.main.arn
    ssl_support_method       = "sni-only"
    minimum_protocol_version = "TLSv1.2_2021"
  }
}

resource "aws_cloudfront_cache_policy" "default" {
  name        = "${local.namespace}-cache-policy"
  min_ttl     = 60  # 1 minute
  default_ttl = 300 # 5 minutes
  max_ttl     = 600 # 10 minutes

  parameters_in_cache_key_and_forwarded_to_origin {
    enable_accept_encoding_brotli = true
    enable_accept_encoding_gzip   = true

    cookies_config {
      cookie_behavior = "none"
    }

    headers_config {
      header_behavior = "none"
    }

    query_strings_config {
      query_string_behavior = "all"
    }
  }
}

resource "aws_cloudfront_origin_request_policy" "default" {
  name = "${local.namespace}-origin-request-policy"

  cookies_config {
    cookie_behavior = "none"
  }

  headers_config {
    header_behavior = "allViewerAndWhitelistCloudFront"

    headers {
      items = [
        "CloudFront-Forwarded-Proto",
      ]
    }
  }

  query_strings_config {
    query_string_behavior = "all"
  }
}