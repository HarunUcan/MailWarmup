import { FormEvent, useEffect, useState } from 'react';
import api from '../api/client';
import { MailAccountDto, WarmupProfileDto } from '../api/types';

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

  useEffect(() => {
    api.get<MailAccountDto[]>('/api/mail-accounts').then((res) => {
      setAccounts(res.data);
      if (res.data.length) {
        setSelected(res.data[0].id);
        setForm((prev) => ({ ...prev, mailAccountId: res.data[0].id }));
      }
    });
  }, []);

  useEffect(() => {
    if (!selected) return;
    api.get<WarmupProfileDto[]>(`/api/warmup-profiles/${selected}`).then((res) => setProfiles(res.data));
  }, [selected]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    await api.post('/api/warmup-profiles', { ...form, mailAccountId: selected });
    const res = await api.get<WarmupProfileDto[]>(`/api/warmup-profiles/${selected}`);
    setProfiles(res.data);
  };

  return (
    <div className="container">
      <h2>Warmup Profilleri</h2>
      <div className="card">
        <label>
          Mail hesabi
          <select value={selected} onChange={(e) => setSelected(e.target.value)}>
            {accounts.map((a) => (
              <option key={a.id} value={a.id}>
                {a.emailAddress}
              </option>
            ))}
          </select>
        </label>
        <div className="grid columns-2">
          <div>
            <h4>Mevcut profiller</h4>
            <table>
              <thead>
                <tr>
                  <th>Durum</th>
                  <th>Aralik</th>
                  <th>Reply</th>
                </tr>
              </thead>
              <tbody>
                {profiles.map((p) => (
                  <tr key={p.id}>
                    <td>{p.isEnabled ? 'Aktif' : 'Kapali'}</td>
                    <td>
                      {p.dailyMinEmails}-{p.dailyMaxEmails}
                    </td>
                    <td>%{Math.round(p.replyRate * 100)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div>
            <h4>Yeni profil</h4>
            <form onSubmit={handleSubmit}>
              <div className="grid columns-2">
                <label>
                  Baslangic tarihi
                  <input
                    type="date"
                    value={form.startDate}
                    onChange={(e) => setForm({ ...form, startDate: e.target.value })}
                    required
                  />
                </label>
                <input
                  type="number"
                  placeholder="Gunluk min"
                  value={form.dailyMinEmails}
                  onChange={(e) => setForm({ ...form, dailyMinEmails: Number(e.target.value) })}
                  required
                />
                <input
                  type="number"
                  placeholder="Gunluk max"
                  value={form.dailyMaxEmails}
                  onChange={(e) => setForm({ ...form, dailyMaxEmails: Number(e.target.value) })}
                  required
                />
              </div>
              <input
                type="number"
                step="0.05"
                placeholder="Reply rate"
                value={form.replyRate}
                onChange={(e) => setForm({ ...form, replyRate: Number(e.target.value) })}
                required
              />
              <input
                type="number"
                placeholder="Sure (gun)"
                value={form.maxDurationDays}
                onChange={(e) => setForm({ ...form, maxDurationDays: Number(e.target.value) })}
                required
              />
              <div className="grid columns-2">
                <input
                  type="time"
                  value={form.timeWindowStart}
                  onChange={(e) => setForm({ ...form, timeWindowStart: e.target.value })}
                  required
                />
                <input
                  type="time"
                  value={form.timeWindowEnd}
                  onChange={(e) => setForm({ ...form, timeWindowEnd: e.target.value })}
                  required
                />
              </div>
              <label>
                <input type="checkbox" checked={form.isEnabled} onChange={(e) => setForm({ ...form, isEnabled: e.target.checked })} /> Aktif
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={form.useRandomization}
                  onChange={(e) => setForm({ ...form, useRandomization: e.target.checked })}
                />{' '}
                Rastgele gonderim
              </label>
              <button type="submit">Kaydet</button>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProfilesPage;
