import api from '../../api/axiosInstance';

export interface Exam {
  id: string; name: string; type: string; status: string; description?: string;
  termId: string; termName: string; academicYearId: string; academicYearName: string;
  scheduleCount: number; schedules: ExamSchedule[]; createdAt: string;
}

export interface ExamSchedule {
  id: string; examId: string; examName: string; subjectId: string; subjectName: string;
  subjectCode: string; classId: string; className: string; examDate: string;
  startTime: string; endTime: string; venue?: string; totalSeats?: number;
  seatedStudents: number; createdAt: string;
}

export interface SeatingArrangement {
  id: string; studentId: string; studentName: string; studentNumber: string;
  seatNumber: string; subjectName: string; className: string; examDate: string; venue: string;
}

export const examsApi = {
  getByTerm: (termId: string) =>
    api.get<Exam[]>('/exams', { params: { termId } }).then(r => r.data),
  createExam: (data: unknown) => api.post<Exam>('/exams', data).then(r => r.data),
  updateExam: (id: string, data: unknown) => api.put<Exam>(`/exams/${id}`, data).then(r => r.data),
  deleteExam: (id: string) => api.delete(`/exams/${id}`),
  addSchedule: (data: unknown) => api.post<ExamSchedule>('/exams/schedules', data).then(r => r.data),
  deleteSchedule: (id: string) => api.delete(`/exams/schedules/${id}`),
  generateSeating: (scheduleId: string) =>
    api.post<SeatingArrangement[]>(`/exams/schedules/${scheduleId}/seating/generate`).then(r => r.data),
  getSeating: (scheduleId: string) =>
    api.get<SeatingArrangement[]>(`/exams/schedules/${scheduleId}/seating`).then(r => r.data),
  downloadSeatingPdf: (scheduleId: string) =>
    api.get(`/exams/schedules/${scheduleId}/seating/pdf`, { responseType: 'blob' }).then(r => {
      const url = window.URL.createObjectURL(new Blob([r.data]));
      const a = document.createElement('a');
      a.href = url; a.download = `seating-${scheduleId}.pdf`; a.click();
      window.URL.revokeObjectURL(url);
    }),
  downloadAdmitCard: (examId: string, studentId: string) =>
    api.get(`/exams/${examId}/admit-card/${studentId}`, { responseType: 'blob' }).then(r => {
      const url = window.URL.createObjectURL(new Blob([r.data]));
      const a = document.createElement('a'); a.href = url;
      a.download = `admit-card-${studentId}.pdf`; a.click();
      window.URL.revokeObjectURL(url);
    }),
};