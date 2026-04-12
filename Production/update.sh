#!/usr/bin/env bash
set -e
# Populates the .env file with the latest environment variables from the Bootstrap Config (SSM Parameter Store).

## Fetch the latest Bootstrap Config rewriting end lines if needed
aws ssm get-parameter \
  --name "/bounan/downloader/bootstrap-config" \
  --query "Parameter.Value" \
  --output text \
  --profile downloader-updater \
  | tr -d '\r' \
  > .env
echo "Fetched .env: "
cat .env

## Inject the .env
set -a && source .env && set +a
echo "Updated .env file with the latest Bootstrap Config from SSM Parameter Store."

## Authenticate with ECR using the AWS CLI
echo "Worker image: $WORKER_IMAGE_URI"
ECR_REPO_URI=$(echo "$WORKER_IMAGE_URI" | cut -d'/' -f1-2)
echo "Logging in to ECR repository: $ECR_REPO_URI"
aws ecr get-login-password | docker login --username AWS --password-stdin "$ECR_REPO_URI"

## Pull the latest images and restart the containers
docker compose pull
docker compose up -d
