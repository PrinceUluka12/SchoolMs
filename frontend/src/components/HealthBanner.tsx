import { useEffect, useState } from 'react';
import api from '../api/axiosInstance';

export default function HealthBanner() {
  const [apiDown, setApiDown] = useState(false);

  useEffect(() => {
    const check = async () => {
      try {
        await api.get('/health/live', { timeout: 5000 });
        setApiDown(false);
      } catch {
        setApiDown(true);
      }
    };

    check();
    const interval = setInterval(check, 30_000);
    return () => clearInterval(interval);
  }, []);

  if (!apiDown) return null;

  return (
    <div className="fixed top-0 left-0 right-0 z-50 bg-red-600 text-white text-sm
      font-medium text-center py-2 px-4 flex items-center justify-center gap-2">
      <span className="w-2 h-2 bg-white rounded-full animate-pulse shrink-0" />
      Service is temporarily unavailable. Retrying...
    </div>
  );
}