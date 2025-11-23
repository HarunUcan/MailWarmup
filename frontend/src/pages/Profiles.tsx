import { FormEvent, useEffect, useState } from 'react';
import api from '../api/client';
import { MailAccountDto, WarmupProfileDto } from '../api/types';
import { useToast } from '../state/ToastContext';

const getTodayLocal = () => {
  const d = new Date();
  d.setMinutes(d.getMinutes() - d.getTimezoneOffset());
  return d.toISOString().slice(0, 10);
};

const defaultProfile = {
  mailAccountId: '',
  isEnabled: true,
  startDate: getTodayLocal(),
  dailyMinEmails: 5,
  dailyMaxEmails: 15,
  replyRate: 0.3,
  maxDurationDays: 30,
  timeWindowStart: '08:00',
  timeWindowEnd: '18:00',
  useRandomization: true,
};

const ProfilesPage = () => {
  const [accounts, setAccounts] = useState<MailAccountDto[]>([]);
  const [selected, setSelected] = useState<string>('');
  const [profiles, setProfiles] = useState<WarmupProfileDto[]>([]);
  const [form, setForm] = useState({ ...defaultProfile });
  const toast = useToast();

  useEffect(() => {
    api.get<MailAccountDto[]>('/api/mail-accounts').then((res) => {
      setAccounts(res.data);
      if (res.data.length) {
        const first = res.data[0].id;
        setSelected(first);
        setForm((prev) => ({ ...prev, mailAccountId: first }));
      }
    });
  }, []);

  useEffect(() => {
    if (!selected) return;
    api.get<WarmupProfileDto[]>(`/api/warmup-profiles/${selected}`).then((res) => setProfiles(res.data));
    setForm((prev) => ({ ...prev, mailAccountId: selected }));
  }, [selected]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    await api.post('/api/warmup-profiles', { ...form, mailAccountId: selected });
    const res = await api.get<WarmupProfileDto[]>(`/api/warmup-profiles/${selected}`);
    setProfiles(res.data);
    toast.push('Profil oluşturuldu ve bugünün işleri üretildi.', 'success');
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="page-title">Warmup profilleri</div>
          <div className="page-subtitle">Günlük hacim, yanıt oranı ve zaman aralığını net şekilde ayarlayın</div>
        </div>
      </div>

      <div className="card section">
        <div className="grid two">
          <div className="field">
            <label>Mail hesabı</label>
            <select className="select" value={selected} onChange={(e) => setSelected(e.target.value)}>
              {accounts.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.emailAddress}
                </option>
              ))}
            </select>
            <div className="hint">Profil oluşturacağınız hesabı seçin.</div>
          </div>
        </div>

        <div className="grid two">
          <div className="section">
            <div className="card-title">Mevcut profiller</div>
            <table className="table">
              <thead>
                <tr>
                  <th>Durum</th>
                  <th>Günlük aralık</th>
                  <th>Yanıt</th>
                  <th>Gün</th>
                </tr>
              </thead>
              <tbody>
                {profiles.map((p) => (
                  <tr key={p.id}>
                    <td>{p.isEnabled ? <span className="badge success">Aktif</span> : <span className="badge muted">Kapali</span>}</td>
                    <td>
                      {p.dailyMinEmails} - {p.dailyMaxEmails}
                    </td>
                    <td>%{Math.round(p.replyRate * 100)}</td>
                    <td>{p.currentDay}</td>
                  </tr>
                ))}
                {!profiles.length && (
                  <tr>
                    <td colSpan={4}>Bu hesap için kayıtlı profil yok.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="section">
            <div className="card-title">Yeni profil oluştur</div>
            <form className="stack" onSubmit={handleSubmit}>
              <div className="grid two">
                <div className="field">
                  <label>Başlangıç tarihi</label>
                  <input
                    className="input"
                    type="date"
                    value={form.startDate}
                    onChange={(e) => setForm({ ...form, startDate: e.target.value })}
                    required
                  />
                  <div className="hint">Gönderimlerin başlayacağı gün (yerel saat).</div>
                </div>
                <div className="field">
                  <label>Süre (gün)</label>
                  <input
                    className="input"
                    type="number"
                    value={form.maxDurationDays}
                    onChange={(e) => setForm({ ...form, maxDurationDays: Number(e.target.value) })}
                    required
                  />
                  <div className="hint">0 limitsiz; aksi halde toplam gün sayısı.</div>
                </div>
              </div>

              <div className="grid two">
                <div className="field">
                  <label>Günlük min e-posta</label>
                  <input
                    className="input"
                    type="number"
                    value={form.dailyMinEmails}
                    onChange={(e) => setForm({ ...form, dailyMinEmails: Number(e.target.value) })}
                    required
                  />
                  <div className="hint">Günlük minimum gönderim.</div>
                </div>
                <div className="field">
                  <label>Günlük max e-posta</label>
                  <input
                    className="input"
                    type="number"
                    value={form.dailyMaxEmails}
                    onChange={(e) => setForm({ ...form, dailyMaxEmails: Number(e.target.value) })}
                    required
                  />
                  <div className="hint">Günlük tavan gönderim.</div>
                </div>
              </div>

              <div className="grid two">
                <div className="field">
                  <label>Yanıt oranı</label>
                  <input
                    className="input"
                    type="number"
                    step="0.05"
                    value={form.replyRate}
                    onChange={(e) => setForm({ ...form, replyRate: Number(e.target.value) })}
                    required
                  />
                  <div className="hint">0.3 = %30 cevap oranı.</div>
                </div>
                <div className="field">
                  <label>Günlük zaman aralığı</label>
                  <div className="grid two">
                    <input
                      className="input"
                      type="time"
                      value={form.timeWindowStart}
                      onChange={(e) => setForm({ ...form, timeWindowStart: e.target.value })}
                      required
                    />
                    <input
                      className="input"
                      type="time"
                      value={form.timeWindowEnd}
                      onChange={(e) => setForm({ ...form, timeWindowEnd: e.target.value })}
                      required
                    />
                  </div>
                  <div className="hint">Gönderimlerin yayılacağı yerel saat aralığı.</div>
                </div>
              </div>

              <label className="checkbox-row">
                <input
                  type="checkbox"
                  checked={form.isEnabled}
                  onChange={(e) => setForm({ ...form, isEnabled: e.target.checked })}
                />
                Profil aktif olsun
              </label>
              <label className="checkbox-row">
                <input
                  type="checkbox"
                  checked={form.useRandomization}
                  onChange={(e) => setForm({ ...form, useRandomization: e.target.checked })}
                />
                Günlük aralık içinde rastgele dağıtım
              </label>

              <div className="row wrap">
                <button className="btn btn-primary" type="submit">
                  Profili kaydet
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProfilesPage;
