import api from '../../api/axiosInstance';
import type { PagedResult, Student, StudentFilter } from './types';

export const studentApi = {
  getAll: (filter: StudentFilter) =>
    api.get<PagedResult<Student>>('/students', { params: filter }).then(r => r.data),

  getById: (id: string) =>
    api.get<Student>(`/students/${id}`).then(r => r.data),

  create: (data: unknown) =>
    api.post<Student>('/students', data).then(r => r.data),

  update: (id: string, data: unknown) =>
    api.put<Student>(`/students/${id}`, data).then(r => r.data),

  delete: (id: string) =>
    api.delete(`/students/${id}`),

  updateStatus: (id: string, status: string) =>
    api.patch<Student>(`/students/${id}/status`, JSON.stringify(status), {
      headers: { 'Content-Type': 'application/json' }
    }).then(r => r.data),

  assignClass: (studentId: string, classId: string) =>
    api.patch<Student>(`/students/${studentId}/class`, JSON.stringify(classId), {
      headers: { 'Content-Type': 'application/json' }
    }).then(r => r.data),

  uploadPhoto: (id: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return api.post<{ photoUrl: string }>(`/students/${id}/photo`, form).then(r => r.data);
  },

  uploadDocument: (id: string, name: string, file: File) => {
    const form = new FormData();
    form.append('name', name);
    form.append('file', file);
    return api.post(`/students/${id}/documents`, form).then(r => r.data);
  },

  deleteDocument: (studentId: string, documentId: string) =>
    api.delete(`/students/${studentId}/documents/${documentId}`),

  bulkImport: (csvFile: File) => {
    const form = new FormData();
    form.append('csvFile', csvFile);
    return api.post('/students/bulk-import', form).then(r => r.data);
  },
};