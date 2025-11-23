import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import api from '../api/client';
import { AuthResponse } from '../api/types';

type AuthContextValue = {
  token: string | null;
  refreshToken: string | null;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

const AUTH_TOKEN_KEY = 'autowarm.token';
const AUTH_REFRESH_KEY = 'autowarm.refresh';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [token, setToken] = useState<string | null>(localStorage.getItem(AUTH_TOKEN_KEY));
  const [refreshToken, setRefreshToken] = useState<string | null>(localStorage.getItem(AUTH_REFRESH_KEY));

  useEffect(() => {
    if (token) {
      localStorage.setItem(AUTH_TOKEN_KEY, token);
    }
  }, [token]);

  useEffect(() => {
    if (refreshToken) {
      localStorage.setItem(AUTH_REFRESH_KEY, refreshToken);
    }
  }, [refreshToken]);

  const handleAuthResponse = (res: AuthResponse) => {
    setToken(res.token);
    setRefreshToken(res.refreshToken);
  };

  const login = async (email: string, password: string) => {
    const response = await api.post<AuthResponse>('/api/auth/login', { email, password });
    handleAuthResponse(response.data);
  };

  const register = async (email: string, password: string) => {
    const response = await api.post<AuthResponse>('/api/auth/register', { email, password });
    handleAuthResponse(response.data);
  };

  const logout = () => {
    setToken(null);
    setRefreshToken(null);
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem(AUTH_REFRESH_KEY);
  };

  const value = useMemo(
    () => ({ token, refreshToken, login, register, logout }),
    [token, refreshToken],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider');
  return ctx;
};
