import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../features/auth/authStore';
import api from '../api/axiosInstance';
import HealthBanner from '../components/HealthBanner';

const NAV_ITEMS = [
  { path: '/dashboard',      label: 'Dashboard',      icon: '⊞' },
  { path: '/students',       label: 'Students',       icon: '🎓' },
  { path: '/staff',          label: 'Staff',          icon: '👤' },
  { path: '/departments',    label: 'Departments',    icon: '🏢' },
  { path: '/academic-years', label: 'Academic Years', icon: '📅' },
  { path: '/classes',        label: 'Classes',        icon: '🏫' },
  { path: '/subjects',       label: 'Subjects',       icon: '📚' },
  { path: '/attendance',     label: 'Attendance',     icon: '✅' },
  { path: '/timetable',      label: 'Timetable',      icon: '🗓' },
  { path: '/grades',         label: 'Grades',         icon: '📝' },
  { path: '/finance',        label: 'Finance',        icon: '💰' },
  { path: '/communications', label: 'Communications', icon: '✉️' },
  { path: '/library',        label: 'Library',        icon: '📖' },
  { path: '/transport',      label: 'Transport',      icon: '🚌' },
  { path: '/hostel',         label: 'Hostel',         icon: '🏠' },
  { path: '/exams',          label: 'Exams',          icon: '📋' },
];

export default function AppLayout({ children }: { children: React.ReactNode }) {
  const location = useLocation();
  const navigate = useNavigate();
  const { email, role, logout } = useAuthStore();
  const [collapsed, setCollapsed] = useState(false);

  const handleLogout = async () => {
    try { await api.post('/auth/logout'); } catch {}
    logout();
    navigate('/login');
  };

  return (
    // Inside AppLayout return:
<div className="flex h-screen bg-gray-100 overflow-hidden">
  <HealthBanner />
    <div className="flex h-screen bg-gray-100 overflow-hidden">
      {/* Sidebar */}
      <aside className={`${collapsed ? 'w-16' : 'w-60'} bg-[#1F3864] flex flex-col transition-all duration-200 shrink-0`}>
        {/* Logo */}
        <div className="flex items-center justify-between px-4 py-5 border-b border-white/10">
          {!collapsed && (
            <span className="text-white font-bold text-lg tracking-tight">SchoolMS</span>
          )}
          <button
            onClick={() => setCollapsed(c => !c)}
            className="text-white/60 hover:text-white transition text-xl leading-none ml-auto">
            {collapsed ? '→' : '←'}
          </button>
        </div>

        {/* Nav */}
        <nav className="flex-1 py-4 overflow-y-auto">
          {NAV_ITEMS.map(item => {
            const active = location.pathname === item.path;
            return (
              <Link key={item.path} to={item.path}
                className={`flex items-center gap-3 px-4 py-2.5 mx-2 rounded-lg mb-0.5 transition text-sm font-medium
                  ${active
                    ? 'bg-white/20 text-white'
                    : 'text-white/60 hover:bg-white/10 hover:text-white'}`}>
                <span className="text-base shrink-0">{item.icon}</span>
                {!collapsed && <span>{item.label}</span>}
              </Link>
            );
          })}
        </nav>

        {/* User */}
        <div className="border-t border-white/10 p-4">
          {!collapsed && (
            <div className="mb-3">
              <p className="text-white text-sm font-medium truncate">{email}</p>
              <p className="text-white/50 text-xs">{role}</p>
            </div>
          )}
          <button onClick={handleLogout}
            className="flex items-center gap-2 text-white/60 hover:text-white text-sm transition w-full">
            <span>⎋</span>
            {!collapsed && <span>Logout</span>}
          </button>
        </div>
      </aside>

      {/* Main */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Top Bar */}
        <header className="bg-white border-b border-gray-200 px-6 py-3 flex items-center justify-between shrink-0">
          <h2 className="text-sm font-semibold text-gray-600 capitalize">
            {location.pathname.replace('/', '').replace('-', ' ') || 'Dashboard'}
          </h2>
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-full bg-blue-700 flex items-center justify-center text-white text-xs font-bold">
              {email?.[0]?.toUpperCase() ?? 'A'}
            </div>
          </div>
        </header>

        {/* Page Content */}
        <main className="flex-1 overflow-y-auto">
          {children}
        </main>
      </div>
    </div>
    </div>
  );
}