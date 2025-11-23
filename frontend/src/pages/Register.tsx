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
    <div className="container">
      <div className="card" style={{ maxWidth: 420, margin: '40px auto' }}>
        <h2>Kayıt ol</h2>
        <form onSubmit={handleSubmit}>
          <input placeholder="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          <input placeholder="Şifre" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
          {error && <span style={{ color: 'crimson' }}>{error}</span>}
          <button type="submit">Hesap oluştur</button>
        </form>
        <p>
          Zaten hesabın var mı? <Link to="/login">Giriş yap</Link>
        </p>
      </div>
    </div>
  );
};

export default RegisterPage;
