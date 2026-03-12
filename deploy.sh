#!/bin/bash

set -e

STACK_NAME="partners-promo-stack"
REGION="us-east-1"
VERIFICATION_CODE="$1"
DAILY_LIMIT="${2:-4}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

if [ -z "$VERIFICATION_CODE" ]; then
  echo -e "${RED}Erro: Código de verificação não fornecido${NC}"
  echo "Uso: ./deploy.sh <verification-code> [daily-limit]"
  echo "Exemplo: ./deploy.sh MySecretCode123 4"
  exit 1
fi

if [ ${#VERIFICATION_CODE} -lt 8 ]; then
  echo -e "${RED}Erro: Código de verificação deve ter pelo menos 8 caracteres${NC}"
  exit 1
fi

echo -e "${GREEN}=== Partners Promo - Deploy Automatizado ===${NC}"
echo "Stack Name: $STACK_NAME"
echo "Region: $REGION"
echo "Daily Limit: $DAILY_LIMIT"
echo ""

# Step 1: Build Lambda
echo -e "${YELLOW}[1/7] Compilando Lambda...${NC}"
cd PartnersPromoLambda
dotnet lambda package -o ../deploy-package.zip
cd ..
echo -e "${GREEN}✓ Lambda compilado${NC}"
echo ""

# Step 2: Create CloudFormation Stack
echo -e "${YELLOW}[2/7] Criando Stack CloudFormation...${NC}"
aws cloudformation create-stack \
  --stack-name $STACK_NAME \
  --template-body file://cloudformation-template.yaml \
  --parameters \
    ParameterKey=VerificationCode,ParameterValue=$VERIFICATION_CODE \
    ParameterKey=DailyExecutionLimit,ParameterValue=$DAILY_LIMIT \
  --capabilities CAPABILITY_NAMED_IAM \
  --region $REGION

echo -e "${GREEN}✓ Stack criada, aguardando conclusão...${NC}"
echo ""

# Step 3: Wait for stack creation
echo -e "${YELLOW}[3/7] Aguardando criação da stack (isso pode levar alguns minutos)...${NC}"
aws cloudformation wait stack-create-complete \
  --stack-name $STACK_NAME \
  --region $REGION

echo -e "${GREEN}✓ Stack criada com sucesso${NC}"
echo ""

# Step 4: Upload Lambda code
echo -e "${YELLOW}[4/7] Fazendo upload do código Lambda...${NC}"
LAMBDA_FUNCTION=$(aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query 'Stacks[0].Outputs[?OutputKey==`LambdaFunctionName`].OutputValue' \
  --output text \
  --region $REGION)

echo "Lambda Function: $LAMBDA_FUNCTION"

aws lambda update-function-code \
  --function-name $LAMBDA_FUNCTION \
  --zip-file fileb://deploy-package.zip \
  --region $REGION > /dev/null

echo "Aguardando atualização da função..."
aws lambda wait function-updated \
  --function-name $LAMBDA_FUNCTION \
  --region $REGION

echo -e "${GREEN}✓ Código Lambda atualizado${NC}"
echo ""

# Step 5: Get Lambda Function URL
echo -e "${YELLOW}[5/7] Obtendo configurações...${NC}"
LAMBDA_URL=$(aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query 'Stacks[0].Outputs[?OutputKey==`LambdaFunctionUrl`].OutputValue' \
  --output text \
  --region $REGION)

FRONTEND_BUCKET=$(aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' \
  --output text \
  --region $REGION)

OUTPUT_BUCKET=$(aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query 'Stacks[0].Outputs[?OutputKey==`OutputBucketName`].OutputValue' \
  --output text \
  --region $REGION)

DYNAMODB_TABLE=$(aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query 'Stacks[0].Outputs[?OutputKey==`ExecutionCounterTableName`].OutputValue' \
  --output text \
  --region $REGION)

echo "Lambda URL: $LAMBDA_URL"
echo "Frontend Bucket: $FRONTEND_BUCKET"
echo "Output Bucket: $OUTPUT_BUCKET"
echo "DynamoDB Table: $DYNAMODB_TABLE"
echo -e "${GREEN}✓ Configurações obtidas${NC}"
echo ""

# Step 6: Build and deploy frontend
echo -e "${YELLOW}[6/7] Compilando e fazendo deploy do Frontend...${NC}"
cd partners-promo-frontend

# Create .env file
echo "VITE_API_ENDPOINT=$LAMBDA_URL" > .env
echo "Arquivo .env criado com Lambda URL"

# Install dependencies if needed
if [ ! -d "node_modules" ]; then
  echo "Instalando dependências do npm..."
  npm install
fi

# Build
echo "Compilando frontend..."
npm run build

# Upload to S3
echo "Fazendo upload para S3..."
aws s3 sync dist/ s3://$FRONTEND_BUCKET --delete --region $REGION

cd ..
echo -e "${GREEN}✓ Frontend deployado${NC}"
echo ""

# Step 7: Get final URLs
echo -e "${YELLOW}[7/7] Obtendo URLs finais...${NC}"
FRONTEND_URL=$(aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendWebsiteURL`].OutputValue' \
  --output text \
  --region $REGION)

echo -e "${GREEN}✓ Deploy completo!${NC}"
echo ""
echo "=========================================="
echo -e "${GREEN}Deploy Concluído com Sucesso!${NC}"
echo "=========================================="
echo ""
echo "📱 Frontend URL:"
echo "   $FRONTEND_URL"
echo ""
echo "🔗 Lambda Function URL:"
echo "   $LAMBDA_URL"
echo ""
echo "📦 Recursos Criados:"
echo "   - Frontend Bucket: $FRONTEND_BUCKET"
echo "   - Output Bucket: $OUTPUT_BUCKET"
echo "   - DynamoDB Table: $DYNAMODB_TABLE"
echo "   - Lambda Function: $LAMBDA_FUNCTION"
echo ""
echo "🔐 Configurações:"
echo "   - Verification Code: ********"
echo "   - Daily Limit: $DAILY_LIMIT execuções/dia"
echo ""
echo "📊 Monitoramento:"
echo "   CloudWatch Logs: /aws/lambda/$LAMBDA_FUNCTION"
echo ""
echo "🌐 Acesse a aplicação em:"
echo "   $FRONTEND_URL"
echo ""
echo "=========================================="
