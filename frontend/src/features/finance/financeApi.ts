import api from '../../api/axiosInstance';

export interface FeeCategory {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  structureCount: number;
  createdAt: string;
}

export interface FeeStructure {
  id: string;
  feeCategoryId: string;
  feeCategoryName: string;
  termId: string;
  termName: string;
  classId?: string;
  className?: string;
  amount: number;
  dueDate: string;
  isActive: boolean;
  invoiceCount: number;
  createdAt: string;
}

export interface Payment {
  id: string;
  receiptNumber: string;
  feeInvoiceId: string;
  invoiceNumber: string;
  studentName: string;
  studentNumber: string;
  amount: number;
  paymentDate: string;
  paymentMode: string;
  referenceNumber?: string;
  notes?: string;
  recordedByName: string;
  createdAt: string;
}

export interface FeeInvoice {
  id: string;
  invoiceNumber: string;
  studentId: string;
  studentName: string;
  studentNumber: string;
  className?: string;
  feeCategoryName: string;
  termName: string;
  amount: number;
  paidAmount: number;
  discountAmount: number;
  balance: number;
  status: string;
  dueDate: string;
  isOverdue: boolean;
  notes?: string;
  payments: Payment[];
  createdAt: string;
}

export interface FinanceSummary {
  termName: string;
  totalBilled: number;
  totalCollected: number;
  totalOutstanding: number;
  totalDiscounts: number;
  collectionRate: number;
  totalInvoices: number;
  paidInvoices: number;
  partialInvoices: number;
  unpaidInvoices: number;
  overdueInvoices: number;
  byCategory: { categoryName: string; totalBilled: number; totalCollected: number; outstanding: number }[];
}

export const financeApi = {
  // Categories
  getCategories: () =>
    api.get<FeeCategory[]>('/finance/categories').then(r => r.data),
  createCategory: (data: { name: string; description?: string }) =>
    api.post<FeeCategory>('/finance/categories', data).then(r => r.data),
  deleteCategory: (id: string) =>
    api.delete(`/finance/categories/${id}`),

  // Structures
  getStructures: (termId?: string) =>
    api.get<FeeStructure[]>('/finance/structures', { params: { termId } }).then(r => r.data),
  createStructure: (data: unknown) =>
    api.post<FeeStructure>('/finance/structures', data).then(r => r.data),
  deleteStructure: (id: string) =>
    api.delete(`/finance/structures/${id}`),

  // Invoices
  generateInvoices: (data: { termId: string; classId?: string }) =>
    api.post('/finance/invoices/generate', data).then(r => r.data),
  getClassInvoices: (classId: string, termId: string) =>
    api.get<FeeInvoice[]>(`/finance/invoices/class/${classId}`, { params: { termId } }).then(r => r.data),
  getOverdueInvoices: (termId: string) =>
    api.get<FeeInvoice[]>('/finance/invoices/overdue', { params: { termId } }).then(r => r.data),
  getStudentStatement: (studentId: string, termId?: string) =>
    api.get(`/finance/student/${studentId}/statement`, { params: { termId } }).then(r => r.data),
  applyDiscount: (invoiceId: string, discountAmount: number, notes?: string) =>
    api.patch(`/finance/invoices/${invoiceId}/discount`, { discountAmount, notes }).then(r => r.data),

  // Payments
  recordPayment: (data: {
    feeInvoiceId: string;
    amount: number;
    paymentDate: string;
    paymentMode: string;
    referenceNumber?: string;
    notes?: string;
  }) => api.post<Payment>('/finance/payments', data).then(r => r.data),

  downloadReceipt: (paymentId: string) =>
    api.get(`/finance/payments/${paymentId}/receipt`, { responseType: 'blob' }).then(r => {
      const url = window.URL.createObjectURL(new Blob([r.data]));
      const a = document.createElement('a');
      a.href = url;
      a.download = `receipt-${paymentId}.pdf`;
      a.click();
      window.URL.revokeObjectURL(url);
    }),

  // Reports
  getSummary: (termId: string) =>
    api.get<FinanceSummary>('/finance/summary', { params: { termId } }).then(r => r.data),
};