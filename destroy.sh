#!/bin/bash

set -e

STACK_NAME="partners-promo-stack"
REGION="us-east-1"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}=== Partners Promo - Destruir Stack ===${NC}"
echo "Stack Name: $STACK_NAME"
echo "Region: $REGION"
echo ""

# Confirm deletion
read -p "Tem certeza que deseja deletar a stack e todos os recursos? (yes/no): " CONFIRM

if [ "$CONFIRM" != "yes" ]; then
  echo -e "${RED}Operação cancelada${NC}"
  exit 0
fi

echo ""
echo -e "${YELLOW}[1/3] Obtendo informações da stack...${NC}"

# Get bucket names
FRONTEND_BUCKET=$(aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' \
  --output text \
  --region $REGION 2>/dev/null || echo "")

OUTPUT_BUCKET=$(aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query 'Stacks[0].Outputs[?OutputKey==`OutputBucketName`].OutputValue' \
  --output text \
  --region $REGION 2>/dev/null || echo "")

if [ -z "$FRONTEND_BUCKET" ]; then
  echo -e "${RED}Stack não encontrada ou já foi deletada${NC}"
  exit 1
fi

echo "Frontend Bucket: $FRONTEND_BUCKET"
echo "Output Bucket: $OUTPUT_BUCKET"
echo -e "${GREEN}✓ Informações obtidas${NC}"
echo ""

# Empty S3 buckets
echo -e "${YELLOW}[2/3] Esvaziando buckets S3...${NC}"

if [ -n "$FRONTEND_BUCKET" ]; then
  echo "Esvaziando $FRONTEND_BUCKET..."
  aws s3 rm s3://$FRONTEND_BUCKET --recursive --region $REGION 2>/dev/null || true
  echo -e "${GREEN}✓ Frontend bucket esvaziado${NC}"
fi

if [ -n "$OUTPUT_BUCKET" ]; then
  echo "Esvaziando $OUTPUT_BUCKET..."
  aws s3 rm s3://$OUTPUT_BUCKET --recursive --region $REGION 2>/dev/null || true
  echo -e "${GREEN}✓ Output bucket esvaziado${NC}"
fi

echo ""

# Delete stack
echo -e "${YELLOW}[3/3] Deletando stack CloudFormation...${NC}"
aws cloudformation delete-stack \
  --stack-name $STACK_NAME \
  --region $REGION

echo "Aguardando deleção da stack (isso pode levar alguns minutos)..."
aws cloudformation wait stack-delete-complete \
  --stack-name $STACK_NAME \
  --region $REGION

echo -e "${GREEN}✓ Stack deletada com sucesso${NC}"
echo ""
echo "=========================================="
echo -e "${GREEN}Todos os recursos foram removidos!${NC}"
echo "=========================================="
echo ""
echo "Recursos deletados:"
echo "  - Frontend Bucket: $FRONTEND_BUCKET"
echo "  - Output Bucket: $OUTPUT_BUCKET"
echo "  - Lambda Function"
echo "  - DynamoDB Table"
echo "  - IAM Role"
echo "  - CloudWatch Log Group"
echo ""
