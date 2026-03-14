export interface ProcessPartnersPromoRequest {
  minimumScore: number;
  verificationCode: string;
}

export interface ProcessPartnersPromoResponse {
  downloadUrl: string;
  fileName: string;
  expiresAt: string;
}

export interface ApiErrorResponse {
  error: string;
  message: string;
}
