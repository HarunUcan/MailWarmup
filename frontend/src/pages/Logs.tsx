import { useEffect, useMemo, useState } from 'react';
import api from '../api/client';
import { MailAccountDto, WarmupLogDto } from '../api/types';
import { useToast } from '../state/ToastContext';

const PAGE_SIZE = 20;

const directionLabel = (dir: number) =>
  dir === 0 ? 'Gönderildi' : dir === 1 ? 'Alındı' : dir === 2 ? 'Yanıt' : 'İşlem';

const LogsPage = () => {
  const [logs, setLogs] = useState<WarmupLogDto[]>([]);
  const [accounts, setAccounts] = useState<MailAccountDto[]>([]);
  const [filters, setFilters] = useState<{ mailAccountId?: string; from?: string; to?: string }>({});
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [selectedLog, setSelectedLog] = useState<WarmupLogDto | null>(null);
  const toast = useToast();

  const paged = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return logs.slice(start, start + PAGE_SIZE);
  }, [logs, page]);

  const totalPages = Math.max(1, Math.ceil(logs.length / PAGE_SIZE));

  const load = async (showLoading = true) => {
    if (filters.from && filters.to && filters.from > filters.to) {
      toast.push('Başlangıç tarihi bitiş tarihinden büyük olamaz.', 'error');
      return;
    }

    try {
      if (showLoading) setLoading(true);
      const res = await api.get<WarmupLogDto[]>('/api/logs', { params: filters });
      setLogs(res.data);
      setPage(1);
    } catch (error: any) {
      toast.push(error?.response?.data ?? 'Loglar yüklenemedi.', 'error');
    } finally {
      if (showLoading) setLoading(false);
    }
  };

  useEffect(() => {
    api
      .get<MailAccountDto[]>('/api/mail-accounts')
      .then((res) => setAccounts(res.data))
      .catch(() => toast.push('Hesap listesi alınamadı.', 'error'));
  }, []);

  useEffect(() => {
    load();
    const interval = setInterval(() => load(false), 15000);
    return () => clearInterval(interval);
  }, [filters.from, filters.to, filters.mailAccountId]);

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="page-title">Warmup logları</div>
          <div className="page-subtitle">Gönderim, yanıt ve inbox aksiyonlarını günlük olarak inceleyin</div>
        </div>
      </div>

      <div className="card section">
        <div className="grid two">
          <div className="field">
            <label>Hesap filtresi</label>
            <select
              className="select"
              value={filters.mailAccountId ?? ''}
              onChange={(e) => setFilters({ ...filters, mailAccountId: e.target.value || undefined })}
            >
              <option value="">Tüm hesaplar</option>
              {accounts.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.emailAddress}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Tarih aralığı</label>
            <div className="row wrap">
              <input
                className="input"
                type="date"
                value={filters.from ?? ''}
                onChange={(e) => setFilters({ ...filters, from: e.target.value || undefined })}
              />
              <input
                className="input"
                type="date"
                value={filters.to ?? ''}
                onChange={(e) => setFilters({ ...filters, to: e.target.value || undefined })}
              />
              <button className="btn btn-primary" onClick={() => load()} disabled={loading} style={{ paddingInline: 14 }}>
                {loading ? 'Yükleniyor...' : 'Uygula'}
              </button>
            </div>
            <div className="hint">Aralığı boş bırakırsanız tüm kayıtlar gelir.</div>
          </div>
        </div>

        {loading ? (
          <div className="hint">Loglar yükleniyor...</div>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Hesap</th>
                <th>Konu</th>
                <th>Yön</th>
                <th>Tarih</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {paged.map((log) => (
                <tr key={log.id}>
                  <td>{accounts.find((a) => a.id === log.mailAccountId)?.emailAddress ?? 'N/A'}</td>
                  <td>{log.subject}</td>
                  <td>{directionLabel(log.direction)}</td>
                  <td>{log.sentAt ? new Date(log.sentAt).toLocaleString() : '-'}</td>
                  <td>
                    <button className="icon-btn" title="Detay" onClick={() => setSelectedLog(log)} style={{ color: '#2563eb' }}>
                      <i className="fa-solid fa-eye"></i>
                    </button>
                  </td>
                </tr>
              ))}
              {!paged.length && (
                <tr>
                  <td colSpan={5}>Kayıt bulunamadı.</td>
                </tr>
              )}
            </tbody>
          </table>
        )}

        <div className="row wrap" style={{ justifyContent: 'space-between', marginTop: 12 }}>
          <div className="hint">
            Sayfa {page}/{totalPages} · Toplam {logs.length} kayıt
          </div>
          <div className="row wrap" style={{ gap: 8 }}>
            <button className="btn btn-ghost" onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page === 1}>
              Önceki
            </button>
            <button className="btn btn-ghost" onClick={() => setPage((p) => Math.min(totalPages, p + 1))} disabled={page === totalPages}>
              Sonraki
            </button>
          </div>
        </div>
      </div>

      {selectedLog && (
        <div className="card section" style={{ marginTop: 16 }}>
          <div className="card-header">
            <div className="card-title">Log detayı</div>
            <button className="icon-btn" title="Kapat" onClick={() => setSelectedLog(null)} style={{ color: '#dc2626' }}>
              <i className="fa-solid fa-xmark"></i>
            </button>
          </div>
          <div className="grid two">
            <div className="field">
              <label>Konu</label>
              <div>{selectedLog.subject}</div>
            </div>
            <div className="field">
              <label>Mesaj ID</label>
              <div>{selectedLog.messageId}</div>
            </div>
            <div className="field">
              <label>Yön</label>
              <div>{directionLabel(selectedLog.direction)}</div>
            </div>
            <div className="field">
              <label>Gönderim tarihi</label>
              <div>{selectedLog.sentAt ? new Date(selectedLog.sentAt).toLocaleString() : '-'}</div>
            </div>
            <div className="field">
              <label>Önemli</label>
              <div>{selectedLog.markedAsImportant ? 'Evet' : 'Hayır'}</div>
            </div>
            <div className="field">
              <label>Spam</label>
              <div>{selectedLog.isSpam ? 'Evet' : 'Hayır'}</div>
            </div>
            <div className="field">
              <label>Kimden</label>
              <div>{selectedLog.fromAddress}</div>
            </div>
            <div className="field">
              <label>Kime</label>
              <div>{selectedLog.toAddress}</div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default LogsPage;
