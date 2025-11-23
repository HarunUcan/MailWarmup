import { useEffect, useState } from 'react';
import api from '../api/client';
import { MailAccountDto, WarmupLogDto } from '../api/types';

const LogsPage = () => {
  const [logs, setLogs] = useState<WarmupLogDto[]>([]);
  const [accounts, setAccounts] = useState<MailAccountDto[]>([]);
  const [filters, setFilters] = useState<{ mailAccountId?: string; from?: string; to?: string }>({});

  const load = async () => {
    const res = await api.get<WarmupLogDto[]>('/api/logs', { params: filters });
    setLogs(res.data);
  };

  useEffect(() => {
    api.get<MailAccountDto[]>('/api/mail-accounts').then((res) => setAccounts(res.data));
  }, []);

  useEffect(() => {
    load();
  }, []);

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
              <button className="btn btn-ghost" onClick={load}>
                Uygula
              </button>
            </div>
            <div className="hint">Aralığı boş bırakırsanız tüm kayıtlar gelir.</div>
          </div>
        </div>

        <table className="table">
          <thead>
            <tr>
              <th>Hesap</th>
              <th>Konu</th>
              <th>Yön</th>
              <th>Tarih</th>
            </tr>
          </thead>
          <tbody>
            {logs.map((log) => (
              <tr key={log.id}>
                <td>{accounts.find((a) => a.id === log.mailAccountId)?.emailAddress ?? 'N/A'}</td>
                <td>{log.subject}</td>
                <td>{log.direction === 0 ? 'Gönderildi' : log.direction === 1 ? 'Alındı' : log.direction === 2 ? 'Yanıt' : 'İşlem'}</td>
                <td>{log.sentAt ? new Date(log.sentAt).toLocaleString() : '-'}</td>
              </tr>
            ))}
            {!logs.length && (
              <tr>
                <td colSpan={4}>Kayıt bulunamadı.</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default LogsPage;
