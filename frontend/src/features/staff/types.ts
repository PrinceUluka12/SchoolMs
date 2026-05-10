export interface StaffDocument {
  id: string;
  name: string;
  fileUrl: string;
  fileType: string;
  fileSizeBytes: number;
  fileSizeFormatted: string;
  createdAt: string;
}

export interface Staff {
  id: string;
  staffNumber: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  gender: string;
  role: string;
  contractType: string;
  status: string;
  photoUrl?: string;
  address?: string;
  qualifications?: string;
  joinDate: string;
  departmentId?: string;
  departmentName?: string;
  documents: StaffDocument[];
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface StaffFilter {
  search?: string;
  role?: string;
  departmentId?: string;
  status?: string;
  page: number;
  pageSize: number;
}