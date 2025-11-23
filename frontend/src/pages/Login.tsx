import { FormEvent, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../state/AuthContext';

const LoginPage = () => {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await login(email, password);
      navigate('/dashboard');
    } catch (err) {
      setError('Giriş başarısız. Bilgileri kontrol edin.');
    }
  };

  return (
    <div className="auth-layout">
      <div className="auth-hero">
        <div className="chip">AutoWarm</div>
        <h1>Sıcak posta kutuları için modern warmup</h1>
        <p>Hesap bağlayın, profil tanımlayın ve günlük gönderimleri otomatik dağıtın. Basit, hızlı, SaaS odaklı.</p>
      </div>
      <div className="auth-panel">
        <h2>Giriş yap</h2>
        <form className="stack" onSubmit={handleSubmit}>
          <div className="field">
            <label>Email</label>
            <input className="input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </div>
          <div className="field">
            <label>Şifre</label>
            <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
          </div>
          {error && <span style={{ color: 'crimson' }}>{error}</span>}
          <button className="btn btn-primary" type="submit">
            Giriş
          </button>
        </form>
        <p>
          Hesabın yok mu? <Link to="/register">Kayıt ol</Link>
        </p>
      </div>
    </div>
  );
};

export default LoginPage;
