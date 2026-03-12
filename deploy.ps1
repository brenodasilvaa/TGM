param(
    [Parameter(Mandatory=$true)]
    [string]$VerificationCode,
    
    [Parameter(Mandatory=$false)]
    [int]$DailyLimit = 4
)

$ErrorActionPreference = "Stop"

$STACK_NAME = "partners-promo-stack"
$REGION = "us-east-1"

# Validate verification code
if ($VerificationCode.Length -lt 8) {
    Write-Host "Erro: Codigo de verificacao deve ter pelo menos 8 caracteres" -ForegroundColor Red
    exit 1
}

Write-Host "=== Partners Promo - Deploy Automatizado ===" -ForegroundColor Green
Write-Host "Stack Name: $STACK_NAME"
Write-Host "Region: $REGION"
Write-Host "Daily Limit: $DailyLimit"
Write-Host ""

# Step 1: Build Lambda
Write-Host "[1/7] Compilando Lambda..." -ForegroundColor Yellow
$ROOT_DIR = Get-Location
Push-Location PartnersPromoLambda

# Publish Lambda with linux-x64 runtime
Write-Host "Publicando Lambda para linux-x64..."
dotnet publish -c Release -r linux-x64 --self-contained false -o bin/Release/net8.0/publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao publicar Lambda" -ForegroundColor Red
    Pop-Location
    exit 1
}

# Create zip package
Write-Host "Criando pacote zip..."
$ZIP_PATH = Join-Path $ROOT_DIR "deploy-package.zip"
if (Test-Path $ZIP_PATH) {
    Remove-Item $ZIP_PATH -Force
}

$PUBLISH_DIR = Join-Path (Get-Location) "bin/Release/net8.0/publish"
if (-not (Test-Path $PUBLISH_DIR)) {
    Write-Host "Erro: Diretorio de publicacao nao encontrado: $PUBLISH_DIR" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host "Compactando arquivos de: $PUBLISH_DIR"
Write-Host "Para: $ZIP_PATH"
Push-Location $PUBLISH_DIR
Compress-Archive -Path * -DestinationPath $ZIP_PATH -Force
Pop-Location

if (-not (Test-Path $ZIP_PATH)) {
    Write-Host "Erro ao criar pacote zip em: $ZIP_PATH" -ForegroundColor Red
    Pop-Location
    exit 1
}

$ZIP_SIZE = (Get-Item $ZIP_PATH).Length / 1MB
Write-Host "Pacote criado: $([math]::Round($ZIP_SIZE, 2)) MB"

Pop-Location
Write-Host "Lambda compilado e empacotado" -ForegroundColor Green
Write-Host ""

# Step 2: Create temporary S3 bucket for Lambda code
Write-Host "[2/8] Criando bucket temporario para codigo Lambda..." -ForegroundColor Yellow
$TEMP_BUCKET = "partners-promo-lambda-code-$((Get-Random -Maximum 99999))"
aws s3 mb s3://$TEMP_BUCKET --region $REGION

if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao criar bucket temporario" -ForegroundColor Red
    exit 1
}

Write-Host "Fazendo upload do codigo Lambda para S3..."
aws s3 cp deploy-package.zip s3://$TEMP_BUCKET/lambda-code.zip --region $REGION

if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao fazer upload do codigo" -ForegroundColor Red
    aws s3 rb s3://$TEMP_BUCKET --force --region $REGION
    exit 1
}

Write-Host "Bucket temporario criado: $TEMP_BUCKET" -ForegroundColor Green
Write-Host ""

# Step 3: Create CloudFormation Stack
Write-Host "[3/8] Criando Stack CloudFormation..." -ForegroundColor Yellow
aws cloudformation create-stack --stack-name $STACK_NAME --template-body file://cloudformation-template.yaml --parameters ParameterKey=VerificationCode,ParameterValue=$VerificationCode ParameterKey=DailyExecutionLimit,ParameterValue=$DailyLimit ParameterKey=LambdaCodeBucket,ParameterValue=$TEMP_BUCKET ParameterKey=LambdaCodeKey,ParameterValue=lambda-code.zip --capabilities CAPABILITY_NAMED_IAM --region $REGION

if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao criar stack" -ForegroundColor Red
    exit 1
}

Write-Host "Stack criada, aguardando conclusao..." -ForegroundColor Green
Write-Host ""

# Step 4: Wait for stack creation
Write-Host "[4/8] Aguardando criacao da stack (isso pode levar alguns minutos)..." -ForegroundColor Yellow

$MAX_ATTEMPTS = 60
$ATTEMPT = 0
$STACK_STATUS = ""

while ($ATTEMPT -lt $MAX_ATTEMPTS) {
    Start-Sleep -Seconds 5
    $ATTEMPT++
    
    $STACK_INFO = aws cloudformation describe-stacks --stack-name $STACK_NAME --region $REGION 2>$null | ConvertFrom-Json
    
    if ($null -eq $STACK_INFO) {
        Write-Host "Stack foi deletada automaticamente apos falha" -ForegroundColor Red
        break
    }
    
    $STACK_STATUS = $STACK_INFO.Stacks[0].StackStatus
    Write-Host "Status: $STACK_STATUS" -ForegroundColor Yellow
    
    if ($STACK_STATUS -eq "CREATE_COMPLETE") {
        Write-Host "Stack criada com sucesso" -ForegroundColor Green
        break
    }
    
    if ($STACK_STATUS -match "ROLLBACK|FAILED") {
        Write-Host "Erro na criacao da stack!" -ForegroundColor Red
        Write-Host "Capturando eventos de erro..." -ForegroundColor Yellow
        
        $EVENTS = aws cloudformation describe-stack-events --stack-name $STACK_NAME --region $REGION --max-items 20 2>$null | ConvertFrom-Json
        
        Write-Host "Eventos de falha:" -ForegroundColor Red
        foreach ($event in $EVENTS.StackEvents) {
            if ($event.ResourceStatus -match "FAILED") {
                Write-Host "Recurso: $($event.LogicalResourceId)" -ForegroundColor Yellow
                Write-Host "Status: $($event.ResourceStatus)" -ForegroundColor Red
                Write-Host "Motivo: $($event.ResourceStatusReason)" -ForegroundColor Red
                Write-Host ""
            }
        }
        
        exit 1
    }
}

if ($STACK_STATUS -ne "CREATE_COMPLETE") {
    Write-Host "Timeout aguardando criacao da stack" -ForegroundColor Red
    exit 1
}

Write-Host "Stack criada com sucesso" -ForegroundColor Green
Write-Host ""

# Step 5: Clean up temporary bucket
Write-Host "[5/8] Limpando bucket temporario..." -ForegroundColor Yellow
aws s3 rm s3://$TEMP_BUCKET/lambda-code.zip --region $REGION
aws s3 rb s3://$TEMP_BUCKET --region $REGION
Write-Host "Bucket temporario removido" -ForegroundColor Green
Write-Host ""

# Step 6: Get outputs
Write-Host "[6/8] Obtendo configuracoes..." -ForegroundColor Yellow
$LAMBDA_URL = aws cloudformation describe-stacks --stack-name $STACK_NAME --query "Stacks[0].Outputs[?OutputKey=='LambdaFunctionUrl'].OutputValue" --output text --region $REGION

$FRONTEND_BUCKET = aws cloudformation describe-stacks --stack-name $STACK_NAME --query "Stacks[0].Outputs[?OutputKey=='FrontendBucketName'].OutputValue" --output text --region $REGION

$OUTPUT_BUCKET = aws cloudformation describe-stacks --stack-name $STACK_NAME --query "Stacks[0].Outputs[?OutputKey=='OutputBucketName'].OutputValue" --output text --region $REGION

$DYNAMODB_TABLE = aws cloudformation describe-stacks --stack-name $STACK_NAME --query "Stacks[0].Outputs[?OutputKey=='ExecutionCounterTableName'].OutputValue" --output text --region $REGION

Write-Host "Lambda URL: $LAMBDA_URL"
Write-Host "Frontend Bucket: $FRONTEND_BUCKET"
Write-Host "Output Bucket: $OUTPUT_BUCKET"
Write-Host "DynamoDB Table: $DYNAMODB_TABLE"
Write-Host "Configuracoes obtidas" -ForegroundColor Green
Write-Host ""

# Step 7: Build and deploy frontend
Write-Host "[7/8] Compilando e fazendo deploy do Frontend..." -ForegroundColor Yellow
Push-Location partners-promo-frontend

# Create .env file
"VITE_API_ENDPOINT=$LAMBDA_URL" | Out-File -FilePath .env -Encoding UTF8
Write-Host "Arquivo .env criado com Lambda URL"

# Install dependencies if needed
if (-not (Test-Path "node_modules")) {
    Write-Host "Instalando dependencias do npm..."
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Erro ao instalar dependencias" -ForegroundColor Red
        Pop-Location
        exit 1
    }
}

# Build
Write-Host "Compilando frontend..."
npm run build
if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao compilar frontend" -ForegroundColor Red
    Pop-Location
    exit 1
}

# Upload to S3
Write-Host "Fazendo upload para S3..."
aws s3 sync dist/ s3://$FRONTEND_BUCKET --delete --region $REGION
if ($LASTEXITCODE -ne 0) {
    Write-Host "Erro ao fazer upload para S3" -ForegroundColor Red
    Pop-Location
    exit 1
}

Pop-Location
Write-Host "Frontend deployado" -ForegroundColor Green
Write-Host ""

# Step 8: Get final URLs
Write-Host "[8/8] Obtendo URLs finais..." -ForegroundColor Yellow
$FRONTEND_URL = aws cloudformation describe-stacks --stack-name $STACK_NAME --query "Stacks[0].Outputs[?OutputKey=='FrontendWebsiteURL'].OutputValue" --output text --region $REGION

Write-Host "Deploy completo!" -ForegroundColor Green
Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Deploy Concluido com Sucesso!" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Frontend URL:" -ForegroundColor Cyan
Write-Host "   $FRONTEND_URL"
Write-Host ""
Write-Host "Lambda Function URL:" -ForegroundColor Cyan
Write-Host "   $LAMBDA_URL"
Write-Host ""
Write-Host "Recursos Criados:" -ForegroundColor Cyan
Write-Host "   - Frontend Bucket: $FRONTEND_BUCKET"
Write-Host "   - Output Bucket: $OUTPUT_BUCKET"
Write-Host "   - DynamoDB Table: $DYNAMODB_TABLE"
Write-Host "   - Lambda Function: $LAMBDA_FUNCTION"
Write-Host ""
Write-Host "Configuracoes:" -ForegroundColor Cyan
Write-Host "   - Verification Code: ********"
Write-Host "   - Daily Limit: $DailyLimit execucoes/dia"
Write-Host ""
Write-Host "Monitoramento:" -ForegroundColor Cyan
Write-Host "   CloudWatch Logs: /aws/lambda/$LAMBDA_FUNCTION"
Write-Host ""
Write-Host "Acesse a aplicacao em:" -ForegroundColor Cyan
Write-Host "   $FRONTEND_URL"
Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
