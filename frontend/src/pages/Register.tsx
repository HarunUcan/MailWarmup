import { FormEvent, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../state/AuthContext';

const RegisterPage = () => {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await register(email, password);
      navigate('/dashboard');
    } catch (err) {
      setError('Kayıt başarısız. Tekrar deneyin.');
    }
  };

  return (
    <div className="auth-layout">
      <div className="auth-hero">
        <div className="chip">AutoWarm</div>
        <h1>Yeni hesap oluştur</h1>
        <p>Warmup profilleri, loglar ve hesap yönetimi tek bir panelde. 2 dakika içinde kullanmaya başlayın.</p>
      </div>
      <div className="auth-panel">
        <h2>Kayıt ol</h2>
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
            Hesap oluştur
          </button>
        </form>
        <p>
          Zaten hesabınız var mı? <Link to="/login">Giriş yap</Link>
        </p>
      </div>
    </div>
  );
};

export default RegisterPage;
