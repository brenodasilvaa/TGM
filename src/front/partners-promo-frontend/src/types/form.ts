export interface PartnersPromoFormData {
  minimumScore: number;
  verificationCode: string;
}

export interface FormState {
  minimumScore: string;
  verificationCode: string;
  isLoading: boolean;
  error: string | null;
  downloadUrl: string | null;
  fileName: string | null;
}
