import api from '../../api/axiosInstance';

export interface DashboardStats {
  totalStudents: number;
  activeStudents: number;
  totalStaff: number;
  activeStaff: number;
  totalClasses: number;
  currentAcademicYear: string | null;
  currentTerm: string | null;
  studentsByGender: { gender: string; count: number }[];
  studentsByClass: { className: string; count: number }[];
  staffByRole: { role: string; count: number }[];
  recentStudents: {
    id: string;
    fullName: string;
    studentNumber: string;
    className: string;
    createdAt: string;
  }[];
}

export const dashboardApi = {
  getStats: () =>
    api.get<DashboardStats>('/dashboard/stats').then(r => r.data),
};