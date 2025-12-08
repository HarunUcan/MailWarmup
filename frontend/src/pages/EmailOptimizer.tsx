import { FormEvent, useState } from 'react';
import api from '../api/client';
import { AiEmailOptimizeRequest, AiEmailOptimizeResponse } from '../api/types';
import { useToast } from '../state/ToastContext';

const initialRequest: AiEmailOptimizeRequest = {
  subject: '',
  body: '',
  mode: 'SpamSafe',
  language: 'tr',
};

const EmailOptimizerPage = () => {
  const [request, setRequest] = useState<AiEmailOptimizeRequest>(initialRequest);
  const [result, setResult] = useState<AiEmailOptimizeResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const toast = useToast();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!request.subject.trim() && !request.body.trim()) {
      setError('Konu veya gövde alanından en az biri doldurulmalı.');
      return;
    }

    setError(null);
    setLoading(true);
    try {
      const res = await api.post<AiEmailOptimizeResponse>('/api/ai/email/optimize', request);
      setResult(res.data);
      toast.push('E-posta optimize edildi.', 'success');
    } catch (err: any) {
      const message = err?.response?.data ?? 'Optimize işlemi başarısız oldu.';
      const text = typeof message === 'string' ? message : 'Optimize işlemi başarısız oldu.';
      setError(text);
      toast.push(text, 'error');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="page optimizer-page">
      <div className="page-header">
        <div>
          <div className="page-title">Email Deliverability Optimizer</div>
          <div className="page-subtitle">
            Solda e-postanızı yazın, sağda yapay zekâ tarafından spam riskine göre optimize edilmiş hâlini görün.
          </div>
        </div>
      </div>

      {error && <div className="alert error">{error}</div>}

      <div className="optimizer-layout">
        <form className="card optimizer-card" onSubmit={handleSubmit}>
          <div className="card-header">
            <div className="card-title">Orijinal E-posta</div>
          </div>

          <div className="field">
            <label>Konu (Subject)</label>
            <input
              className="input"
              placeholder="Örn: Haftalık güncelleme notları"
              value={request.subject}
              onChange={(e) => setRequest({ ...request, subject: e.target.value })}
            />
          </div>

          <div className="field">
            <label>Gövde (Body)</label>
            <textarea
              className="textarea optimizer-textarea"
              placeholder="E-postanızı buraya yazın..."
              value={request.body}
              onChange={(e) => setRequest({ ...request, body: e.target.value })}
            />
          </div>

          <div className="grid two">
            <div className="field">
              <label>Mod</label>
              <select
                className="select"
                value={request.mode}
                onChange={(e) => setRequest({ ...request, mode: e.target.value as AiEmailOptimizeRequest['mode'] })}
              >
                <option value="SpamSafe">Spam Safe (Tonumu Koru)</option>
                <option value="SpamSafeAndProfessional">Spam Safe + Profesyonel</option>
              </select>
            </div>
            <div className="field">
              <label>Dil</label>
              <select
                className="select"
                value={request.language}
                onChange={(e) => setRequest({ ...request, language: e.target.value })}
              >
                <option value="tr">Türkçe (tr)</option>
                <option value="en">English (en)</option>
              </select>
            </div>
          </div>

          <div className="optimizer-actions">
            {loading && <span className="hint">Optimize ediliyor...</span>}
            <button className="btn btn-primary" type="submit" disabled={loading}>
              {loading ? 'Çalışıyor...' : 'Optimize Et'}
            </button>
          </div>
        </form>

        <div className="card optimizer-card">
          <div className="card-header">
            <div className="card-title">Optimize Edilmiş E-posta</div>
          </div>

          {result ? (
            <div className="stack">
              <div className="field">
                <label>Konu (AI Sonuç)</label>
                <input className="input" value={result.optimizedSubject} readOnly />
              </div>
              <div className="field">
                <label>Gövde</label>
                <textarea className="textarea optimizer-textarea" value={result.optimizedBody} readOnly />
              </div>
              <div className="field">
                <label>Ne Değişti?</label>
                <div className="explanation-box">{result.explanationSummary}</div>
              </div>
            </div>
          ) : (
            <div className="optimizer-placeholder">Optimize edilmiş e-posta burada görünecek.</div>
          )}
        </div>
      </div>
    </div>
  );
};

export default EmailOptimizerPage;
