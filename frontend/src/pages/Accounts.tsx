import { FormEvent, useEffect, useState } from 'react';
import api from '../api/client';
import { DnsCheckDto, MailAccountDto } from '../api/types';
import { useToast } from '../state/ToastContext';

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

const statusColor = (status: string) => {
  const normalized = status.toLowerCase();
  if (normalized.includes('pass') || normalized.includes('ok')) return '#16a34a';
  if (normalized.includes('warning') || normalized.includes('unknown')) return '#f59e0b';
  if (normalized.includes('fail') || normalized.includes('error')) return '#dc2626';
  return '#0ea5e9';
};

const AccountsPage = () => {
  const [accounts, setAccounts] = useState<MailAccountDto[]>([]);
  const [dnsChecks, setDnsChecks] = useState<DnsCheckDto[]>([]);
  const [custom, setCustom] = useState({ ...emptyCustom });
  const [status, setStatus] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [dnsLoading, setDnsLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [actionBusyId, setActionBusyId] = useState<string | null>(null);
  const toast = useToast();

  const loadAccounts = async () => {
    try {
      setLoading(true);
      const res = await api.get<MailAccountDto[]>('/api/mail-accounts');
      setAccounts(res.data);
    } catch (error: any) {
      toast.push(error?.response?.data ?? 'Hesaplar yüklenemedi.', 'error');
    } finally {
      setLoading(false);
    }
  };

  const loadDns = async () => {
    try {
      setDnsLoading(true);
      const res = await api.get<DnsCheckDto[]>('/api/mail-accounts/dns-checks');
      setDnsChecks(res.data);
    } catch (error: any) {
      toast.push(error?.response?.data ?? 'DNS sonuçları alınamadı.', 'error');
    } finally {
      setDnsLoading(false);
    }
  };

  useEffect(() => {
    loadAccounts();
    loadDns();
  }, []);

  const handleCustomSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setStatus(null);
    setSubmitting(true);
    try {
      await api.post('/api/mail-accounts/custom', custom);
      setCustom({ ...emptyCustom });
      setStatus('Hesap eklendi ve doğrulama başlatıldı');
      toast.push('Hesap eklendi ve doğrulama başlatıldı', 'success');
      await loadAccounts();
      await loadDns();
    } catch (error: any) {
      const message = error?.response?.data ?? 'Hesap eklenemedi, bilgileri kontrol edin.';
      setStatus(message);
      toast.push(message, 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const startGmail = async () => {
    try {
      const res = await api.post<{ authorizationUrl: string; state: string }>('/api/mail-accounts/gmail/start-auth');
      toast.push('Gmail bağlantısı başlatıldı, yönlendiriliyorsunuz...', 'info');
      window.location.href = res.data.authorizationUrl;
    } catch (error: any) {
      toast.push(error?.response?.data ?? 'Gmail bağlantısı başlatılamadı.', 'error');
    }
  };

  const handleToggleStatus = async (acc: MailAccountDto) => {
    const nextEnabled = acc.status === 2; // disabled -> enable, others -> disable
    setActionBusyId(acc.id);
    try {
      await api.patch(`/api/mail-accounts/${acc.id}/status`, { isEnabled: nextEnabled });
      toast.push(nextEnabled ? 'Hesap aktifleştirildi.' : 'Hesap pasif edildi.', 'info');
      await loadAccounts();
    } catch (error: any) {
      toast.push(error?.response?.data ?? 'Durum güncellenemedi.', 'error');
    } finally {
      setActionBusyId(null);
    }
  };

  const handleDelete = async (acc: MailAccountDto) => {
    if (!confirm(`${acc.emailAddress} hesabını silmek istiyor musun?`)) return;
    setActionBusyId(acc.id);
    try {
      await api.delete(`/api/mail-accounts/${acc.id}`);
      toast.push('Hesap silindi.', 'info');
      await loadAccounts();
      await loadDns();
    } catch (error: any) {
      toast.push(error?.response?.data ?? 'Hesap silinemedi.', 'error');
    } finally {
      setActionBusyId(null);
    }
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="page-title">Mail hesapları</div>
          <div className="page-subtitle">Gmail veya custom SMTP/IMAP hesaplarını bağla, durum ve DNS sağlığını izle</div>
        </div>
        <div className="row wrap">
          <button className="btn btn-primary" onClick={startGmail} disabled={loading}>
            Gmail bağla
          </button>
        </div>
      </div>

      <div className="grid two">
        <div className="card section">
          <div className="card-header">
            <div className="card-title">Bağlı hesaplar</div>
          </div>
          {loading ? (
            <div className="hint">Yükleniyor...</div>
          ) : (
            <table className="table">
              <thead>
                <tr>
                  <th>Email</th>
                  <th>Tip</th>
                  <th>Durum</th>
                  <th>Aksiyon</th>
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
                    <td className="row wrap action-row" style={{ gap: 4 }}>
                      <button
                        className="icon-btn"
                        title={acc.status === 2 ? 'Aktif et' : 'Pasif et'}
                        onClick={() => handleToggleStatus(acc)}
                        disabled={actionBusyId === acc.id}
                        style={{ color: acc.status === 2 ? '#16a34a' : '#ea580c' }}
                      >
                        <i className={`fa-solid ${acc.status === 2 ? 'fa-play' : 'fa-pause'}`}></i>
                      </button>
                      <button
                        className="icon-btn"
                        title="Sil"
                        onClick={() => handleDelete(acc)}
                        disabled={actionBusyId === acc.id}
                        style={{ color: '#dc2626' }}
                      >
                        <i className="fa-solid fa-trash"></i>
                      </button>
                    </td>
                  </tr>
                ))}
                {!accounts.length && (
                  <tr>
                    <td colSpan={4}>Hesap bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          )}
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
                  min={1}
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
                  min={1}
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
              <button className="btn btn-primary" type="submit" disabled={submitting}>
                {submitting ? 'Kaydediliyor...' : 'Hesabı ekle'}
              </button>
            </div>
          </form>
        </div>
      </div>

      <div className="card section" style={{ marginTop: 16 }}>
        <div className="card-header">
          <div className="card-title">DNS / Authentication Check</div>
          <div className="page-subtitle" style={{ margin: 0 }}>SPF, DKIM, DMARC, MX ve Reverse DNS durumu</div>
        </div>
        {dnsLoading ? (
          <div className="hint">DNS sonuçları yükleniyor...</div>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Hesap</th>
                <th>SPF</th>
                <th>DKIM</th>
                <th>DMARC</th>
                <th>MX</th>
                <th>Reverse DNS</th>
              </tr>
            </thead>
            <tbody>
              {dnsChecks.map((c) => (
                <tr key={c.mailAccountId}>
                  <td>{c.emailAddress}</td>
                  <td>
                    <span className="pill" style={{ background: `${statusColor(c.spf)}22`, color: statusColor(c.spf) }}>
                      {c.spf}
                    </span>
                  </td>
                  <td>
                    <span className="pill" style={{ background: `${statusColor(c.dkim)}22`, color: statusColor(c.dkim) }}>
                      {c.dkim}
                    </span>
                  </td>
                  <td>
                    <span className="pill" style={{ background: `${statusColor(c.dmarc)}22`, color: statusColor(c.dmarc) }}>
                      {c.dmarc}
                    </span>
                  </td>
                  <td>
                    <span className="pill" style={{ background: `${statusColor(c.mx)}22`, color: statusColor(c.mx) }}>
                      {c.mx}
                    </span>
                  </td>
                  <td>
                    <span className="pill" style={{ background: `${statusColor(c.reverseDns)}22`, color: statusColor(c.reverseDns) }}>
                      {c.reverseDns}
                    </span>
                  </td>
                </tr>
              ))}
              {!dnsChecks.length && (
                <tr>
                  <td colSpan={6}>DNS sonucu bulunamadı.</td>
                </tr>
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

export default AccountsPage;
