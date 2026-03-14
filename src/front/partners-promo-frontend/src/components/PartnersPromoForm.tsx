import { useState, FormEvent } from 'react';
import { processPartnersPromoRequest, ApiError } from '../services/apiClient';
import type { FormState } from '../types/form';
import './PartnersPromoForm.css';

export function PartnersPromoForm() {
  const [formState, setFormState] = useState<FormState>({
    minimumScore: '',
    verificationCode: '',
    isLoading: false,
    error: null,
    downloadUrl: null,
    fileName: null,
  });

  const validateMinimumScore = (value: string): string | null => {
    if (!value.trim()) {
      return 'Pontuação mínima é obrigatória';
    }
    
    if (!/^\d+$/.test(value)) {
      return 'Pontuação mínima deve ser um número inteiro';
    }

    const numValue = parseInt(value, 10);
    if (numValue <= 0) {
      return 'Pontuação mínima deve ser maior que zero';
    }

    return null;
  };

  const validateVerificationCode = (value: string): string | null => {
    if (!value.trim()) {
      return 'Código de verificação é obrigatório';
    }
    return null;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    // Clear previous state
    setFormState(prev => ({
      ...prev,
      error: null,
      downloadUrl: null,
      fileName: null,
    }));

    // Validate inputs
    const scoreError = validateMinimumScore(formState.minimumScore);
    if (scoreError) {
      setFormState(prev => ({ ...prev, error: scoreError }));
      return;
    }

    const codeError = validateVerificationCode(formState.verificationCode);
    if (codeError) {
      setFormState(prev => ({ ...prev, error: codeError }));
      return;
    }

    // Set loading state
    setFormState(prev => ({ ...prev, isLoading: true }));

    try {
      const response = await processPartnersPromoRequest({
        minimumScore: parseInt(formState.minimumScore, 10),
        verificationCode: formState.verificationCode,
      });

      setFormState(prev => ({
        ...prev,
        isLoading: false,
        downloadUrl: response.downloadUrl,
        fileName: response.fileName,
      }));
    } catch (error) {
      let errorMessage = 'Ocorreu um erro ao processar sua requisição';

      if (error instanceof ApiError) {
        switch (error.statusCode) {
          case 401:
            errorMessage = 'Código de verificação inválido';
            break;
          case 400:
            errorMessage = error.message;
            break;
          case 429:
            errorMessage = 'Limite diário de execuções atingido. Tente novamente amanhã.';
            break;
          case 408:
            errorMessage = 'Tempo limite excedido. Tente novamente.';
            break;
          case 500:
            errorMessage = 'Erro no servidor. Tente novamente mais tarde.';
            break;
          default:
            errorMessage = error.message;
        }
      }

      setFormState(prev => ({
        ...prev,
        isLoading: false,
        error: errorMessage,
      }));
    }
  };

  const handleInputChange = (field: 'minimumScore' | 'verificationCode', value: string) => {
    setFormState(prev => ({
      ...prev,
      [field]: value,
      error: null, // Clear error when user types
    }));
  };

  return (
    <div className="form-container">
      <h1>Promoção de Parceiros</h1>
      <p className="subtitle">Gere um arquivo CSV com as paridades disponíveis</p>

      <form onSubmit={handleSubmit} className="partners-promo-form">
        <div className="form-group">
          <label htmlFor="minimumScore">Pontuação Mínima</label>
          <input
            type="text"
            id="minimumScore"
            value={formState.minimumScore}
            onChange={(e) => handleInputChange('minimumScore', e.target.value)}
            disabled={formState.isLoading}
            placeholder="Ex: 1000"
            autoComplete="off"
          />
        </div>

        <div className="form-group">
          <label htmlFor="verificationCode">Código de Verificação</label>
          <input
            type="password"
            id="verificationCode"
            value={formState.verificationCode}
            onChange={(e) => handleInputChange('verificationCode', e.target.value)}
            disabled={formState.isLoading}
            placeholder="Digite o código"
            autoComplete="off"
          />
        </div>

        <button 
          type="submit" 
          disabled={formState.isLoading}
          className="submit-button"
        >
          {formState.isLoading ? 'Processando...' : 'Gerar CSV'}
        </button>
      </form>

      {formState.isLoading && (
        <div className="loading">
          <div className="spinner"></div>
          <p>Processando paridades... Isso pode levar alguns minutos.</p>
        </div>
      )}

      {formState.error && (
        <div className="error-message">
          <strong>Erro:</strong> {formState.error}
        </div>
      )}

      {formState.downloadUrl && formState.fileName && (
        <div className="success-message">
          <h3>✓ Arquivo gerado com sucesso!</h3>
          <p>Arquivo: <strong>{formState.fileName}</strong></p>
          <a 
            href={formState.downloadUrl} 
            download={formState.fileName}
            className="download-button"
          >
            Baixar CSV
          </a>
          <p className="expiry-note">
            O link expira em 1 hora
          </p>
        </div>
      )}
    </div>
  );
}
