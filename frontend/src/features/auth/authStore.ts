import { create } from 'zustand';

interface AuthState {
  userId: string | null;
  email: string | null;
  role: string | null;
  isAuthenticated: boolean;
  mfaPending: boolean;
  mfaToken: string | null;
  setAuth: (userId: string, email: string, role: string, accessToken: string, refreshToken: string) => void;
  setMfaPending: (mfaToken: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  userId: localStorage.getItem('userId'),
  email: localStorage.getItem('email'),
  role: localStorage.getItem('role'),
  isAuthenticated: !!localStorage.getItem('accessToken'),
  mfaPending: false,
  mfaToken: null,

  setAuth: (userId, email, role, accessToken, refreshToken) => {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    localStorage.setItem('userId', userId);
    localStorage.setItem('email', email);
    localStorage.setItem('role', role);
    set({ userId, email, role, isAuthenticated: true, mfaPending: false, mfaToken: null });
  },

  setMfaPending: (mfaToken) => set({ mfaPending: true, mfaToken }),

  logout: () => {
    localStorage.clear();
    set({ userId: null, email: null, role: null, isAuthenticated: false, mfaPending: false, mfaToken: null });
  },
}));