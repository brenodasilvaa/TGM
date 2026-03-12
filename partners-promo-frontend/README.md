# Partners Promo Frontend

React + TypeScript frontend for the Partners Promo system.

## Setup

1. Install dependencies:
```bash
npm install
```

2. Configure environment variables:
```bash
cp .env.example .env
```

Edit `.env` and set your Lambda Function URL:
```
VITE_API_ENDPOINT=https://your-function-url.lambda-url.us-east-1.on.aws/
```

## Development

Run the development server:
```bash
npm run dev
```

The app will be available at `http://localhost:5173`

## Build for Production

Build the static files:
```bash
npm run build
```

The output will be in the `dist/` directory.

## Deployment to S3

### Prerequisites

- AWS CLI installed and configured
- S3 bucket created for static website hosting

### Steps

1. Build the project:
```bash
npm run build
```

2. Upload to S3:
```bash
aws s3 sync dist/ s3://your-bucket-name --delete
```

3. Configure S3 bucket for static website hosting:
```bash
aws s3 website s3://your-bucket-name --index-document index.html
```

4. Set bucket policy for public read access:
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicReadGetObject",
      "Effect": "Allow",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::your-bucket-name/*"
    }
  ]
}
```

5. Access your site at:
```
http://your-bucket-name.s3-website-us-east-1.amazonaws.com
```

## Project Structure

```
src/
├── components/       # React components
├── services/         # API client services
├── types/           # TypeScript type definitions
├── App.tsx          # Main app component
├── App.css          # App styles
├── main.tsx         # Entry point
└── index.css        # Global styles
```

## Environment Variables

- `VITE_API_ENDPOINT`: Lambda Function URL endpoint

## Features

- Form validation (client-side)
- Loading states
- Error handling
- Download link generation
- Responsive design
