import api from '../../api/axiosInstance';

export interface AttendanceStudentRow {
  studentId: string;
  studentName: string;
  studentNumber: string;
  photoUrl?: string;
  status?: string;
  notes?: string;
  attendanceId?: string;
}

export interface AttendanceRegister {
  classId: string;
  className: string;
  date: string;
  period?: number;
  isMarked: boolean;
  students: AttendanceStudentRow[];
}

export interface AttendanceSummary {
  studentId: string;
  studentName: string;
  studentNumber: string;
  totalDays: number;
  present: number;
  absent: number;
  late: number;
  excused: number;
  attendanceRate: number;
}

export interface ClassAttendanceReport {
  className: string;
  fromDate: string;
  toDate: string;
  totalStudents: number;
  averageAttendanceRate: number;
  students: AttendanceSummary[];
}

export const attendanceApi = {
  getRegister: (classId: string, date: string, period?: number) =>
    api.get<AttendanceRegister>('/attendance/register', {
      params: { classId, date, period }
    }).then(r => r.data),

  markBulk: (data: {
    classId: string;
    date: string;
    period?: number;
    entries: { studentId: string; status: string; notes?: string }[];
  }) => api.post('/attendance/mark', data).then(r => r.data),

  getStudentSummary: (studentId: string, termId?: string) =>
    api.get<AttendanceSummary[]>(`/attendance/student/${studentId}/summary`, {
      params: { termId }
    }).then(r => r.data),

  getClassReport: (classId: string, from: string, to: string) =>
    api.get<ClassAttendanceReport>(`/attendance/class/${classId}/report`, {
      params: { from, to }
    }).then(r => r.data),
};