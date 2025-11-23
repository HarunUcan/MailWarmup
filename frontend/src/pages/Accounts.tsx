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
    setStatus('Hesap eklendi ve doğrulama başlatıldı');
    load();
  };

  const startGmail = async () => {
    const res = await api.post<{ authorizationUrl: string; state: string }>('/api/mail-accounts/gmail/start-auth');
    window.location.href = res.data.authorizationUrl;
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="page-title">Mail hesapları</div>
          <div className="page-subtitle">Gmail veya custom SMTP/IMAP hesaplarını bağla ve sağlığını izle</div>
        </div>
        <div className="row wrap">
          <button className="btn btn-primary" onClick={startGmail}>
            Gmail bağla
          </button>
        </div>
      </div>

      <div className="grid two">
        <div className="card section">
          <div className="card-header">
            <div className="card-title">Bağlı hesaplar</div>
          </div>
          <table className="table">
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
                  <td>
                    {acc.status === 1 ? (
                      <span className="badge success">Bağlandı</span>
                    ) : acc.status === 2 ? (
                      <span className="badge muted">Pasif</span>
                    ) : (
                      <span className="badge warning">Bekliyor</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="card section">
          <div className="card-header">
            <div className="card-title">Yeni custom SMTP/IMAP</div>
          </div>
          <form className="stack" onSubmit={handleCustomSubmit}>
            <div className="grid two">
              <div className="field">
                <label>Görünen ad</label>
                <input
                  className="input"
                  placeholder="Kullanıcı adı"
                  value={custom.displayName}
                  onChange={(e) => setCustom({ ...custom, displayName: e.target.value })}
                  required
                />
                <div className="hint">Mail içinde görünecek isim.</div>
              </div>
              <div className="field">
                <label>Email adresi</label>
                <input
                  className="input"
                  placeholder="name@domain.com"
                  value={custom.emailAddress}
                  onChange={(e) => setCustom({ ...custom, emailAddress: e.target.value })}
                  required
                />
              </div>
            </div>

            <div className="section-title">SMTP</div>
            <div className="grid two">
              <div className="field">
                <label>SMTP host</label>
                <input
                  className="input"
                  placeholder="smtp.domain.com"
                  value={custom.smtpHost}
                  onChange={(e) => setCustom({ ...custom, smtpHost: e.target.value })}
                  required
                />
              </div>
              <div className="field">
                <label>SMTP port</label>
                <input
                  className="input"
                  type="number"
                  value={custom.smtpPort}
                  onChange={(e) => setCustom({ ...custom, smtpPort: Number(e.target.value) })}
                  required
                />
              </div>
            </div>
            <label className="checkbox-row">
              <input
                type="checkbox"
                checked={custom.smtpUseSsl}
                onChange={(e) => setCustom({ ...custom, smtpUseSsl: e.target.checked })}
              />
              SMTP SSL kullan
            </label>
            <div className="grid two">
              <div className="field">
                <label>SMTP kullanıcı</label>
                <input
                  className="input"
                  value={custom.smtpUsername}
                  onChange={(e) => setCustom({ ...custom, smtpUsername: e.target.value })}
                  required
                />
              </div>
              <div className="field">
                <label>SMTP şifre</label>
                <input
                  className="input"
                  type="password"
                  value={custom.smtpPassword}
                  onChange={(e) => setCustom({ ...custom, smtpPassword: e.target.value })}
                  required
                />
              </div>
            </div>

            <div className="section-title">IMAP</div>
            <div className="grid two">
              <div className="field">
                <label>IMAP host</label>
                <input
                  className="input"
                  placeholder="imap.domain.com"
                  value={custom.imapHost}
                  onChange={(e) => setCustom({ ...custom, imapHost: e.target.value })}
                  required
                />
              </div>
              <div className="field">
                <label>IMAP port</label>
                <input
                  className="input"
                  type="number"
                  value={custom.imapPort}
                  onChange={(e) => setCustom({ ...custom, imapPort: Number(e.target.value) })}
                  required
                />
              </div>
            </div>
            <label className="checkbox-row">
              <input
                type="checkbox"
                checked={custom.imapUseSsl}
                onChange={(e) => setCustom({ ...custom, imapUseSsl: e.target.checked })}
              />
              IMAP SSL kullan
            </label>
            <div className="grid two">
              <div className="field">
                <label>IMAP kullanıcı</label>
                <input
                  className="input"
                  value={custom.imapUsername}
                  onChange={(e) => setCustom({ ...custom, imapUsername: e.target.value })}
                  required
                />
              </div>
              <div className="field">
                <label>IMAP şifre</label>
                <input
                  className="input"
                  type="password"
                  value={custom.imapPassword}
                  onChange={(e) => setCustom({ ...custom, imapPassword: e.target.value })}
                  required
                />
              </div>
            </div>

            {status && <span className="hint">{status}</span>}
            <div className="row wrap">
              <button className="btn btn-primary" type="submit">
                Hesabı ekle
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default AccountsPage;
