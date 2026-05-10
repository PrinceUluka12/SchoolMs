import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../../api/axiosInstance';
import { useAuthStore } from './authStore';

export default function MfaPage() {
  const navigate = useNavigate();
  const { mfaToken, setAuth } = useAuthStore();
  const [code, setCode] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const { data } = await api.post('/auth/mfa/verify', { mfaToken, code });
      setAuth(data.userId, data.email, data.role, data.accessToken, data.refreshToken);
      navigate('/dashboard');
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Invalid code. Try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="bg-white rounded-2xl shadow-lg p-10 w-full max-w-sm">
        <h2 className="text-2xl font-bold text-blue-900 mb-2">Two-Factor Auth</h2>
        <p className="text-gray-500 text-sm mb-8">Enter the 6-digit code from your authenticator app.</p>
        {error && <div className="bg-red-50 text-red-700 rounded-lg px-4 py-3 mb-5 text-sm">{error}</div>}
        <form onSubmit={handleSubmit} className="space-y-5">
          <input
            type="text" inputMode="numeric" maxLength={6} required
            value={code} onChange={(e) => setCode(e.target.value)}
            className="w-full text-center text-2xl tracking-widest border border-gray-300 rounded-lg px-4 py-3 focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="000000"
          />
          <button type="submit" disabled={loading}
            className="w-full bg-blue-700 hover:bg-blue-800 text-white font-semibold py-2.5 rounded-lg transition disabled:opacity-50">
            {loading ? 'Verifying...' : 'Verify'}
          </button>
        </form>
      </div>
    </div>
  );
}