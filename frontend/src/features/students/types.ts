export interface Guardian {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  relationship: string;
}

export interface StudentDocument {
  id: string;
  name: string;
  fileUrl: string;
  fileType: string;
  fileSizeBytes: number;
  fileSizeFormatted: string;
  createdAt: string;
}

export interface Student {
  id: string;
  studentNumber: string;
  firstName: string;
  lastName: string;
  fullName: string;
  dateOfBirth: string;
  age: number;
  gender: string;
  photoUrl?: string;
  status: string;
  address?: string;
  medicalNotes?: string;
  classId?: string;
  className?: string;
  academicYear?: string;
  guardian?: Guardian;
  documents: StudentDocument[];
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

export interface StudentFilter {
  search?: string;
  classId?: string;
  status?: string;
  gender?: string;
  page: number;
  pageSize: number;
}