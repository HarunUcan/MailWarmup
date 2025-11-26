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
  const [editingId, setEditingId] = useState<string | null>(null);
  const [loadingProfiles, setLoadingProfiles] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const toast = useToast();

  const formatTime = (value: string) => {
    if (!value) return '00:00';
    const parts = value.split('T').pop() ?? value;
    return parts.slice(0, 5);
  };

  const mapProfileToForm = (p: WarmupProfileDto) => ({
    mailAccountId: p.mailAccountId,
    isEnabled: p.isEnabled,
    startDate: p.startDate.slice(0, 10),
    dailyMinEmails: p.dailyMinEmails,
    dailyMaxEmails: p.dailyMaxEmails,
    replyRate: p.replyRate,
    maxDurationDays: p.maxDurationDays,
    timeWindowStart: formatTime(p.timeWindowStart),
    timeWindowEnd: formatTime(p.timeWindowEnd),
    useRandomization: p.useRandomization,
  });

  const loadProfiles = async (accountId: string) => {
    try {
      setLoadingProfiles(true);
      const res = await api.get<WarmupProfileDto[]>(`/api/warmup-profiles/${accountId}`);
      setProfiles(res.data);
    } catch (error: any) {
      toast.push(error?.response?.data ?? 'Profiller yüklenemedi.', 'error');
    } finally {
      setLoadingProfiles(false);
    }
  };

  useEffect(() => {
    api
      .get<MailAccountDto[]>('/api/mail-accounts')
      .then((res) => {
        setAccounts(res.data);
        if (res.data.length) {
          const first = res.data[0].id;
          setSelected(first);
          setForm((prev) => ({ ...prev, mailAccountId: first }));
        }
      })
      .catch(() => toast.push('Hesaplar yüklenemedi.', 'error'));
  }, []);

  useEffect(() => {
    if (!selected) return;
    loadProfiles(selected);
    setEditingId(null);
    setForm((prev) => ({ ...prev, mailAccountId: selected }));
  }, [selected]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (form.dailyMinEmails > form.dailyMaxEmails) {
      toast.push('Günlük min, max değerinden büyük olamaz.', 'error');
      return;
    }
    if (form.replyRate < 0 || form.replyRate > 1) {
      toast.push('Yanıt oranı 0-1 aralığında olmalı.', 'error');
      return;
    }
    if (form.timeWindowStart >= form.timeWindowEnd) {
      toast.push('Zaman aralığı başlangıcı, bitişten önce olmalı.', 'error');
      return;
    }

    setSubmitting(true);
    try {
      if (editingId) {
        await api.put(`/api/warmup-profiles/${editingId}`, {
          isEnabled: form.isEnabled,
          dailyMinEmails: form.dailyMinEmails,
          dailyMaxEmails: form.dailyMaxEmails,
          replyRate: form.replyRate,
          maxDurationDays: form.maxDurationDays,
          timeWindowStart: form.timeWindowStart,
          timeWindowEnd: form.timeWindowEnd,
          useRandomization: form.useRandomization,
        });
        toast.push('Profil güncellendi.', 'success');
        setEditingId(null);
      } else {
        await api.post('/api/warmup-profiles', { ...form, mailAccountId: selected });
        toast.push('Profil oluşturuldu ve bugünün işleri üretildi.', 'success');
      }

      await loadProfiles(selected);
      setForm({ ...defaultProfile, mailAccountId: selected });
    } catch (error: any) {
      toast.push(error?.response?.data ?? 'İşlem başarısız.', 'error');
    } finally {
      setSubmitting(false);
    }
  };

  const handleEdit = (profile: WarmupProfileDto) => {
    setEditingId(profile.id);
    setForm(mapProfileToForm(profile));
  };

  const handleDelete = async (id: string) => {
    try {
      await api.delete(`/api/warmup-profiles/${id}`);
      toast.push('Profil silindi.', 'info');
      setEditingId(null);
      await loadProfiles(selected);
      setForm({ ...defaultProfile, mailAccountId: selected });
    } catch (error: any) {
      toast.push(error?.response?.data ?? 'Profil silinemedi.', 'error');
    }
  };

  const handleToggle = async (profile: WarmupProfileDto) => {
    try {
      await api.put(`/api/warmup-profiles/${profile.id}`, {
        isEnabled: !profile.isEnabled,
        dailyMinEmails: profile.dailyMinEmails,
        dailyMaxEmails: profile.dailyMaxEmails,
        replyRate: profile.replyRate,
        maxDurationDays: profile.maxDurationDays,
        timeWindowStart: formatTime(profile.timeWindowStart),
        timeWindowEnd: formatTime(profile.timeWindowEnd),
        useRandomization: profile.useRandomization,
      });
      toast.push(profile.isEnabled ? 'Profil pasif edildi.' : 'Profil aktif edildi.', 'info');
      await loadProfiles(selected);
    } catch (error: any) {
      toast.push(error?.response?.data ?? 'Durum güncellenemedi.', 'error');
    }
  };

  return (
    <div className="page">
      <div className="page-header">
        <div>
          <div className="page-title">Warmup profilleri</div>
          <div className="page-subtitle">Günlük hacim, yanıt oranı ve zaman aralığını ayarlayın</div>
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
            {loadingProfiles ? (
              <div className="hint">Yükleniyor...</div>
            ) : (
              <table className="table">
                <thead>
                  <tr>
                    <th>Durum</th>
                    <th>Günlük aralık</th>
                    <th>Yanıt</th>
                    <th>Gün</th>
                    <th>Aksiyon</th>
                  </tr>
                </thead>
                <tbody>
                  {profiles.map((p) => (
                    <tr key={p.id}>
                      <td>{p.isEnabled ? <span className="badge success">Aktif</span> : <span className="badge muted">Pasif</span>}</td>
                      <td>
                        {p.dailyMinEmails} - {p.dailyMaxEmails}
                      </td>
                      <td>%{Math.round(p.replyRate * 100)}</td>
                      <td>{p.currentDay}</td>
                      <td className="row wrap action-row" style={{ gap: 2 }}>
                        <button
                          className="icon-btn"
                          type="button"
                          onClick={() => handleEdit(p)}
                          title="Düzenle"
                          style={{ color: '#2563eb' }}
                        >
                          <i className="fa-solid fa-pen"></i>
                        </button>
                        <button
                          className="icon-btn"
                          type="button"
                          onClick={() => handleToggle(p)}
                          title={p.isEnabled ? 'Pasif et' : 'Aktif et'}
                          style={{ color: p.isEnabled ? '#ea580c' : '#16a34a' }}
                        >
                          <i className={`fa-solid ${p.isEnabled ? 'fa-pause' : 'fa-play'}`}></i>
                        </button>
                        <button
                          className="icon-btn"
                          type="button"
                          onClick={() => handleDelete(p.id)}
                          title="Sil"
                          style={{ color: '#dc2626' }}
                        >
                          <i className="fa-solid fa-trash"></i>
                        </button>
                      </td>
                    </tr>
                  ))}
                  {!profiles.length && (
                    <tr>
                      <td colSpan={5}>Bu hesap için kayıtlı profil yok.</td>
                    </tr>
                  )}
                </tbody>
              </table>
            )}
          </div>

          <div className="section">
            <div className="card-title row wrap" style={{ alignItems: 'center', gap: 8 }}>
              <span>{editingId ? 'Profili güncelle' : 'Yeni profil oluştur'}</span>
              {editingId && (
                <button
                  className="icon-btn"
                  type="button"
                  onClick={() => {
                    setEditingId(null);
                    setForm({ ...defaultProfile, mailAccountId: selected });
                  }}
                  title="Yeni profil oluştur"
                  style={{ color: '#16a34a' }}
                >
                  <i className="fa-solid fa-plus"></i>
                </button>
              )}
            </div>
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
                    disabled={!!editingId}
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
                <button className="btn btn-primary" type="submit" disabled={submitting}>
                  {submitting ? 'Kaydediliyor...' : editingId ? 'Güncelle' : 'Profili kaydet'}
                </button>
                {editingId && (
                  <button
                    className="btn btn-ghost"
                    type="button"
                    onClick={() => {
                      setEditingId(null);
                      setForm({ ...defaultProfile, mailAccountId: selected });
                    }}
                  >
                    İptal
                  </button>
                )}
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProfilesPage;
