# CloudFormation - Deployment Rápido

Este diretório contém templates e scripts para fazer deploy automático da infraestrutura AWS usando CloudFormation.

## Arquivos

- `cloudformation-template.yaml` - Template CloudFormation completo
- `deploy.sh` - Script automatizado de deploy
- `destroy.sh` - Script para remover todos os recursos
- `CLOUDFORMATION_DEPLOYMENT.md` - Guia detalhado de deployment

## Quick Start

### 1. Deploy Completo (Recomendado)

```bash
# Tornar o script executável
chmod +x deploy.sh

# Executar deploy (substitua YOUR_SECRET_CODE por um código de 8+ caracteres)
./deploy.sh YOUR_SECRET_CODE 4
```

Este comando irá:
1. ✅ Compilar o Lambda
2. ✅ Criar a stack CloudFormation
3. ✅ Criar buckets S3 (frontend e output)
4. ✅ Criar tabela DynamoDB
5. ✅ Criar função Lambda com Function URL
6. ✅ Fazer upload do código Lambda
7. ✅ Compilar e fazer deploy do frontend

**Tempo estimado**: 5-10 minutos

### 2. Acessar a Aplicação

Após o deploy, o script mostrará a URL do frontend:

```
🌐 Acesse a aplicação em:
   http://partners-promo-stack-frontend-123456789.s3-website-us-east-1.amazonaws.com
```

### 3. Remover Todos os Recursos

```bash
# Tornar o script executável
chmod +x destroy.sh

# Executar remoção
./destroy.sh
```

## Deploy Manual via AWS CLI

Se preferir fazer o deploy manualmente:

```bash
# 1. Compilar Lambda
cd PartnersPromoLambda
dotnet lambda package -o ../deploy-package.zip
cd ..

# 2. Criar stack
aws cloudformation create-stack \
  --stack-name partners-promo-stack \
  --template-body file://cloudformation-template.yaml \
  --parameters \
    ParameterKey=VerificationCode,ParameterValue=YOUR_SECRET_CODE \
    ParameterKey=DailyExecutionLimit,ParameterValue=4 \
  --capabilities CAPABILITY_NAMED_IAM \
  --region us-east-1

# 3. Aguardar criação
aws cloudformation wait stack-create-complete \
  --stack-name partners-promo-stack \
  --region us-east-1

# 4. Upload do código Lambda
LAMBDA_FUNCTION=$(aws cloudformation describe-stacks \
  --stack-name partners-promo-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`LambdaFunctionName`].OutputValue' \
  --output text)

aws lambda update-function-code \
  --function-name $LAMBDA_FUNCTION \
  --zip-file fileb://deploy-package.zip

# 5. Deploy frontend
LAMBDA_URL=$(aws cloudformation describe-stacks \
  --stack-name partners-promo-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`LambdaFunctionUrl`].OutputValue' \
  --output text)

cd partners-promo-frontend
echo "VITE_API_ENDPOINT=$LAMBDA_URL" > .env
npm install
npm run build

FRONTEND_BUCKET=$(aws cloudformation describe-stacks \
  --stack-name partners-promo-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' \
  --output text)

aws s3 sync dist/ s3://$FRONTEND_BUCKET --delete
```

## Deploy via Console AWS

1. Acesse: https://console.aws.amazon.com/cloudformation
2. Clique em "Create stack" → "With new resources"
3. Upload `cloudformation-template.yaml`
4. Preencha os parâmetros:
   - Stack name: `partners-promo-stack`
   - VerificationCode: Seu código secreto (min 8 chars)
   - DailyExecutionLimit: `4`
5. Marque "I acknowledge that AWS CloudFormation might create IAM resources"
6. Clique em "Create stack"
7. Aguarde a criação (~5 minutos)
8. Siga os passos 4 e 5 do "Deploy Manual" acima

## Recursos Criados

A stack CloudFormation cria automaticamente:

### S3 Buckets
- **Frontend Bucket**: Hospedagem do site estático React
  - Website hosting habilitado
  - Acesso público para leitura
  - CORS configurado
  
- **Output Bucket**: Armazenamento dos CSVs gerados
  - Acesso privado (apenas via presigned URLs)
  - Lifecycle policy: deleta arquivos após 7 dias

### DynamoDB
- **Execution Counter Table**: Controle de limite diário
  - Billing mode: Pay-per-request (on-demand)
  - Partition key: Date (String)

### Lambda
- **Partners Promo Function**: Processamento de paridades
  - Runtime: .NET 8
  - Memory: 1024 MB
  - Timeout: 300 segundos (5 minutos)
  - Function URL habilitada (HTTPS público)
  - CORS configurado

### IAM
- **Lambda Execution Role**: Permissões para Lambda
  - S3: PutObject, GetObject
  - DynamoDB: GetItem, PutItem, UpdateItem
  - CloudWatch Logs: CreateLogGroup, CreateLogStream, PutLogEvents

### CloudWatch
- **Log Group**: Logs da função Lambda
  - Retenção: 7 dias

## Parâmetros do Template

| Parâmetro | Descrição | Padrão | Obrigatório |
|-----------|-----------|--------|-------------|
| VerificationCode | Código secreto para autenticação | - | Sim |
| DailyExecutionLimit | Limite de execuções por dia | 4 | Não |
| LambdaCodeBucket | Bucket S3 com código Lambda (opcional) | - | Não |
| LambdaCodeKey | Chave S3 do zip Lambda (opcional) | lambda/PartnersPromoLambda.zip | Não |

## Outputs da Stack

Após a criação, a stack fornece os seguintes outputs:

| Output | Descrição |
|--------|-----------|
| FrontendBucketName | Nome do bucket frontend |
| FrontendWebsiteURL | URL do site (use esta para acessar) |
| OutputBucketName | Nome do bucket de output |
| ExecutionCounterTableName | Nome da tabela DynamoDB |
| LambdaFunctionName | Nome da função Lambda |
| LambdaFunctionArn | ARN da função Lambda |
| LambdaFunctionUrl | URL da função Lambda (use no frontend) |
| LambdaExecutionRoleArn | ARN da role de execução |

## Obter Outputs

```bash
# Todos os outputs
aws cloudformation describe-stacks \
  --stack-name partners-promo-stack \
  --query 'Stacks[0].Outputs'

# Output específico
aws cloudformation describe-stacks \
  --stack-name partners-promo-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendWebsiteURL`].OutputValue' \
  --output text
```

## Atualizar a Stack

Para atualizar parâmetros ou template:

```bash
aws cloudformation update-stack \
  --stack-name partners-promo-stack \
  --template-body file://cloudformation-template.yaml \
  --parameters \
    ParameterKey=VerificationCode,UsePreviousValue=true \
    ParameterKey=DailyExecutionLimit,ParameterValue=10 \
  --capabilities CAPABILITY_NAMED_IAM
```

## Monitoramento

### Ver Logs do Lambda

```bash
# Tail logs em tempo real
aws logs tail /aws/lambda/partners-promo-stack-lambda --follow

# Últimas 100 linhas
aws logs tail /aws/lambda/partners-promo-stack-lambda --since 1h
```

### Ver Métricas

```bash
# Invocações nas últimas 24 horas
aws cloudwatch get-metric-statistics \
  --namespace AWS/Lambda \
  --metric-name Invocations \
  --dimensions Name=FunctionName,Value=partners-promo-stack-lambda \
  --start-time $(date -u -d '24 hours ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 3600 \
  --statistics Sum
```

### Ver Eventos da Stack

```bash
aws cloudformation describe-stack-events \
  --stack-name partners-promo-stack \
  --max-items 20
```

## Troubleshooting

### Stack creation failed

```bash
# Ver motivo da falha
aws cloudformation describe-stack-events \
  --stack-name partners-promo-stack \
  --query 'StackEvents[?ResourceStatus==`CREATE_FAILED`]'
```

### Lambda retorna erro

```bash
# Ver logs
aws logs tail /aws/lambda/partners-promo-stack-lambda --since 1h

# Testar Lambda
aws lambda invoke \
  --function-name partners-promo-stack-lambda \
  --payload '{"body":"{\"minimumScore\":1000,\"verificationCode\":\"test\"}"}' \
  response.json

cat response.json
```

### Frontend não carrega

```bash
# Verificar arquivos no bucket
BUCKET=$(aws cloudformation describe-stacks \
  --stack-name partners-promo-stack \
  --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' \
  --output text)

aws s3 ls s3://$BUCKET/

# Verificar configuração de website
aws s3api get-bucket-website --bucket $BUCKET
```

## Estimativa de Custos

### Com Free Tier (primeiros 12 meses)
- Lambda: $0.00 (1M requests + 400K GB-seconds free)
- S3: $0.00 (5GB storage + 20K GET requests free)
- DynamoDB: $0.00 (25GB storage + 25 RCU/WCU free)
- CloudWatch: $0.00 (5GB logs free)

**Total: $0.00/mês**

### Após Free Tier (120 execuções/mês)
- Lambda: ~$0.20/mês
- S3: ~$0.10/mês
- DynamoDB: ~$0.10/mês
- CloudWatch: ~$0.10/mês

**Total: ~$0.50-$1.50/mês**

## Segurança

### Boas Práticas Implementadas

✅ Buckets S3 com acesso mínimo necessário
✅ IAM Role com princípio de menor privilégio
✅ Presigned URLs com expiração (1 hora)
✅ CORS configurado adequadamente
✅ Logs habilitados para auditoria
✅ Código de verificação via parâmetro (NoEcho)
✅ Lifecycle policy para deletar arquivos antigos

### Recomendações Adicionais

- Use AWS Secrets Manager para o VerificationCode em produção
- Habilite AWS CloudTrail para auditoria completa
- Configure AWS Config para compliance
- Use AWS WAF se expor publicamente
- Habilite MFA Delete nos buckets S3

## Suporte

Para problemas ou dúvidas:

1. Verifique os logs do CloudWatch
2. Revise os eventos da stack CloudFormation
3. Consulte `CLOUDFORMATION_DEPLOYMENT.md` para guia detalhado
4. Verifique a documentação AWS CloudFormation

## Limpeza

Para remover todos os recursos e evitar custos:

```bash
./destroy.sh
```

Ou manualmente:

```bash
# Esvaziar buckets
aws s3 rm s3://$(aws cloudformation describe-stacks --stack-name partners-promo-stack --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' --output text) --recursive
aws s3 rm s3://$(aws cloudformation describe-stacks --stack-name partners-promo-stack --query 'Stacks[0].Outputs[?OutputKey==`OutputBucketName`].OutputValue' --output text) --recursive

# Deletar stack
aws cloudformation delete-stack --stack-name partners-promo-stack
```
