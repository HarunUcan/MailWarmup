import { Navigate, NavLink, Route, Routes } from 'react-router-dom';
import LoginPage from './pages/Login';
import RegisterPage from './pages/Register';
import DashboardPage from './pages/Dashboard';
import AccountsPage from './pages/Accounts';
import ProfilesPage from './pages/Profiles';
import LogsPage from './pages/Logs';
import { ProtectedRoute } from './components/ProtectedRoute';
import { useAuth } from './state/AuthContext';

const NavBar = () => {
  const { token, logout } = useAuth();

  return (
    <div className="topbar">
      <div className="brand">
        <span className="dot" />
        <span>AutoWarm</span>
        <span className="pill">SaaS</span>
      </div>
      {token && (
        <nav className="nav-links">
          <NavLink className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`} to="/dashboard">
            Dashboard
          </NavLink>
          <NavLink className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`} to="/accounts">
            Hesaplar
          </NavLink>
          <NavLink className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`} to="/profiles">
            Profiller
          </NavLink>
          <NavLink className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`} to="/logs">
            Loglar
          </NavLink>
        </nav>
      )}
      {token ? (
        <div className="topbar-actions">
          <button className="btn btn-ghost" onClick={logout}>
            Çıkış yap
          </button>
        </div>
      ) : null}
    </div>
  );
};

function App() {
  return (
    <div className="app-shell">
      <NavBar />
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/accounts"
          element={
            <ProtectedRoute>
              <AccountsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/profiles"
          element={
            <ProtectedRoute>
              <ProfilesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/logs"
          element={
            <ProtectedRoute>
              <LogsPage />
            </ProtectedRoute>
          }
        />
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </div>
  );
}

export default App;
