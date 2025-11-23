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
    <div className="container">
      <h2>Warmup Logları</h2>
      <div className="card">
        <div className="grid columns-2">
          <select value={filters.mailAccountId} onChange={(e) => setFilters({ ...filters, mailAccountId: e.target.value || undefined })}>
            <option value="">Tüm hesaplar</option>
            {accounts.map((a) => (
              <option key={a.id} value={a.id}>
                {a.emailAddress}
              </option>
            ))}
          </select>
          <div style={{ display: 'flex', gap: 8 }}>
            <input type="date" value={filters.from} onChange={(e) => setFilters({ ...filters, from: e.target.value || undefined })} />
            <input type="date" value={filters.to} onChange={(e) => setFilters({ ...filters, to: e.target.value || undefined })} />
            <button onClick={load}>Filtrele</button>
          </div>
        </div>
        <table>
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
                <td>{log.direction === 0 ? 'Gönderildi' : log.direction === 1 ? 'Alındı' : 'Yanıtlandı'}</td>
                <td>{log.sentAt ? new Date(log.sentAt).toLocaleString() : '-'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default LogsPage;
