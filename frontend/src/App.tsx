import { Navigate, NavLink, Route, Routes, useLocation } from 'react-router-dom';
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
  const location = useLocation();

  return (
    <div className="topbar">
      <strong>AutoWarm</strong>
      {token && (
        <nav>
          <NavLink className={location.pathname === '/dashboard' ? 'active' : ''} to="/dashboard">
            Dashboard
          </NavLink>
          <NavLink className={location.pathname === '/accounts' ? 'active' : ''} to="/accounts">
            Hesaplar
          </NavLink>
          <NavLink className={location.pathname === '/profiles' ? 'active' : ''} to="/profiles">
            Profiller
          </NavLink>
          <NavLink className={location.pathname === '/logs' ? 'active' : ''} to="/logs">
            Loglar
          </NavLink>
        </nav>
      )}
      {token ? <button onClick={logout}>Çıkış</button> : null}
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
