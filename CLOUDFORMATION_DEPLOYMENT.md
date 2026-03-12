# CloudFormation Deployment Guide

Este guia explica como fazer o deployment completo da infraestrutura usando AWS CloudFormation.

## Pré-requisitos

- AWS CLI instalado e configurado
- Permissões AWS para criar recursos (S3, Lambda, DynamoDB, IAM, CloudFormation)
- Código Lambda compilado e empacotado

## Passo 1: Compilar e Empacotar o Lambda

```bash
# Compilar o projeto Lambda
cd ParityExportLambda
dotnet lambda package -o ../deploy-package.zip

# Voltar para o diretório raiz
cd ..
```

## Passo 2: Criar a Stack CloudFormation

### Opção A: Deploy via AWS CLI

```bash
aws cloudformation create-stack \
  --stack-name parity-export-stack \
  --template-body file://cloudformation-template.yaml \
  --parameters \
    ParameterKey=VerificationCode,ParameterValue=YOUR_SECRET_CODE_HERE \
    ParameterKey=DailyExecutionLimit,ParameterValue=4 \
  --capabilities CAPABILITY_NAMED_IAM \
  --region us-east-1
```

### Opção B: Deploy via Console AWS

1. Acesse o AWS CloudFormation Console
2. Clique em "Create stack" → "With new resources"
3. Upload o arquivo `cloudformation-template.yaml`
4. Preencha os parâmetros:
   - **Stack name**: `parity-export-stack`
   - **VerificationCode**: Seu código secreto (mínimo 8 caracteres)
   - **DailyExecutionLimit**: `4` (ou outro valor)
5. Marque a opção "I acknowledge that AWS CloudFormation might create IAM resources"
6. Clique em "Create stack"

## Passo 3: Aguardar a Criação da Stack

```bash
# Monitorar o progresso
aws cloudformation wait stack-create-complete \
  --stack-name parity-export-stack \
  --region us-east-1

# Verificar status
aws cloudformation describe-stacks \
  --stack-name parity-export-stack \
  --region us-east-1
```

## Passo 4: Fazer Upload do Código Lambda

Após a stack ser criada, você precisa fazer upload do código Lambda real:

```bash
# Obter o nome da função Lambda
LAMBDA_FUNCTION=$(aws cloudformation describe-stacks \
  --stack-name parity-export-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`LambdaFunctionName`].OutputValue' \
  --output text \
  --region us-east-1)

# Fazer upload do código
aws lambda update-function-code \
  --function-name $LAMBDA_FUNCTION \
  --zip-file fileb://deploy-package.zip \
  --region us-east-1

# Aguardar a atualização
aws lambda wait function-updated \
  --function-name $LAMBDA_FUNCTION \
  --region us-east-1
```

## Passo 5: Obter as URLs e Configurações

```bash
# Obter Lambda Function URL (para usar no frontend)
aws cloudformation describe-stacks \
  --stack-name parity-export-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`LambdaFunctionUrl`].OutputValue' \
  --output text \
  --region us-east-1

# Obter Frontend Bucket Name
aws cloudformation describe-stacks \
  --stack-name parity-export-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' \
  --output text \
  --region us-east-1

# Obter Frontend Website URL
aws cloudformation describe-stacks \
  --stack-name parity-export-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendWebsiteURL`].OutputValue' \
  --output text \
  --region us-east-1
```

## Passo 6: Configurar e Fazer Deploy do Frontend

```bash
cd parity-export-frontend

# Criar arquivo .env com a Lambda Function URL
echo "VITE_API_ENDPOINT=<LAMBDA_FUNCTION_URL_AQUI>" > .env

# Instalar dependências
npm install

# Build
npm run build

# Obter o nome do bucket frontend
FRONTEND_BUCKET=$(aws cloudformation describe-stacks \
  --stack-name parity-export-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' \
  --output text \
  --region us-east-1)

# Upload para S3
aws s3 sync dist/ s3://$FRONTEND_BUCKET --delete --region us-east-1

cd ..
```

## Passo 7: Testar a Aplicação

```bash
# Obter a URL do frontend
FRONTEND_URL=$(aws cloudformation describe-stacks \
  --stack-name parity-export-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendWebsiteURL`].OutputValue' \
  --output text \
  --region us-east-1)

echo "Acesse a aplicação em: $FRONTEND_URL"
```

## Script de Deploy Completo

Crie um arquivo `deploy.sh`:

```bash
#!/bin/bash

set -e

STACK_NAME="parity-export-stack"
REGION="us-east-1"
VERIFICATION_CODE="$1"

if [ -z "$VERIFICATION_CODE" ]; then
  echo "Uso: ./deploy.sh <verification-code>"
  exit 1
fi

echo "=== Compilando Lambda ==="
cd ParityExportLambda
dotnet lambda package -o ../deploy-package.zip
cd ..

echo "=== Criando Stack CloudFormation ==="
aws cloudformation create-stack \
  --stack-name $STACK_NAME \
  --template-body file://cloudformation-template.yaml \
  --parameters \
    ParameterKey=VerificationCode,ParameterValue=$VERIFICATION_CODE \
    ParameterKey=DailyExecutionLimit,ParameterValue=4 \
  --capabilities CAPABILITY_NAMED_IAM \
  --region $REGION

echo "=== Aguardando criação da stack ==="
aws cloudformation wait stack-create-complete \
  --stack-name $STACK_NAME \
  --region $REGION

echo "=== Fazendo upload do código Lambda ==="
LAMBDA_FUNCTION=$(aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query 'Stacks[0].Outputs[?OutputKey==`LambdaFunctionName`].OutputValue' \
  --output text \
  --region $REGION)

aws lambda update-function-code \
  --function-name $LAMBDA_FUNCTION \
  --zip-file fileb://deploy-package.zip \
  --region $REGION

aws lambda wait function-updated \
  --function-name $LAMBDA_FUNCTION \
  --region $REGION

echo "=== Obtendo Lambda Function URL ==="
LAMBDA_URL=$(aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query 'Stacks[0].Outputs[?OutputKey==`LambdaFunctionUrl`].OutputValue' \
  --output text \
  --region $REGION)

echo "Lambda Function URL: $LAMBDA_URL"

echo "=== Compilando Frontend ==="
cd parity-export-frontend
echo "VITE_API_ENDPOINT=$LAMBDA_URL" > .env
npm install
npm run build

echo "=== Fazendo upload do Frontend ==="
FRONTEND_BUCKET=$(aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' \
  --output text \
  --region $REGION)

aws s3 sync dist/ s3://$FRONTEND_BUCKET --delete --region $REGION
cd ..

echo "=== Deploy Completo! ==="
FRONTEND_URL=$(aws cloudformation describe-stacks \
  --stack-name $STACK_NAME \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendWebsiteURL`].OutputValue' \
  --output text \
  --region $REGION)

echo ""
echo "Frontend URL: $FRONTEND_URL"
echo "Lambda Function URL: $LAMBDA_URL"
echo ""
echo "Acesse a aplicação em: $FRONTEND_URL"
```

Tornar executável e rodar:

```bash
chmod +x deploy.sh
./deploy.sh YOUR_SECRET_CODE
```

## Atualizar a Stack

Para atualizar a stack existente:

```bash
aws cloudformation update-stack \
  --stack-name parity-export-stack \
  --template-body file://cloudformation-template.yaml \
  --parameters \
    ParameterKey=VerificationCode,UsePreviousValue=true \
    ParameterKey=DailyExecutionLimit,ParameterValue=4 \
  --capabilities CAPABILITY_NAMED_IAM \
  --region us-east-1
```

## Deletar a Stack

Para remover todos os recursos:

```bash
# Esvaziar os buckets S3 primeiro
FRONTEND_BUCKET=$(aws cloudformation describe-stacks \
  --stack-name parity-export-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' \
  --output text \
  --region us-east-1)

OUTPUT_BUCKET=$(aws cloudformation describe-stacks \
  --stack-name parity-export-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`OutputBucketName`].OutputValue' \
  --output text \
  --region us-east-1)

aws s3 rm s3://$FRONTEND_BUCKET --recursive --region us-east-1
aws s3 rm s3://$OUTPUT_BUCKET --recursive --region us-east-1

# Deletar a stack
aws cloudformation delete-stack \
  --stack-name parity-export-stack \
  --region us-east-1

# Aguardar a deleção
aws cloudformation wait stack-delete-complete \
  --stack-name parity-export-stack \
  --region us-east-1
```

## Recursos Criados

A stack CloudFormation cria os seguintes recursos:

1. **S3 Buckets**:
   - Frontend bucket (static website hosting)
   - Output bucket (CSV files, lifecycle 7 dias)

2. **DynamoDB Table**:
   - Execution counter table (on-demand billing)

3. **Lambda Function**:
   - Runtime: .NET 8
   - Memory: 1024 MB
   - Timeout: 300 segundos
   - Function URL habilitada

4. **IAM Role**:
   - Lambda execution role com permissões S3, DynamoDB e CloudWatch

5. **CloudWatch Log Group**:
   - Retenção de 7 dias

## Monitoramento

```bash
# Ver logs do Lambda
aws logs tail /aws/lambda/<function-name> --follow --region us-east-1

# Ver métricas do Lambda
aws cloudwatch get-metric-statistics \
  --namespace AWS/Lambda \
  --metric-name Invocations \
  --dimensions Name=FunctionName,Value=<function-name> \
  --start-time $(date -u -d '1 hour ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Sum \
  --region us-east-1
```

## Troubleshooting

### Stack creation failed

```bash
# Ver eventos da stack
aws cloudformation describe-stack-events \
  --stack-name parity-export-stack \
  --region us-east-1
```

### Lambda não funciona

```bash
# Ver logs
aws logs tail /aws/lambda/<function-name> --follow --region us-east-1

# Testar Lambda diretamente
aws lambda invoke \
  --function-name <function-name> \
  --payload '{"body":"{\"minimumScore\":1000,\"verificationCode\":\"test\"}"}' \
  response.json \
  --region us-east-1

cat response.json
```

### Frontend não carrega

```bash
# Verificar se os arquivos foram uploadados
aws s3 ls s3://<frontend-bucket>/ --region us-east-1

# Verificar política do bucket
aws s3api get-bucket-policy --bucket <frontend-bucket> --region us-east-1
```

## Estimativa de Custos

Com 120 execuções/mês (4/dia):

- Lambda: $0.00 (free tier)
- S3: $0.00 (free tier)
- DynamoDB: $0.00 (free tier)
- CloudWatch: $0.00 (free tier)

**Total**: $0.00/mês (dentro do free tier)

Após free tier: ~$0.50-$1.50/mês
