# Web Interface Parity Export - Project Summary

## Overview

This project implements a serverless web application for exporting parity data to CSV files. The system consists of a React frontend hosted on S3 and a .NET 8 Lambda function that processes parity requests.

## Architecture

```
User Browser
    ↓
S3 Static Website (React Frontend)
    ↓
Lambda Function URL (HTTPS)
    ↓
Lambda Function (.NET 8)
    ↓
├── DynamoDB (Execution Counter)
├── TGM Services (Parity Processing)
└── S3 (CSV Output)
    ↓
Presigned URL → User Download
```

## Components

### Backend (ParityExportLambda/)

**Technology**: .NET 8 AWS Lambda

**Key Files**:
- `Handlers/ParityExportHandler.cs` - Main Lambda handler
- `Services/ValidationService.cs` - Request validation
- `Services/ExecutionLimitService.cs` - Daily execution limit control
- `Services/ParityProcessingOrchestrator.cs` - Parity processing coordination
- `Services/CsvGenerationService.cs` - CSV generation and S3 upload
- `Services/PresignedUrlService.cs` - Presigned URL generation
- `Models/` - Request/Response models

**Dependencies**:
- Amazon.Lambda.Core
- Amazon.Lambda.APIGatewayEvents
- AWSSDK.S3
- AWSSDK.DynamoDBv2
- TGM project (shared code)

**Configuration**:
- Runtime: dotnet8
- Memory: 1024 MB
- Timeout: 300 seconds
- Handler: ParityExportLambda::ParityExportLambda.Handlers.ParityExportHandler::HandleRequest

### Frontend (parity-export-frontend/)

**Technology**: React 18 + TypeScript + Vite

**Key Files**:
- `src/components/ParityExportForm.tsx` - Main form component
- `src/services/apiClient.ts` - API communication
- `src/types/` - TypeScript type definitions

**Features**:
- Client-side validation
- Loading states
- Error handling
- Download link generation
- Responsive design

## Environment Variables

### Backend (Lambda)
- `VERIFICATION_CODE` - Secret authentication code
- `DAILY_EXECUTION_LIMIT` - Max executions per day (default: 4)
- `S3_BUCKET_NAME` - Output bucket name
- `DYNAMODB_TABLE_NAME` - Execution counter table
- `AWS_REGION` - AWS region

### Frontend
- `VITE_API_ENDPOINT` - Lambda Function URL

## AWS Resources Required

1. **Lambda Function**
   - Runtime: .NET 8
   - Function URL enabled (public, CORS configured)
   - IAM role with S3, DynamoDB, CloudWatch permissions

2. **S3 Buckets**
   - Frontend bucket (static website hosting, public read)
   - Output bucket (private, presigned URL access)

3. **DynamoDB Table**
   - Partition key: `Date` (String)
   - Attributes: `Count` (Number), `LastUpdated` (String)

4. **IAM Permissions**
   - Lambda execution role needs:
     - S3: PutObject, GetObject
     - DynamoDB: GetItem, PutItem, UpdateItem
     - CloudWatch Logs: CreateLogGroup, CreateLogStream, PutLogEvents

## Deployment Steps

### Backend

1. Build Lambda package:
```bash
cd ParityExportLambda
dotnet lambda package -o deploy-package.zip
```

2. Deploy to AWS Lambda (manual or via AWS CLI)

3. Enable Function URL with CORS

4. Configure environment variables

### Frontend

1. Install dependencies:
```bash
cd parity-export-frontend
npm install
```

2. Configure `.env` with Lambda Function URL

3. Build:
```bash
npm run build
```

4. Upload to S3:
```bash
aws s3 sync dist/ s3://your-bucket-name --delete
```

## API Contract

### POST /

**Request**:
```json
{
  "minimumScore": 1000,
  "verificationCode": "secret-code"
}
```

**Success Response (200)**:
```json
{
  "downloadUrl": "https://bucket.s3.amazonaws.com/file.csv?...",
  "fileName": "parity-export-2024-01-15T10-30-00.csv",
  "expiresAt": "2024-01-15T11:30:00Z"
}
```

**Error Responses**:
- 400: Bad Request (invalid input)
- 401: Unauthorized (invalid verification code)
- 429: Too Many Requests (daily limit reached)
- 500: Internal Server Error

## CSV Output Format

```csv
Programa;Parceiro;Bonificação;Validade;Termos Legais
Livelo;Partner A;1.5x;2024-12-31;Terms text
Esfera;Partner B;2.0x;2024-11-30;Terms text
```

## Cost Estimation

**Monthly costs (4 executions/day, 120/month)**:

- Lambda: $0.00 (within free tier)
- S3 Frontend: $0.00 (within free tier)
- S3 Output: $0.00 (within free tier)
- DynamoDB: $0.00 (within free tier)
- CloudWatch: $0.00 (within free tier)

**Total with free tier**: $0.00/month
**Total after free tier**: $0.50 - $1.50/month

## Security Features

1. **Authentication**: Verification code validation
2. **Rate Limiting**: Daily execution limit (4/day)
3. **Presigned URLs**: Temporary access (1 hour expiration)
4. **CORS**: Configured on Lambda Function URL
5. **Error Handling**: No sensitive data in error responses
6. **Logging**: CloudWatch Logs for monitoring

## Testing

### Backend
```bash
cd ParityExportLambda
dotnet build
```

### Frontend
```bash
cd parity-export-frontend
npm install
npm run dev
```

## Documentation

- Backend: `ParityExportLambda/README.md`
- Frontend: `parity-export-frontend/README.md`
- Deployment: `parity-export-frontend/DEPLOYMENT.md`
- Spec: `.kiro/specs/web-interface-parity-export/`

## Project Structure

```
.
├── ParityExportLambda/          # Backend Lambda function
│   ├── Handlers/
│   ├── Services/
│   ├── Models/
│   └── README.md
├── parity-export-frontend/      # Frontend React app
│   ├── src/
│   │   ├── components/
│   │   ├── services/
│   │   └── types/
│   ├── README.md
│   └── DEPLOYMENT.md
├── Models/                      # TGM shared models
├── Services/                    # TGM shared services
├── Repositories/                # TGM shared repositories
├── Helpers/                     # TGM shared helpers
└── PROJECT_SUMMARY.md          # This file
```

## Next Steps

1. Deploy Lambda function to AWS
2. Create DynamoDB table
3. Create S3 buckets
4. Configure IAM roles and permissions
5. Enable Lambda Function URL
6. Deploy frontend to S3
7. Test end-to-end flow
8. Monitor CloudWatch Logs

## Support

For issues or questions:
1. Check CloudWatch Logs for Lambda errors
2. Verify environment variables are set correctly
3. Ensure IAM permissions are configured
4. Check S3 bucket policies
5. Verify DynamoDB table exists and is accessible
