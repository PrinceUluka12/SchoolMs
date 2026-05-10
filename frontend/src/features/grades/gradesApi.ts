import api from '../../api/axiosInstance';

export interface GradeEntry {
  id: string;
  studentId: string;
  studentName: string;
  studentNumber: string;
  subjectId: string;
  subjectName: string;
  termId: string;
  termName: string;
  assessmentType: string;
  score: number;
  maxScore: number;
  percentage: number;
  isLocked: boolean;
  teacherRemark?: string;
  enteredByName: string;
  createdAt: string;
}

export interface ClassGradeSheet {
  className: string;
  subjectName: string;
  termName: string;
  assessmentType: string;
  grades: GradeEntry[];
}

export interface StudentTermReport {
  studentId: string;
  studentName: string;
  studentNumber: string;
  className: string;
  termName: string;
  academicYear: string;
  overallAverage: number;
  overallGrade: string;
  overallRemark: string;
  classRank: number;
  totalStudents: number;
  attendanceRate: number;
  subjects: {
    subjectName: string;
    subjectCode: string;
    assessments: {
      assessmentType: string;
      score: number;
      maxScore: number;
      weight: number;
      percentage: number;
    }[];
    weightedTotal: number;
    grade: string;
    remark: string;
    teacherRemark?: string;
  }[];
}

export const gradesApi = {
  getSheet: (classId: string, termId: string, subjectId: string, assessmentType: string) =>
    api.get<ClassGradeSheet>('/grades/sheet', {
      params: { classId, termId, subjectId, assessmentType }
    }).then(r => r.data),

  bulkEnter: (data: {
    subjectId: string;
    termId: string;
    classId: string;
    assessmentType: string;
    maxScore: number;
    entries: { studentId: string; score: number; teacherRemark?: string }[];
  }) => api.post('/grades/bulk', data).then(r => r.data),

  getStudentReport: (studentId: string, termId: string) =>
    api.get<StudentTermReport>(`/grades/student/${studentId}/report`, {
      params: { termId }
    }).then(r => r.data),

  getClassReports: (classId: string, termId: string) =>
    api.get<StudentTermReport[]>(`/grades/class/${classId}/reports`, {
      params: { termId }
    }).then(r => r.data),

  downloadReportCard: (studentId: string, termId: string) =>
    api.get(`/grades/report-card/${studentId}`, {
      params: { termId },
      responseType: 'blob'
    }).then(r => {
      const url = window.URL.createObjectURL(new Blob([r.data]));
      const a = document.createElement('a');
      a.href = url;
      a.download = `report-card-${studentId}.pdf`;
      a.click();
      window.URL.revokeObjectURL(url);
    }),

  downloadClassReportCards: (classId: string, termId: string) =>
    api.get(`/grades/report-cards/class/${classId}`, {
      params: { termId },
      responseType: 'blob'
    }).then(r => {
      const url = window.URL.createObjectURL(new Blob([r.data]));
      const a = document.createElement('a');
      a.href = url;
      a.download = `class-report-cards.pdf`;
      a.click();
      window.URL.revokeObjectURL(url);
    }),

  lockGrades: (classId: string, termId: string) =>
    api.post('/grades/lock', null, { params: { classId, termId } }),

  unlockGrades: (classId: string, termId: string) =>
    api.post('/grades/unlock', null, { params: { classId, termId } }),

  setWeights: (data: {
    classId: string;
    termId: string;
    weights: { assessmentType: string; weight: number }[];
  }) => api.post('/grades/weights', data),

  getWeights: (classId: string, termId: string) =>
    api.get('/grades/weights', { params: { classId, termId } }).then(r => r.data),
};