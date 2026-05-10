import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { libraryApi, type Book, type BookLoan } from './libraryApi';
import { format } from 'date-fns';

export default function LibraryPage() {
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<'books'|'loans'|'overdue'|'stats'>('books');
  const [search, setSearch] = useState('');
  const [showAddBook, setShowAddBook] = useState(false);
  const [showIssueLoan, setShowIssueLoan] = useState(false);
  const [selectedBook, setSelectedBook] = useState<Book | null>(null);
  const [bookForm, setBookForm] = useState({
    isbn: '', title: '', author: '', publisher: '', publicationYear: '',
    category: 'General', totalQuantity: '1', shelfLocation: '', description: ''
  });
  const [loanForm, setLoanForm] = useState({
    borrowerId: '', borrowerType: 'Student', dueDate: ''
  });

  const { data: books = [], isLoading: booksLoading } = useQuery({
    queryKey: ['books', search],
    queryFn: () => libraryApi.searchBooks(search || undefined),
  });

  const { data: activeLoans = [] } = useQuery({
    queryKey: ['active-loans'],
    queryFn: libraryApi.getActiveLoans,
    enabled: tab === 'loans',
  });

  const { data: overdueLoans = [] } = useQuery({
    queryKey: ['overdue-loans'],
    queryFn: libraryApi.getOverdueLoans,
    enabled: tab === 'overdue',
  });

  const { data: stats } = useQuery({
    queryKey: ['library-stats'],
    queryFn: libraryApi.getStats,
    enabled: tab === 'stats',
  });

  const addBookMutation = useMutation({
    mutationFn: () => libraryApi.addBook({
      ...bookForm,
      publicationYear: bookForm.publicationYear ? parseInt(bookForm.publicationYear) : undefined,
      totalQuantity: parseInt(bookForm.totalQuantity)
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['books'] });
      queryClient.invalidateQueries({ queryKey: ['library-stats'] });
      setShowAddBook(false);
      setBookForm({ isbn: '', title: '', author: '', publisher: '', publicationYear: '',
        category: 'General', totalQuantity: '1', shelfLocation: '', description: '' });
    },
    onError: (e: any) => alert(e.response?.data?.message ?? 'Error adding book'),
  });

  const issueLoanMutation = useMutation({
    mutationFn: () => libraryApi.issueLoan({
      bookId: selectedBook!.id,
      borrowerId: loanForm.borrowerId,
      borrowerType: loanForm.borrowerType,
      dueDate: loanForm.dueDate,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['books'] });
      queryClient.invalidateQueries({ queryKey: ['active-loans'] });
      queryClient.invalidateQueries({ queryKey: ['library-stats'] });
      setShowIssueLoan(false);
      setSelectedBook(null);
    },
    onError: (e: any) => alert(e.response?.data?.message ?? 'Error issuing loan'),
  });

  const returnMutation = useMutation({
    mutationFn: (loanId: string) => libraryApi.returnLoan(loanId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['active-loans'] });
      queryClient.invalidateQueries({ queryKey: ['overdue-loans'] });
      queryClient.invalidateQueries({ queryKey: ['books'] });
      queryClient.invalidateQueries({ queryKey: ['library-stats'] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: libraryApi.deleteBook,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['books'] }),
    onError: (e: any) => alert(e.response?.data?.message ?? 'Cannot delete'),
  });

  const CATEGORIES = ['General', 'Science', 'Mathematics', 'History', 'Literature',
    'Technology', 'Arts', 'Reference', 'Fiction', 'Non-Fiction'];

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Library</h1>
          <p className="text-sm text-gray-500 mt-1">Catalogue, loans, and fines management</p>
        </div>
        {tab === 'books' && (
          <button onClick={() => setShowAddBook(true)}
            className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition">
            + Add Book
          </button>
        )}
      </div>

      {/* Tabs */}
      <div className="flex gap-1 bg-gray-100 rounded-xl p-1 mb-6 w-fit">
        {(['books', 'loans', 'overdue', 'stats'] as const).map(t => (
          <button key={t} onClick={() => setTab(t)}
            className={`px-4 py-2 text-sm font-medium rounded-lg transition capitalize
              ${tab === t ? 'bg-white text-blue-700 shadow-sm' : 'text-gray-600 hover:text-gray-800'}`}>
            {t}
            {t === 'overdue' && (overdueLoans as BookLoan[]).length > 0 && (
              <span className="ml-1.5 bg-red-500 text-white text-xs rounded-full px-1.5 py-0.5">
                {(overdueLoans as BookLoan[]).length}
              </span>
            )}
          </button>
        ))}
      </div>

      {/* Add Book Form */}
      {showAddBook && (
        <div className="bg-white border border-gray-200 rounded-xl p-5 mb-6 shadow-sm">
          <h3 className="font-semibold text-gray-800 mb-4">Add New Book</h3>
          <div className="grid grid-cols-3 gap-4 mb-4">
            {[
              ['ISBN *', 'isbn', 'text'],
              ['Title *', 'title', 'text'],
              ['Author *', 'author', 'text'],
              ['Publisher', 'publisher', 'text'],
              ['Publication Year', 'publicationYear', 'number'],
              ['Shelf Location', 'shelfLocation', 'text'],
              ['Total Quantity *', 'totalQuantity', 'number'],
            ].map(([label, field, type]) => (
              <div key={field}>
                <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
                <input type={type} value={(bookForm as any)[field]}
                  onChange={e => setBookForm(f => ({ ...f, [field]: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
            ))}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Category *</label>
              <select value={bookForm.category}
                onChange={e => setBookForm(f => ({ ...f, category: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                {CATEGORIES.map(c => <option key={c}>{c}</option>)}
              </select>
            </div>
          </div>
          <div className="flex gap-3">
            <button onClick={() => addBookMutation.mutate()} disabled={addBookMutation.isPending}
              className="px-4 py-2 bg-blue-700 text-white text-sm font-semibold rounded-lg hover:bg-blue-800 disabled:opacity-50 transition">
              {addBookMutation.isPending ? 'Adding...' : 'Add Book'}
            </button>
            <button onClick={() => setShowAddBook(false)}
              className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition">Cancel</button>
          </div>
        </div>
      )}

      {/* Books Tab */}
      {tab === 'books' && (
        <div>
          <div className="flex gap-3 mb-4">
            <input type="text" placeholder="Search by title, author, or ISBN..."
              value={search} onChange={e => setSearch(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && queryClient.invalidateQueries({ queryKey: ['books'] })}
              className="flex-1 border border-gray-300 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>

          <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="text-left px-4 py-3 font-semibold text-gray-600">Book</th>
                  <th className="text-left px-4 py-3 font-semibold text-gray-600">ISBN</th>
                  <th className="text-left px-4 py-3 font-semibold text-gray-600">Category</th>
                  <th className="text-left px-4 py-3 font-semibold text-gray-600">Shelf</th>
                  <th className="text-center px-4 py-3 font-semibold text-gray-600">Available</th>
                  <th className="text-center px-4 py-3 font-semibold text-gray-600">Total</th>
                  <th className="text-left px-4 py-3 font-semibold text-gray-600">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {booksLoading ? (
                  Array.from({ length: 5 }).map((_, i) => (
                    <tr key={i} className="animate-pulse">
                      {Array.from({ length: 7 }).map((_, j) => (
                        <td key={j} className="px-4 py-3"><div className="h-4 bg-gray-200 rounded w-3/4" /></td>
                      ))}
                    </tr>
                  ))
                ) : (books as Book[]).length === 0 ? (
                  <tr><td colSpan={7} className="text-center py-8 text-gray-400">No books found.</td></tr>
                ) : (
                  (books as Book[]).map(book => (
                    <tr key={book.id} className="hover:bg-gray-50 transition">
                      <td className="px-4 py-3">
                        <p className="font-medium text-gray-900">{book.title}</p>
                        <p className="text-xs text-gray-400">{book.author}</p>
                      </td>
                      <td className="px-4 py-3 font-mono text-xs text-gray-600">{book.isbn}</td>
                      <td className="px-4 py-3">
                        <span className="px-2 py-1 bg-blue-100 text-blue-800 text-xs rounded-full">{book.category}</span>
                      </td>
                      <td className="px-4 py-3 text-gray-500 text-xs">{book.shelfLocation ?? '—'}</td>
                      <td className="px-4 py-3 text-center">
                        <span className={`font-bold ${book.availableQuantity > 0 ? 'text-green-700' : 'text-red-600'}`}>
                          {book.availableQuantity}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-center text-gray-600">{book.totalQuantity}</td>
                      <td className="px-4 py-3">
                        <div className="flex gap-2">
                          {book.availableQuantity > 0 && (
                            <button onClick={() => { setSelectedBook(book); setShowIssueLoan(true); }}
                              className="text-xs text-blue-600 hover:underline">Issue</button>
                          )}
                          <button onClick={() => { if (confirm('Delete book?')) deleteMutation.mutate(book.id); }}
                            className="text-xs text-red-500 hover:underline">Delete</button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Loans Tab */}
      {tab === 'loans' && (
        <LoanTable loans={activeLoans as BookLoan[]} onReturn={id => returnMutation.mutate(id)} title="Active Loans" />
      )}

      {/* Overdue Tab */}
      {tab === 'overdue' && (
        <LoanTable loans={overdueLoans as BookLoan[]} onReturn={id => returnMutation.mutate(id)} title="Overdue Loans" isOverdue />
      )}

      {/* Stats Tab */}
      {tab === 'stats' && stats && (
        <div className="space-y-6">
          <div className="grid grid-cols-3 md:grid-cols-6 gap-4">
            {[
              { label: 'Total Books', value: stats.totalBooks, color: 'text-blue-700' },
              { label: 'Total Copies', value: stats.totalCopies, color: 'text-gray-700' },
              { label: 'Available', value: stats.availableCopies, color: 'text-green-700' },
              { label: 'Active Loans', value: stats.activeLoans, color: 'text-purple-700' },
              { label: 'Overdue', value: stats.overdueLoans, color: 'text-red-700' },
              { label: 'Fines Outstanding', value: `$${stats.totalFinesOutstanding.toFixed(2)}`, color: 'text-amber-700' },
            ].map(card => (
              <div key={card.label} className="bg-white rounded-xl border border-gray-200 shadow-sm p-4 text-center">
                <p className={`text-2xl font-bold ${card.color}`}>{card.value}</p>
                <p className="text-xs text-gray-500 mt-1">{card.label}</p>
              </div>
            ))}
          </div>
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <h3 className="font-semibold text-gray-800 mb-4">Most Borrowed Books</h3>
            {stats.mostBorrowed.map((book, i) => (
              <div key={book.id} className="flex items-center justify-between py-2 border-b border-gray-100 last:border-0">
                <div className="flex items-center gap-3">
                  <span className="w-6 h-6 rounded-full bg-blue-100 text-blue-700 text-xs font-bold flex items-center justify-center">{i + 1}</span>
                  <div>
                    <p className="text-sm font-medium text-gray-900">{book.title}</p>
                    <p className="text-xs text-gray-400">{book.author}</p>
                  </div>
                </div>
                <span className="text-xs text-gray-500">{book.checkedOutQuantity} checked out</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Issue Loan Modal */}
      {showIssueLoan && selectedBook && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
            <div className="flex items-center justify-between px-6 py-4 border-b">
              <h2 className="text-lg font-bold text-gray-900">Issue Book</h2>
              <button onClick={() => setShowIssueLoan(false)} className="text-gray-400 hover:text-gray-600 text-2xl leading-none">&times;</button>
            </div>
            <div className="p-6 space-y-4">
              <div className="bg-blue-50 rounded-lg p-3">
                <p className="font-medium text-gray-800">{selectedBook.title}</p>
                <p className="text-sm text-gray-500">{selectedBook.author} · Available: {selectedBook.availableQuantity}</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Borrower User ID *</label>
                <input value={loanForm.borrowerId} onChange={e => setLoanForm(f => ({ ...f, borrowerId: e.target.value }))}
                  placeholder="Paste User ID"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Borrower Type</label>
                <select value={loanForm.borrowerType} onChange={e => setLoanForm(f => ({ ...f, borrowerType: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                  <option>Student</option><option>Staff</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Due Date *</label>
                <input type="date" value={loanForm.dueDate} onChange={e => setLoanForm(f => ({ ...f, dueDate: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
            </div>
            <div className="flex justify-end gap-3 px-6 py-4 border-t bg-gray-50 rounded-b-2xl">
              <button onClick={() => setShowIssueLoan(false)} className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition">Cancel</button>
              <button onClick={() => issueLoanMutation.mutate()} disabled={issueLoanMutation.isPending}
                className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition disabled:opacity-50">
                {issueLoanMutation.isPending ? 'Issuing...' : 'Issue Book'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function LoanTable({ loans, onReturn, title, isOverdue = false }: {
  loans: BookLoan[]; onReturn: (id: string) => void; title: string; isOverdue?: boolean;
}) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="px-5 py-3 border-b border-gray-200 bg-gray-50">
        <p className="font-semibold text-gray-800">{title} ({loans.length})</p>
      </div>
      <table className="w-full text-sm">
        <thead className="bg-gray-50 border-b border-gray-200">
          <tr>
            <th className="text-left px-4 py-3 font-semibold text-gray-600">Book</th>
            <th className="text-left px-4 py-3 font-semibold text-gray-600">Borrower</th>
            <th className="text-left px-4 py-3 font-semibold text-gray-600">Issued</th>
            <th className="text-left px-4 py-3 font-semibold text-gray-600">Due Date</th>
            {isOverdue && <th className="text-left px-4 py-3 font-semibold text-gray-600">Days Overdue</th>}
            <th className="text-left px-4 py-3 font-semibold text-gray-600">Fine</th>
            <th className="text-left px-4 py-3 font-semibold text-gray-600">Actions</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100">
          {loans.length === 0 ? (
            <tr><td colSpan={7} className="text-center py-8 text-gray-400">No records.</td></tr>
          ) : loans.map(loan => (
            <tr key={loan.id} className={`hover:bg-gray-50 ${isOverdue ? 'bg-red-50' : ''}`}>
              <td className="px-4 py-3">
                <p className="font-medium text-gray-900">{loan.bookTitle}</p>
                <p className="text-xs text-gray-400">{loan.bookISBN}</p>
              </td>
              <td className="px-4 py-3">
                <p className="text-gray-700">{loan.borrowerName}</p>
                <p className="text-xs text-gray-400">{loan.borrowerType}</p>
              </td>
              <td className="px-4 py-3 text-gray-600">{format(new Date(loan.issueDate), 'MMM d, yyyy')}</td>
              <td className="px-4 py-3 text-gray-600">{format(new Date(loan.dueDate), 'MMM d, yyyy')}</td>
              {isOverdue && <td className="px-4 py-3 text-red-700 font-medium">{loan.daysOverdue} days</td>}
              <td className="px-4 py-3">
                {loan.fineAmount > 0
                  ? <span className="text-red-600 font-medium">${loan.fineAmount.toFixed(2)}</span>
                  : <span className="text-gray-400">—</span>}
              </td>
              <td className="px-4 py-3">
                <button onClick={() => { if (confirm('Return this book?')) onReturn(loan.id); }}
                  className="text-xs text-blue-600 hover:underline">Return</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}