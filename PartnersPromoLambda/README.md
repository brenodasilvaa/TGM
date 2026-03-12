# Partners Promo Lambda Function

AWS Lambda function in .NET 8 that processes partners promo requests and generates CSV files.

## Environment Variables

The following environment variables must be configured in the Lambda function:

- `VERIFICATION_CODE`: Secret code for request authentication (required)
- `DAILY_EXECUTION_LIMIT`: Maximum number of executions per day (default: 4)
- `S3_BUCKET_NAME`: S3 bucket name for CSV output storage (required)
- `DYNAMODB_TABLE_NAME`: DynamoDB table name for execution counter tracking (required)
- `AWS_REGION`: AWS region (default: us-east-1)

## Lambda Configuration

- **Runtime**: .NET 8 (dotnet8)
- **Memory**: 1024 MB (recommended)
- **Timeout**: 300 seconds (5 minutes)
- **Handler**: PartnersPromoLambda::PartnersPromoLambda.Handlers.PartnersPromoHandler::HandleRequest

## IAM Permissions Required

The Lambda execution role must have the following permissions:

### S3 Permissions
```json
{
  "Effect": "Allow",
  "Action": [
    "s3:PutObject",
    "s3:GetObject"
  ],
  "Resource": "arn:aws:s3:::your-bucket-name/*"
}
```

### DynamoDB Permissions
```json
{
  "Effect": "Allow",
  "Action": [
    "dynamodb:GetItem",
    "dynamodb:PutItem",
    "dynamodb:UpdateItem"
  ],
  "Resource": "arn:aws:dynamodb:us-east-1:*:table/your-table-name"
}
```

### CloudWatch Logs Permissions
```json
{
  "Effect": "Allow",
  "Action": [
    "logs:CreateLogGroup",
    "logs:CreateLogStream",
    "logs:PutLogEvents"
  ],
  "Resource": "arn:aws:logs:*:*:*"
}
```

## DynamoDB Table Structure

Create a DynamoDB table with the following structure:

- **Table Name**: As specified in `DYNAMODB_TABLE_NAME` environment variable
- **Partition Key**: `Date` (String) - Format: yyyy-MM-dd
- **Attributes**:
  - `Date` (String): The date in yyyy-MM-dd format
  - `Count` (Number): Number of executions for that date
  - `LastUpdated` (String): ISO 8601 timestamp of last update

## S3 Bucket Configuration

The S3 bucket for CSV output should have:
- Private access (no public read)
- Versioning enabled (optional but recommended)
- Lifecycle policy to delete old files (optional)

## Deployment

### Using AWS Lambda Tools

1. Install AWS Lambda Tools for .NET:
```bash
dotnet tool install -g Amazon.Lambda.Tools
```

2. Deploy the function:
```bash
cd PartnersPromoLambda
dotnet lambda deploy-function PartnersPromoFunction
```

3. Follow the prompts to configure:
   - Function name
   - IAM role
   - Memory size (1024 MB)
   - Timeout (300 seconds)

### Using AWS CLI

1. Build and package:
```bash
cd PartnersPromoLambda
dotnet lambda package -o ../deploy-package.zip
```

2. Create or update function:
```bash
aws lambda create-function \
  --function-name PartnersPromoFunction \
  --runtime dotnet8 \
  --role arn:aws:iam::YOUR_ACCOUNT:role/lambda-execution-role \
  --handler PartnersPromoLambda::PartnersPromoLambda.Handlers.PartnersPromoHandler::HandleRequest \
  --zip-file fileb://../deploy-package.zip \
  --timeout 300 \
  --memory-size 1024 \
  --environment Variables="{VERIFICATION_CODE=your-secret-code,DAILY_EXECUTION_LIMIT=4,S3_BUCKET_NAME=your-bucket,DYNAMODB_TABLE_NAME=your-table}"
```

## Function URL Configuration

After deployment, enable Function URL:

1. Go to Lambda Console → Your Function → Configuration → Function URL
2. Click "Create function URL"
3. Configure:
   - **Auth type**: NONE (public access)
   - **CORS**: Enable
   - **Allowed origins**: * (or specific domain)
   - **Allowed methods**: POST, OPTIONS
   - **Allowed headers**: Content-Type
   - **Max age**: 300

4. Save and copy the Function URL (format: `https://<url-id>.lambda-url.<region>.on.aws/`)

## Testing

### Local Testing

You can test the handler locally using the AWS Lambda Test Tool:

```bash
dotnet lambda invoke-function PartnersPromoFunction --payload '{"body":"{\"minimumScore\":1000,\"verificationCode\":\"your-code\"}"}'
```

### Testing via Function URL

```bash
curl -X POST https://your-function-url.lambda-url.us-east-1.on.aws/ \
  -H "Content-Type: application/json" \
  -d '{"minimumScore":1000,"verificationCode":"your-secret-code"}'
```

## Monitoring

Monitor the function using:
- CloudWatch Logs: `/aws/lambda/PartnersPromoFunction`
- CloudWatch Metrics: Invocations, Duration, Errors, Throttles
- X-Ray Tracing (optional): Enable for detailed request tracing

## Troubleshooting

### Common Issues

1. **Timeout errors**: Increase timeout or optimize parity processing
2. **Out of memory**: Increase memory allocation
3. **DynamoDB throttling**: Increase table capacity or use on-demand billing
4. **S3 access denied**: Check IAM permissions and bucket policy
5. **CORS errors**: Verify Function URL CORS configuration

### Logs

Check CloudWatch Logs for detailed error messages and stack traces.
