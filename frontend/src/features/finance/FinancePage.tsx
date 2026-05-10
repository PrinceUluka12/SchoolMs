import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { financeApi, type FeeInvoice } from './financeApi';
import { academicApi } from '../academic/academicApi';
import { format } from 'date-fns';

const STATUS_COLORS: Record<string, string> = {
  Paid:          'bg-green-100 text-green-800',
  PartiallyPaid: 'bg-blue-100 text-blue-800',
  Unpaid:        'bg-yellow-100 text-yellow-800',
  Overdue:       'bg-red-100 text-red-800',
  Waived:        'bg-gray-100 text-gray-600',
};

export default function FinancePage() {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'dashboard'|'invoices'|'categories'|'structures'>('dashboard');
  const [selectedTerm, setSelectedTerm] = useState('');
  const [selectedClass, setSelectedClass] = useState('');
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  const [selectedInvoice, setSelectedInvoice] = useState<FeeInvoice | null>(null);
  const [paymentForm, setPaymentForm] = useState({
    amount: '', paymentDate: format(new Date(), 'yyyy-MM-dd'),
    paymentMode: 'Cash', referenceNumber: '', notes: ''
  });

  // Categories form
  const [newCategory, setNewCategory] = useState({ name: '', description: '' });
  // Structure form
  const [newStructure, setNewStructure] = useState({
    feeCategoryId: '', termId: '', classId: '', amount: '', dueDate: ''
  });

  const { data: years = [] } = useQuery({ queryKey: ['academic-years'], queryFn: academicApi.getAcademicYears });
  const { data: classes = [] } = useQuery({ queryKey: ['classes'], queryFn: () => academicApi.getClasses() });
  const { data: categories = [] } = useQuery({ queryKey: ['fee-categories'], queryFn: financeApi.getCategories });
  const allTerms = (years as any[]).flatMap((y: any) => y.terms ?? []);

  const { data: summary } = useQuery({
    queryKey: ['finance-summary', selectedTerm],
    queryFn: () => financeApi.getSummary(selectedTerm),
    enabled: !!selectedTerm,
  });

  const { data: invoices = [], isLoading: invoicesLoading } = useQuery({
    queryKey: ['class-invoices', selectedClass, selectedTerm],
    queryFn: () => financeApi.getClassInvoices(selectedClass, selectedTerm),
    enabled: !!(selectedClass && selectedTerm),
  });

  const { data: structures = [] } = useQuery({
    queryKey: ['fee-structures', selectedTerm],
    queryFn: () => financeApi.getStructures(selectedTerm || undefined),
  });

  const generateMutation = useMutation({
    mutationFn: () => financeApi.generateInvoices({ termId: selectedTerm, classId: selectedClass || undefined }),
    onSuccess: (data: any) => {
      alert(`Generated ${data.generated} invoices.`);
      queryClient.invalidateQueries({ queryKey: ['class-invoices'] });
    },
    onError: (e: any) => alert(e.response?.data?.message ?? 'Error generating invoices'),
  });

  const paymentMutation = useMutation({
    mutationFn: () => financeApi.recordPayment({
      feeInvoiceId: selectedInvoice!.id,
      amount: parseFloat(paymentForm.amount),
      paymentDate: paymentForm.paymentDate,
      paymentMode: paymentForm.paymentMode,
      referenceNumber: paymentForm.referenceNumber || undefined,
      notes: paymentForm.notes || undefined,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['class-invoices'] });
      queryClient.invalidateQueries({ queryKey: ['finance-summary'] });
      setShowPaymentModal(false);
      setSelectedInvoice(null);
    },
    onError: (e: any) => alert(e.response?.data?.message ?? 'Error recording payment'),
  });

  const createCategoryMutation = useMutation({
    mutationFn: () => financeApi.createCategory(newCategory),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['fee-categories'] });
      setNewCategory({ name: '', description: '' });
    },
    onError: (e: any) => alert(e.response?.data?.message ?? 'Error'),
  });

  const createStructureMutation = useMutation({
    mutationFn: () => financeApi.createStructure({
      feeCategoryId: newStructure.feeCategoryId,
      termId: newStructure.termId,
      classId: newStructure.classId || undefined,
      amount: parseFloat(newStructure.amount),
      dueDate: newStructure.dueDate,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['fee-structures'] });
      setNewStructure({ feeCategoryId: '', termId: '', classId: '', amount: '', dueDate: '' });
    },
    onError: (e: any) => alert(e.response?.data?.message ?? 'Error'),
  });

  const TABS = ['dashboard', 'invoices', 'categories', 'structures'] as const;

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Finance</h1>
          <p className="text-sm text-gray-500 mt-1">Fee management, payments, and financial reports</p>
        </div>
        <div className="flex gap-3 items-center">
          <select value={selectedTerm} onChange={e => setSelectedTerm(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none">
            <option value="">Select Term</option>
            {allTerms.map((t: any) => <option key={t.id} value={t.id}>{t.name}</option>)}
          </select>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 bg-gray-100 rounded-xl p-1 mb-6 w-fit">
        {TABS.map(tab => (
          <button key={tab} onClick={() => setActiveTab(tab)}
            className={`px-4 py-2 text-sm font-medium rounded-lg transition capitalize
              ${activeTab === tab ? 'bg-white text-blue-700 shadow-sm' : 'text-gray-600 hover:text-gray-800'}`}>
            {tab}
          </button>
        ))}
      </div>

      {/* ── Dashboard Tab ────────────────────────────────────────────────── */}
      {activeTab === 'dashboard' && (
        <div className="space-y-6">
          {!selectedTerm ? (
            <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
              Select a term to view the financial summary.
            </div>
          ) : !summary ? (
            <div className="grid grid-cols-4 gap-4">
              {[1,2,3,4].map(i => <div key={i} className="bg-white rounded-xl h-24 animate-pulse border border-gray-200" />)}
            </div>
          ) : (
            <>
              {/* KPI Cards */}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                {[
                  { label: 'Total Billed', value: `$${summary.totalBilled.toLocaleString()}`, color: 'text-blue-700' },
                  { label: 'Collected', value: `$${summary.totalCollected.toLocaleString()}`, color: 'text-green-700' },
                  { label: 'Outstanding', value: `$${summary.totalOutstanding.toLocaleString()}`, color: 'text-amber-600' },
                  { label: 'Collection Rate', value: `${summary.collectionRate}%`, color: 'text-purple-700' },
                ].map(card => (
                  <div key={card.label} className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
                    <p className="text-sm text-gray-500">{card.label}</p>
                    <p className={`text-2xl font-bold mt-1 ${card.color}`}>{card.value}</p>
                  </div>
                ))}
              </div>

              {/* Invoice Status Row */}
              <div className="grid grid-cols-5 gap-4">
                {[
                  { label: 'Total Invoices', value: summary.totalInvoices, color: 'text-gray-700' },
                  { label: 'Paid', value: summary.paidInvoices, color: 'text-green-700' },
                  { label: 'Partially Paid', value: summary.partialInvoices, color: 'text-blue-700' },
                  { label: 'Unpaid', value: summary.unpaidInvoices, color: 'text-yellow-700' },
                  { label: 'Overdue', value: summary.overdueInvoices, color: 'text-red-700' },
                ].map(card => (
                  <div key={card.label} className="bg-white rounded-xl border border-gray-200 shadow-sm p-4 text-center">
                    <p className={`text-2xl font-bold ${card.color}`}>{card.value}</p>
                    <p className="text-xs text-gray-500 mt-1">{card.label}</p>
                  </div>
                ))}
              </div>

              {/* By Category Table */}
              {summary.byCategory.length > 0 && (
                <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
                  <div className="px-5 py-3 border-b border-gray-200 bg-gray-50">
                    <p className="font-semibold text-gray-800">Collection by Category</p>
                  </div>
                  <table className="w-full text-sm">
                    <thead className="bg-gray-50 border-b border-gray-200">
                      <tr>
                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Category</th>
                        <th className="text-right px-4 py-3 font-semibold text-gray-600">Billed</th>
                        <th className="text-right px-4 py-3 font-semibold text-gray-600">Collected</th>
                        <th className="text-right px-4 py-3 font-semibold text-gray-600">Outstanding</th>
                        <th className="text-right px-4 py-3 font-semibold text-gray-600">Rate</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                      {summary.byCategory.map((cat, i) => {
                        const rate = cat.totalBilled > 0
                          ? Math.round(cat.totalCollected / cat.totalBilled * 100)
                          : 0;
                        return (
                          <tr key={i} className="hover:bg-gray-50">
                            <td className="px-4 py-3 font-medium text-gray-900">{cat.categoryName}</td>
                            <td className="px-4 py-3 text-right text-gray-700">${cat.totalBilled.toLocaleString()}</td>
                            <td className="px-4 py-3 text-right text-green-700 font-medium">${cat.totalCollected.toLocaleString()}</td>
                            <td className="px-4 py-3 text-right text-amber-700">${cat.outstanding.toLocaleString()}</td>
                            <td className="px-4 py-3 text-right">
                              <div className="flex items-center justify-end gap-2">
                                <div className="w-16 bg-gray-200 rounded-full h-1.5">
                                  <div className="bg-blue-600 h-1.5 rounded-full" style={{ width: `${rate}%` }} />
                                </div>
                                <span className="text-xs text-gray-600">{rate}%</span>
                              </div>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </>
          )}
        </div>
      )}

      {/* ── Invoices Tab ─────────────────────────────────────────────────── */}
      {activeTab === 'invoices' && (
        <div className="space-y-4">
          <div className="flex gap-3">
            <select value={selectedClass} onChange={e => setSelectedClass(e.target.value)}
              className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none">
              <option value="">All Classes</option>
              {(classes as any[]).map((c: any) => (
                <option key={c.id} value={c.id}>{c.displayName}</option>
              ))}
            </select>
            <button
              onClick={() => generateMutation.mutate()}
              disabled={!selectedTerm || generateMutation.isPending}
              className="px-4 py-2 bg-green-700 hover:bg-green-800 text-white text-sm font-semibold rounded-lg transition disabled:opacity-50">
              {generateMutation.isPending ? 'Generating...' : '⚡ Generate Invoices'}
            </button>
          </div>

          {!(selectedClass && selectedTerm) ? (
            <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
              Select a class and term to view invoices.
            </div>
          ) : invoicesLoading ? (
            <div className="bg-white rounded-xl border border-gray-200 p-12 text-center animate-pulse text-gray-300">
              Loading invoices...
            </div>
          ) : (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="text-left px-4 py-3 font-semibold text-gray-600">Student</th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-600">Invoice #</th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-600">Category</th>
                    <th className="text-right px-4 py-3 font-semibold text-gray-600">Amount</th>
                    <th className="text-right px-4 py-3 font-semibold text-gray-600">Paid</th>
                    <th className="text-right px-4 py-3 font-semibold text-gray-600">Balance</th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-600">Due Date</th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-600">Status</th>
                    <th className="text-left px-4 py-3 font-semibold text-gray-600">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {(invoices as FeeInvoice[]).length === 0 ? (
                    <tr>
                      <td colSpan={9} className="text-center py-8 text-gray-400">
                        No invoices found. Click Generate Invoices to create them.
                      </td>
                    </tr>
                  ) : (invoices as FeeInvoice[]).map(inv => (
                    <tr key={inv.id} className={`hover:bg-gray-50 ${inv.isOverdue ? 'bg-red-50' : ''}`}>
                      <td className="px-4 py-3">
                        <p className="font-medium text-gray-900">{inv.studentName}</p>
                        <p className="text-xs text-gray-400">{inv.studentNumber}</p>
                      </td>
                      <td className="px-4 py-3 font-mono text-xs text-gray-700">{inv.invoiceNumber}</td>
                      <td className="px-4 py-3 text-gray-600">{inv.feeCategoryName}</td>
                      <td className="px-4 py-3 text-right text-gray-900">${inv.amount.toLocaleString()}</td>
                      <td className="px-4 py-3 text-right text-green-700">${inv.paidAmount.toLocaleString()}</td>
                      <td className="px-4 py-3 text-right font-medium text-amber-700">${inv.balance.toLocaleString()}</td>
                      <td className="px-4 py-3 text-gray-600">
                        {format(new Date(inv.dueDate), 'MMM d, yyyy')}
                        {inv.isOverdue && <span className="ml-1 text-red-500 text-xs">• Overdue</span>}
                      </td>
                      <td className="px-4 py-3">
                        <span className={`px-2 py-1 rounded-full text-xs font-medium ${STATUS_COLORS[inv.status] ?? 'bg-gray-100'}`}>
                          {inv.status}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        {inv.balance > 0 && inv.status !== 'Waived' && (
                          <button
                            onClick={() => { setSelectedInvoice(inv); setShowPaymentModal(true); setPaymentForm(f => ({ ...f, amount: inv.balance.toString() })); }}
                            className="text-xs text-blue-600 hover:underline">
                            Record Payment
                          </button>
                        )}
                        {inv.payments.length > 0 && (
                          <button
                            onClick={() => financeApi.downloadReceipt(inv.payments[inv.payments.length - 1].id)}
                            className="text-xs text-green-600 hover:underline ml-2">
                            Receipt
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* ── Categories Tab ───────────────────────────────────────────────── */}
      {activeTab === 'categories' && (
        <div className="space-y-4">
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <h3 className="font-semibold text-gray-800 mb-4">New Fee Category</h3>
            <div className="grid grid-cols-2 gap-4 mb-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Name *</label>
                <input value={newCategory.name}
                  onChange={e => setNewCategory(f => ({ ...f, name: e.target.value }))}
                  placeholder="e.g. Tuition, Transport, Hostel"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                <input value={newCategory.description}
                  onChange={e => setNewCategory(f => ({ ...f, description: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
            </div>
            <button onClick={() => createCategoryMutation.mutate()}
              disabled={createCategoryMutation.isPending}
              className="px-4 py-2 bg-blue-700 text-white text-sm font-semibold rounded-lg hover:bg-blue-800 disabled:opacity-50 transition">
              {createCategoryMutation.isPending ? 'Creating...' : 'Create Category'}
            </button>
          </div>

          <div className="grid grid-cols-3 gap-4">
            {(categories as any[]).map((cat: any) => (
              <div key={cat.id} className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
                <div className="flex items-start justify-between">
                  <div>
                    <h3 className="font-semibold text-gray-900">{cat.name}</h3>
                    {cat.description && <p className="text-sm text-gray-500 mt-1">{cat.description}</p>}
                  </div>
                  <span className={`px-2 py-1 text-xs rounded-full ${cat.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-600'}`}>
                    {cat.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
                <p className="text-sm text-gray-400 mt-2">{cat.structureCount} structures</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── Structures Tab ───────────────────────────────────────────────── */}
      {activeTab === 'structures' && (
        <div className="space-y-4">
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
            <h3 className="font-semibold text-gray-800 mb-4">New Fee Structure</h3>
            <div className="grid grid-cols-3 gap-4 mb-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Category *</label>
                <select value={newStructure.feeCategoryId}
                  onChange={e => setNewStructure(f => ({ ...f, feeCategoryId: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                  <option value="">Select category</option>
                  {(categories as any[]).map((c: any) => (
                    <option key={c.id} value={c.id}>{c.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Term *</label>
                <select value={newStructure.termId}
                  onChange={e => setNewStructure(f => ({ ...f, termId: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                  <option value="">Select term</option>
                  {allTerms.map((t: any) => (
                    <option key={t.id} value={t.id}>{t.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Class (optional)</label>
                <select value={newStructure.classId}
                  onChange={e => setNewStructure(f => ({ ...f, classId: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                  <option value="">All Classes</option>
                  {(classes as any[]).map((c: any) => (
                    <option key={c.id} value={c.id}>{c.displayName}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Amount *</label>
                <input type="number" value={newStructure.amount}
                  onChange={e => setNewStructure(f => ({ ...f, amount: e.target.value }))}
                  placeholder="0.00"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Due Date *</label>
                <input type="date" value={newStructure.dueDate}
                  onChange={e => setNewStructure(f => ({ ...f, dueDate: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
            </div>
            <button onClick={() => createStructureMutation.mutate()}
              disabled={createStructureMutation.isPending}
              className="px-4 py-2 bg-blue-700 text-white text-sm font-semibold rounded-lg hover:bg-blue-800 disabled:opacity-50 transition">
              {createStructureMutation.isPending ? 'Creating...' : 'Create Structure'}
            </button>
          </div>

          <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="text-left px-4 py-3 font-semibold text-gray-600">Category</th>
                  <th className="text-left px-4 py-3 font-semibold text-gray-600">Term</th>
                  <th className="text-left px-4 py-3 font-semibold text-gray-600">Class</th>
                  <th className="text-right px-4 py-3 font-semibold text-gray-600">Amount</th>
                  <th className="text-left px-4 py-3 font-semibold text-gray-600">Due Date</th>
                  <th className="text-left px-4 py-3 font-semibold text-gray-600">Status</th>
                  <th className="text-left px-4 py-3 font-semibold text-gray-600">Invoices</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {(structures as any[]).map((s: any) => (
                  <tr key={s.id} className="hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium text-gray-900">{s.feeCategoryName}</td>
                    <td className="px-4 py-3 text-gray-600">{s.termName}</td>
                    <td className="px-4 py-3 text-gray-600">{s.className ?? 'All Classes'}</td>
                    <td className="px-4 py-3 text-right font-medium text-blue-700">${parseFloat(s.amount).toLocaleString()}</td>
                    <td className="px-4 py-3 text-gray-600">{format(new Date(s.dueDate), 'MMM d, yyyy')}</td>
                    <td className="px-4 py-3">
                      <span className={`px-2 py-1 text-xs rounded-full ${s.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-600'}`}>
                        {s.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-gray-600">{s.invoiceCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Payment Modal */}
      {showPaymentModal && selectedInvoice && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
            <div className="flex items-center justify-between px-6 py-4 border-b">
              <h2 className="text-lg font-bold text-gray-900">Record Payment</h2>
              <button onClick={() => setShowPaymentModal(false)}
                className="text-gray-400 hover:text-gray-600 text-2xl leading-none">&times;</button>
            </div>

            <div className="p-6 space-y-4">
              <div className="bg-blue-50 rounded-lg p-3 text-sm">
                <p className="font-medium text-gray-800">{selectedInvoice.studentName}</p>
                <p className="text-gray-500">{selectedInvoice.feeCategoryName} · {selectedInvoice.invoiceNumber}</p>
                <p className="text-blue-700 font-bold mt-1">Balance: ${selectedInvoice.balance.toLocaleString()}</p>
              </div>

              {[
                ['Amount *', 'amount', 'number'],
                ['Payment Date *', 'paymentDate', 'date'],
                ['Reference Number', 'referenceNumber', 'text'],
              ].map(([label, field, type]) => (
                <div key={field}>
                  <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
                  <input type={type}
                    value={(paymentForm as any)[field]}
                    onChange={e => setPaymentForm(f => ({ ...f, [field]: e.target.value }))}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
              ))}

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Payment Mode *</label>
                <select value={paymentForm.paymentMode}
                  onChange={e => setPaymentForm(f => ({ ...f, paymentMode: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                  {['Cash', 'BankTransfer', 'Card', 'Online', 'Cheque'].map(m => (
                    <option key={m}>{m}</option>
                  ))}
                </select>
              </div>
            </div>

            <div className="flex justify-end gap-3 px-6 py-4 border-t bg-gray-50 rounded-b-2xl">
              <button onClick={() => setShowPaymentModal(false)}
                className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition">Cancel</button>
              <button onClick={() => paymentMutation.mutate()}
                disabled={paymentMutation.isPending}
                className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition disabled:opacity-50">
                {paymentMutation.isPending ? 'Recording...' : 'Record Payment'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}