terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

provider "aws" {
  access_key                  = "test"
  secret_key                  = "test"
  region                      = "us-east-1"

  # ここが重要：すべての向き先を LocalStack に固定
  s3_use_path_style           = true
  skip_credentials_validation = true
  skip_metadata_api_check     = true
  skip_requesting_account_id  = true

  endpoints {
    s3     = "http://localhost:4566"
    lambda = "http://localhost:4566"
    sqs    = "http://localhost:4566"
  }
}

# 練習用に S3 バケットを 1 つ定義してみる
resource "aws_s3_bucket" "test_bucket" {
  bucket = "my-local-training-bucket"
}
