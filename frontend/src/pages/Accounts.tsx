import { FormEvent, useEffect, useState } from 'react';
import api from '../api/client';
import { MailAccountDto } from '../api/types';

const emptyCustom = {
  displayName: '',
  emailAddress: '',
  smtpHost: '',
  smtpPort: 587,
  smtpUseSsl: true,
  smtpUsername: '',
  smtpPassword: '',
  imapHost: '',
  imapPort: 993,
  imapUseSsl: true,
  imapUsername: '',
  imapPassword: '',
};

const AccountsPage = () => {
  const [accounts, setAccounts] = useState<MailAccountDto[]>([]);
  const [custom, setCustom] = useState({ ...emptyCustom });
  const [status, setStatus] = useState<string | null>(null);

  const load = () => api.get<MailAccountDto[]>('/api/mail-accounts').then((res) => setAccounts(res.data));
  useEffect(() => {
    load();
  }, []);

  const handleCustomSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setStatus(null);
    await api.post('/api/mail-accounts/custom', custom);
    setCustom({ ...emptyCustom });
    setStatus('Hesap eklendi');
    load();
  };

  const startGmail = async () => {
    const res = await api.post<{ authorizationUrl: string; state: string }>('/api/mail-accounts/gmail/start-auth');
    window.location.href = res.data.authorizationUrl;
  };

  return (
    <div className="container">
      <h2>Mail Hesapları</h2>
      <div className="grid columns-2">
        <div className="card">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h4>Bağlı hesaplar</h4>
            <button onClick={startGmail}>Gmail bağla</button>
          </div>
          <table>
            <thead>
              <tr>
                <th>Email</th>
                <th>Tip</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {accounts.map((acc) => (
                <tr key={acc.id}>
                  <td>{acc.emailAddress}</td>
                  <td>{acc.providerType === 1 ? 'Gmail' : 'Custom SMTP'}</td>
                  <td>{acc.status === 1 ? 'Bağlı' : 'Bekliyor'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="card">
          <h4>Custom SMTP/IMAP ekle</h4>
          <form onSubmit={handleCustomSubmit}>
            <input placeholder="Görünen ad" value={custom.displayName} onChange={(e) => setCustom({ ...custom, displayName: e.target.value })} required />
            <input placeholder="Email" value={custom.emailAddress} onChange={(e) => setCustom({ ...custom, emailAddress: e.target.value })} required />
            <div className="grid columns-2">
              <input placeholder="SMTP host" value={custom.smtpHost} onChange={(e) => setCustom({ ...custom, smtpHost: e.target.value })} required />
              <input placeholder="SMTP port" type="number" value={custom.smtpPort} onChange={(e) => setCustom({ ...custom, smtpPort: Number(e.target.value) })} required />
            </div>
            <label>
              <input type="checkbox" checked={custom.smtpUseSsl} onChange={(e) => setCustom({ ...custom, smtpUseSsl: e.target.checked })} /> SMTP SSL
            </label>
            <input placeholder="SMTP kullanıcı" value={custom.smtpUsername} onChange={(e) => setCustom({ ...custom, smtpUsername: e.target.value })} required />
            <input placeholder="SMTP şifre" type="password" value={custom.smtpPassword} onChange={(e) => setCustom({ ...custom, smtpPassword: e.target.value })} required />
            <div className="grid columns-2">
              <input placeholder="IMAP host" value={custom.imapHost} onChange={(e) => setCustom({ ...custom, imapHost: e.target.value })} required />
              <input placeholder="IMAP port" type="number" value={custom.imapPort} onChange={(e) => setCustom({ ...custom, imapPort: Number(e.target.value) })} required />
            </div>
            <label>
              <input type="checkbox" checked={custom.imapUseSsl} onChange={(e) => setCustom({ ...custom, imapUseSsl: e.target.checked })} /> IMAP SSL
            </label>
            <input placeholder="IMAP kullanıcı" value={custom.imapUsername} onChange={(e) => setCustom({ ...custom, imapUsername: e.target.value })} required />
            <input placeholder="IMAP şifre" type="password" value={custom.imapPassword} onChange={(e) => setCustom({ ...custom, imapPassword: e.target.value })} required />
            {status && <span>{status}</span>}
            <button type="submit">Kaydet</button>
          </form>
        </div>
      </div>
    </div>
  );
};

export default AccountsPage;
