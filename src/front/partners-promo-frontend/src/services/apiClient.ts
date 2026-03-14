import type { ProcessPartnersPromoRequest, ProcessPartnersPromoResponse, ApiErrorResponse } from '../types/api';

const API_ENDPOINT = import.meta.env.VITE_API_ENDPOINT;
const REQUEST_TIMEOUT = 600000; // 10 minutes

export class ApiError extends Error {
  constructor(
    public statusCode: number,
    public error: string,
    message: string
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export async function processPartnersPromoRequest(
  request: ProcessPartnersPromoRequest
): Promise<ProcessPartnersPromoResponse> {
  if (!API_ENDPOINT) {
    throw new Error('API endpoint not configured. Please set VITE_API_ENDPOINT environment variable.');
  }

  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), REQUEST_TIMEOUT);

  try {
    const response = await fetch(API_ENDPOINT, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
      signal: controller.signal,
    });

    clearTimeout(timeoutId);

    if (!response.ok) {
      // Try to parse error response
      try {
        const errorData: ApiErrorResponse = await response.json();
        throw new ApiError(response.status, errorData.error, errorData.message);
      } catch (parseError) {
        // If parsing fails, throw generic error
        throw new ApiError(
          response.status,
          'UnknownError',
          `Request failed with status ${response.status}`
        );
      }
    }

    const data: ProcessPartnersPromoResponse = await response.json();
    return data;
  } catch (error) {
    clearTimeout(timeoutId);

    if (error instanceof ApiError) {
      throw error;
    }

    if (error instanceof Error) {
      if (error.name === 'AbortError') {
        throw new ApiError(
          408,
          'Timeout',
          'Request timed out. Please try again.'
        );
      }

      throw new ApiError(
        0,
        'NetworkError',
        `Network error: ${error.message}`
      );
    }

    throw new ApiError(
      0,
      'UnknownError',
      'An unknown error occurred'
    );
  }
}
