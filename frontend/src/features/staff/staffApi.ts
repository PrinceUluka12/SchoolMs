import api from '../../api/axiosInstance';
import type { PagedResult, Staff, StaffFilter } from './types';

export const staffApi = {
  getAll: (filter: StaffFilter) =>
    api.get<PagedResult<Staff>>('/staff', { params: filter }).then(r => r.data),

  getById: (id: string) =>
    api.get<Staff>(`/staff/${id}`).then(r => r.data),

  create: (data: unknown) =>
    api.post<Staff>('/staff', data).then(r => r.data),

  update: (id: string, data: unknown) =>
    api.put<Staff>(`/staff/${id}`, data).then(r => r.data),

  delete: (id: string) =>
    api.delete(`/staff/${id}`),

  updateStatus: (id: string, status: string) =>
    api.patch<Staff>(`/staff/${id}/status`, JSON.stringify(status), {
      headers: { 'Content-Type': 'application/json' }
    }).then(r => r.data),

  uploadPhoto: (id: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return api.post<{ photoUrl: string }>(`/staff/${id}/photo`, form).then(r => r.data);
  },
};