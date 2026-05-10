import api from '../../api/axiosInstance';

export interface Vehicle {
  id: string; plateNumber: string; type: string; capacity: number;
  model?: string; color?: string; status: string;
  driverId?: string; driverName?: string; routeCount: number; createdAt: string;
}

export interface Route {
  id: string; name: string; description?: string; stops: string[];
  morningPickupFee?: number; afternoonDropFee?: number;
  isActive: boolean; vehicleId?: string; vehiclePlate?: string;
  driverName?: string; studentCount: number; createdAt: string;
}

export interface StudentTransport {
  id: string; studentId: string; studentName: string; studentNumber: string;
  routeId: string; routeName: string; pickupStop: string; dropStop: string;
  type: string; isActive: boolean; termName: string; createdAt: string;
}

export const transportApi = {
  getVehicles: () => api.get<Vehicle[]>('/transport/vehicles').then(r => r.data),
  createVehicle: (data: unknown) => api.post<Vehicle>('/transport/vehicles', data).then(r => r.data),
  updateVehicle: (id: string, data: unknown) => api.put<Vehicle>(`/transport/vehicles/${id}`, data).then(r => r.data),
  deleteVehicle: (id: string) => api.delete(`/transport/vehicles/${id}`),
  getRoutes: () => api.get<Route[]>('/transport/routes').then(r => r.data),
  createRoute: (data: unknown) => api.post<Route>('/transport/routes', data).then(r => r.data),
  deleteRoute: (id: string) => api.delete(`/transport/routes/${id}`),
  assignStudent: (data: unknown) => api.post('/transport/assignments', data).then(r => r.data),
  getStudentsByRoute: (routeId: string, termId: string) =>
    api.get<StudentTransport[]>(`/transport/routes/${routeId}/students`, { params: { termId } }).then(r => r.data),
  removeAssignment: (id: string) => api.delete(`/transport/assignments/${id}`),
};