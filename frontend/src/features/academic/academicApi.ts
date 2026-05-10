import api from '../../api/axiosInstance';

export interface AcademicYear {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  status: string;
  termCount: number;
  classCount: number;
  terms: Term[];
  createdAt: string;
}

export interface Term {
  id: string;
  name: string;
  termNumber: number;
  startDate: string;
  endDate: string;
  isCurrent: boolean;
  status: string;
  academicYearId: string;
  academicYearName: string;
  createdAt: string;
}

export interface Department {
  id: string;
  name: string;
  description?: string;
  headOfDepartmentId?: string;
  headOfDepartmentName?: string;
  staffCount: number;
  subjectCount: number;
  createdAt: string;
}

export interface ClassItem {
  id: string;
  name: string;
  section?: string;
  stream?: string;
  level: number;
  capacity: number;
  displayName: string;
  academicYearId: string;
  academicYearName: string;
  classTeacherId?: string;
  classTeacherName?: string;
  studentCount: number;
  subjectCount: number;
  subjects: ClassSubject[];
  createdAt: string;
}

export interface ClassSubject {
  id: string;
  subjectId: string;
  subjectName: string;
  subjectCode: string;
  teacherId?: string;
  teacherName?: string;
  isActive: boolean;
}

export interface Subject {
  id: string;
  name: string;
  code: string;
  type: string;
  creditHours: number;
  description?: string;
  isActive: boolean;
  departmentId?: string;
  departmentName?: string;
  classCount: number;
  createdAt: string;
}

export const academicApi = {
  // Academic Years
  getAcademicYears: () =>
    api.get<AcademicYear[]>('/academic-years').then(r => r.data),
  createAcademicYear: (data: unknown) =>
    api.post<AcademicYear>('/academic-years', data).then(r => r.data),
  updateAcademicYear: (id: string, data: unknown) =>
    api.put<AcademicYear>(`/academic-years/${id}`, data).then(r => r.data),
  deleteAcademicYear: (id: string) =>
    api.delete(`/academic-years/${id}`),
  setCurrentAcademicYear: (id: string) =>
    api.patch<AcademicYear>(`/academic-years/${id}/set-current`).then(r => r.data),

  // Terms
  getTerms: (academicYearId: string) =>
    api.get<Term[]>('/terms', { params: { academicYearId } }).then(r => r.data),
  createTerm: (data: unknown) =>
    api.post<Term>('/terms', data).then(r => r.data),
  updateTerm: (id: string, data: unknown) =>
    api.put<Term>(`/terms/${id}`, data).then(r => r.data),
  deleteTerm: (id: string) =>
    api.delete(`/terms/${id}`),
  setCurrentTerm: (id: string) =>
    api.patch<Term>(`/terms/${id}/set-current`).then(r => r.data),

  // Departments
  getDepartments: () =>
    api.get<Department[]>('/departments').then(r => r.data),
  createDepartment: (data: unknown) =>
    api.post<Department>('/departments', data).then(r => r.data),
  updateDepartment: (id: string, data: unknown) =>
    api.put<Department>(`/departments/${id}`, data).then(r => r.data),
  deleteDepartment: (id: string) =>
    api.delete(`/departments/${id}`),

  // Classes
  getClasses: (academicYearId?: string) =>
    api.get<ClassItem[]>('/classes', { params: academicYearId ? { academicYearId } : {} }).then(r => r.data),
  createClass: (data: unknown) =>
    api.post<ClassItem>('/classes', data).then(r => r.data),
  updateClass: (id: string, data: unknown) =>
    api.put<ClassItem>(`/classes/${id}`, data).then(r => r.data),
  deleteClass: (id: string) =>
    api.delete(`/classes/${id}`),
  assignSubject: (classId: string, data: unknown) =>
    api.post(`/classes/${classId}/subjects`, data),
  removeSubject: (classId: string, subjectId: string) =>
    api.delete(`/classes/${classId}/subjects/${subjectId}`),

  // Subjects
  getSubjects: () =>
    api.get<Subject[]>('/subjects').then(r => r.data),
  createSubject: (data: unknown) =>
    api.post<Subject>('/subjects', data).then(r => r.data),
  updateSubject: (id: string, data: unknown) =>
    api.put<Subject>(`/subjects/${id}`, data).then(r => r.data),
  deleteSubject: (id: string) =>
    api.delete(`/subjects/${id}`),
};