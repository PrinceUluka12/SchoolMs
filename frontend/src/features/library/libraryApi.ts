import api from '../../api/axiosInstance';

export interface Book {
  id: string; isbn: string; title: string; author: string;
  publisher?: string; publicationYear?: number; category: string;
  totalQuantity: number; availableQuantity: number; checkedOutQuantity: number;
  shelfLocation?: string; coverUrl?: string; description?: string;
  isAvailable: boolean; createdAt: string;
}

export interface BookLoan {
  id: string; bookId: string; bookTitle: string; bookISBN: string; bookAuthor: string;
  borrowerId: string; borrowerName: string; borrowerType: string; issuedByName: string;
  issueDate: string; dueDate: string; returnDate?: string; status: string;
  fineAmount: number; finePaid: boolean; isOverdue: boolean; daysOverdue: number;
  notes?: string; createdAt: string;
}

export interface LibraryStats {
  totalBooks: number; totalCopies: number; availableCopies: number;
  activeLoans: number; overdueLoans: number; totalFinesOutstanding: number;
  mostBorrowed: Book[];
}

export const libraryApi = {
  searchBooks: (search?: string, category?: string) =>
    api.get<Book[]>('/library/books', { params: { search, category } }).then(r => r.data),
  addBook: (data: unknown) => api.post<Book>('/library/books', data).then(r => r.data),
  updateBook: (id: string, data: unknown) => api.put<Book>(`/library/books/${id}`, data).then(r => r.data),
  deleteBook: (id: string) => api.delete(`/library/books/${id}`),
  issueLoan: (data: { bookId: string; borrowerId: string; borrowerType: string; dueDate: string; notes?: string }) =>
    api.post<BookLoan>('/library/loans', data).then(r => r.data),
  returnLoan: (id: string, notes?: string) =>
    api.patch<BookLoan>(`/library/loans/${id}/return`, { notes }).then(r => r.data),
  getActiveLoans: () => api.get<BookLoan[]>('/library/loans/active').then(r => r.data),
  getOverdueLoans: () => api.get<BookLoan[]>('/library/loans/overdue').then(r => r.data),
  getStats: () => api.get<LibraryStats>('/library/stats').then(r => r.data),
};