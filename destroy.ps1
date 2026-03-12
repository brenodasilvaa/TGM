$ErrorActionPreference = "Stop"

$STACK_NAME = "partners-promo-stack"
$REGION = "us-east-1"

Write-Host "=== Partners Promo - Destruir Stack ===" -ForegroundColor Yellow
Write-Host "Stack Name: $STACK_NAME"
Write-Host "Region: $REGION"
Write-Host ""

# Confirm deletion
$CONFIRM = Read-Host "Tem certeza que deseja deletar a stack e todos os recursos? (yes/no)"

if ($CONFIRM -ne "yes") {
    Write-Host "Operacao cancelada" -ForegroundColor Red
    exit 0
}

Write-Host ""
Write-Host "[1/3] Obtendo informacoes da stack..." -ForegroundColor Yellow

# Get bucket names
$FRONTEND_BUCKET = aws cloudformation describe-stacks `
    --stack-name $STACK_NAME `
    --query 'Stacks[0].Outputs[?OutputKey==`FrontendBucketName`].OutputValue' `
    --output text `
    --region $REGION 2>$null

$OUTPUT_BUCKET = aws cloudformation describe-stacks `
    --stack-name $STACK_NAME `
    --query 'Stacks[0].Outputs[?OutputKey==`OutputBucketName`].OutputValue' `
    --output text `
    --region $REGION 2>$null

if ([string]::IsNullOrEmpty($FRONTEND_BUCKET)) {
    Write-Host "Stack nao encontrada ou ja foi deletada" -ForegroundColor Red
    exit 1
}

Write-Host "Frontend Bucket: $FRONTEND_BUCKET"
Write-Host "Output Bucket: $OUTPUT_BUCKET"
Write-Host "Informacoes obtidas" -ForegroundColor Green
Write-Host ""

# Empty S3 buckets
Write-Host "[2/3] Esvaziando buckets S3..." -ForegroundColor Yellow

if (-not [string]::IsNullOrEmpty($FRONTEND_BUCKET)) {
    Write-Host "Esvaziando $FRONTEND_BUCKET..."
    aws s3 rm s3://$FRONTEND_BUCKET --recursive --region $REGION 2>$null
    Write-Host "Frontend bucket esvaziado" -ForegroundColor Green
}

if (-not [string]::IsNullOrEmpty($OUTPUT_BUCKET)) {
    Write-Host "Esvaziando $OUTPUT_BUCKET..."
    aws s3 rm s3://$OUTPUT_BUCKET --recursive --region $REGION 2>$null
    Write-Host "Output bucket esvaziado" -ForegroundColor Green
}

Write-Host ""

# Delete stack
Write-Host "[3/3] Deletando stack CloudFormation..." -ForegroundColor Yellow
aws cloudformation delete-stack `
    --stack-name $STACK_NAME `
    --region $REGION

Write-Host "Aguardando delecao da stack (isso pode levar alguns minutos)..."
aws cloudformation wait stack-delete-complete `
    --stack-name $STACK_NAME `
    --region $REGION

Write-Host "Stack deletada com sucesso" -ForegroundColor Green
Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Todos os recursos foram removidos!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Recursos deletados:"
Write-Host "  - Frontend Bucket: $FRONTEND_BUCKET"
Write-Host "  - Output Bucket: $OUTPUT_BUCKET"
Write-Host "  - Lambda Function"
Write-Host "  - DynamoDB Table"
Write-Host "  - IAM Role"
Write-Host "  - CloudWatch Log Group"
Write-Host ""
