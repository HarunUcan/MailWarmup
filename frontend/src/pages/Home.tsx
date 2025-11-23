import { NavLink } from 'react-router-dom';

const HomePage = () => {
  return (
    <div className="home">
      <section className="hero">
        <div className="chip">AutoWarm</div>
        <h1>Giriş kutularınızı sıcak tutan akıllı warmup SaaS'ı</h1>
        <p>SPF/DKIM/DMARC kontrolü, otomatik warmup, itibar skoru ve loglarla tek panelde.</p>
        <div className="row wrap" style={{ gap: 12, marginTop: 12 }}>
          <NavLink className="btn btn-primary" to="/register">
            Ücretsiz başla
          </NavLink>
          <NavLink className="btn btn-ghost" to="/login">
            Giriş yap
          </NavLink>
        </div>
      </section>

      <section id="features" className="card section">
        <div className="card-header">
          <div className="card-title">Neden AutoWarm?</div>
          <div className="page-subtitle" style={{ margin: 0 }}>
            Günlük warmup, reply otomasyonu, inbox/spam takibi ve DNS sağlık kontrolü.
          </div>
        </div>
        <div className="grid three">
          <div className="feature">
            <div className="pill">Warmup</div>
            <h3>Gerçekçi gönderimler</h3>
            <p>Ağ içinde karşılıklı mail ve reply dağıtımı, rastgele zamanlama ile.</p>
          </div>
          <div className="feature">
            <div className="pill">Itibar</div>
            <h3>Health score</h3>
            <p>Spam/bounce ve reply sinyallerine göre 100 üzerinden skor ve trend.</p>
          </div>
          <div className="feature">
            <div className="pill">Gözetim</div>
            <h3>DNS & Log</h3>
            <p>SPF, DKIM, DMARC, MX ve Reverse DNS kontrolleri; ayrıntılı loglar.</p>
          </div>
        </div>
      </section>

      <section id="pricing" className="card section">
        <div className="card-header">
          <div className="card-title">Paketler</div>
          <div className="page-subtitle" style={{ margin: 0 }}>Esnek planlar; tümü DNS check ve loglama ile.</div>
        </div>
        <div className="grid three">
          <div className="pricing">
            <h3>Starter</h3>
            <div className="price">$19/ay</div>
            <ul>
              <li>1 hesap</li>
              <li>Günlük 30 email</li>
              <li>DNS kontrol</li>
            </ul>
            <NavLink className="btn btn-primary" to="/register">
              Başla
            </NavLink>
          </div>
          <div className="pricing highlighted">
            <h3>Growth</h3>
            <div className="price">$49/ay</div>
            <ul>
              <li>5 hesap</li>
              <li>Günlük 150 email</li>
              <li>Reply otomasyon</li>
              <li>Health score trend</li>
            </ul>
            <NavLink className="btn btn-primary" to="/register">
              Seç
            </NavLink>
          </div>
          <div className="pricing">
            <h3>Scale</h3>
            <div className="price">$99/ay</div>
            <ul>
              <li>15 hesap</li>
              <li>Günlük 400 email</li>
              <li>API erişimi</li>
              <li>Öncelikli destek</li>
            </ul>
            <NavLink className="btn btn-primary" to="/register">
              İletişime geç
            </NavLink>
          </div>
        </div>
      </section>
    </div>
  );
};

export default HomePage;
