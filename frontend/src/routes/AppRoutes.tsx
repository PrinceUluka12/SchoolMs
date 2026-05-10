import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import LoginPage from '../features/auth/LoginPage';
import MfaPage from '../features/auth/MfaPage';
import StudentsPage from '../features/students/StudentsPage';
import StaffPage from '../features/staff/StaffPage';
import DashboardPage from '../features/dashboard/DashboardPage';
import AcademicYearsPage from '../features/academic/AcademicYearsPage';
import DepartmentsPage from '../features/academic/DepartmentsPage';
import ClassesPage from '../features/academic/ClassesPage';
import SubjectsPage from '../features/academic/SubjectsPage';
import AttendancePage from '../features/attendance/AttendancePage';
import TimetablePage from '../features/timetable/TimetablePage';
import GradesPage from '../features/grades/GradesPage';
import FinancePage from '../features/finance/FinancePage';
import CommunicationPage from '../features/communication/CommunicationPage';
import LibraryPage from '../features/library/LibraryPage';
import ExamsPage from '../features/exams/ExamsPage';
import AppLayout from '../layouts/AppLayout';
import { useAuthStore } from '../features/auth/authStore';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuthStore();
  return isAuthenticated
    ? <AppLayout>{children}</AppLayout>
    : <Navigate to="/login" replace />;
}

export default function AppRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login"          element={<LoginPage />} />
        <Route path="/mfa"            element={<MfaPage />} />
        <Route path="/dashboard"      element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />
        <Route path="/students"       element={<ProtectedRoute><StudentsPage /></ProtectedRoute>} />
        <Route path="/staff"          element={<ProtectedRoute><StaffPage /></ProtectedRoute>} />
        <Route path="/departments"    element={<ProtectedRoute><DepartmentsPage /></ProtectedRoute>} />
        <Route path="/academic-years" element={<ProtectedRoute><AcademicYearsPage /></ProtectedRoute>} />
        <Route path="/classes"        element={<ProtectedRoute><ClassesPage /></ProtectedRoute>} />
        <Route path="/subjects"       element={<ProtectedRoute><SubjectsPage /></ProtectedRoute>} />
        <Route path="/attendance"     element={<ProtectedRoute><AttendancePage /></ProtectedRoute>} />
        <Route path="/timetable"      element={<ProtectedRoute><TimetablePage /></ProtectedRoute>} />
        <Route path="/grades"         element={<ProtectedRoute><GradesPage /></ProtectedRoute>} />
        <Route path="/finance"        element={<ProtectedRoute><FinancePage /></ProtectedRoute>} />
        <Route path="/communications" element={<ProtectedRoute><CommunicationPage /></ProtectedRoute>} />
        <Route path="/library"        element={<ProtectedRoute><LibraryPage /></ProtectedRoute>} />
        <Route path="/transport"      element={<ProtectedRoute><div className="p-8"><h1 className="text-2xl font-bold text-gray-900">Transport</h1><p className="text-gray-500 mt-2">Transport management UI — use the API directly or extend this page.</p></div></ProtectedRoute>} />
        <Route path="/hostel"         element={<ProtectedRoute><div className="p-8"><h1 className="text-2xl font-bold text-gray-900">Hostel</h1><p className="text-gray-500 mt-2">Hostel management UI — use the API directly or extend this page.</p></div></ProtectedRoute>} />
        <Route path="/exams"          element={<ProtectedRoute><ExamsPage /></ProtectedRoute>} />
        <Route path="/"               element={<Navigate to="/dashboard" replace />} />
        <Route path="*"               element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}