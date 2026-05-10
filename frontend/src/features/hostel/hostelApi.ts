import api from '../../api/axiosInstance';

export interface Building {
  id: string; name: string; gender: string; description?: string;
  wardenId?: string; wardenName?: string; totalRooms: number;
  totalCapacity: number; occupiedBeds: number; availableBeds: number;
  rooms: Room[]; createdAt: string;
}

export interface Room {
  id: string; roomNumber: string; capacity: number; type: string;
  status: string; buildingId: string; buildingName: string;
  occupiedBeds: number; availableBeds: number;
  currentAllocations: Allocation[]; createdAt: string;
}

export interface Allocation {
  id: string; studentId: string; studentName: string; studentNumber: string;
  roomId: string; roomNumber: string; buildingName: string;
  bedNumber: string; isActive: boolean; termName: string; createdAt: string;
}

export const hostelApi = {
  getBuildings: () => api.get<Building[]>('/hostel/buildings').then(r => r.data),
  createBuilding: (data: unknown) => api.post<Building>('/hostel/buildings', data).then(r => r.data),
  deleteBuilding: (id: string) => api.delete(`/hostel/buildings/${id}`),
  getRooms: (buildingId: string) => api.get<Room[]>(`/hostel/buildings/${buildingId}/rooms`).then(r => r.data),
  createRoom: (data: unknown) => api.post<Room>('/hostel/rooms', data).then(r => r.data),
  deleteRoom: (id: string) => api.delete(`/hostel/rooms/${id}`),
  getAllocations: (termId: string) =>
    api.get<Allocation[]>('/hostel/allocations', { params: { termId } }).then(r => r.data),
  allocateStudent: (data: unknown) => api.post<Allocation>('/hostel/allocations', data).then(r => r.data),
  deallocate: (id: string) => api.delete(`/hostel/allocations/${id}`),
};