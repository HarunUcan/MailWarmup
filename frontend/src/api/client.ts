import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5085',
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('autowarm.token');
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response && error.response.status === 401) {
      localStorage.removeItem('autowarm.token');
      localStorage.removeItem('autowarm.refresh');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  },
);

export default api;
