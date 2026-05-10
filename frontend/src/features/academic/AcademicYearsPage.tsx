import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { academicApi, type AcademicYear } from './academicApi';
import { format } from 'date-fns';

export default function AcademicYearsPage() {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [expandedYear, setExpandedYear] = useState<string | null>(null);
  const [newYear, setNewYear] = useState({ name: '', startDate: '', endDate: '' });
  const [newTerm, setNewTerm] = useState({ name: '', termNumber: 1, startDate: '', endDate: '' });
  const [error, setError] = useState('');

  const { data: years = [], isLoading } = useQuery({
    queryKey: ['academic-years'],
    queryFn: academicApi.getAcademicYears,
  });

  const createYearMutation = useMutation({
    mutationFn: academicApi.createAcademicYear,
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['academic-years'] }); setShowCreate(false); setNewYear({ name: '', startDate: '', endDate: '' }); },
    onError: (e: any) => setError(e.response?.data?.message ?? 'Error creating academic year'),
  });

  const setCurrentMutation = useMutation({
    mutationFn: academicApi.setCurrentAcademicYear,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['academic-years'] }),
  });

  const deleteYearMutation = useMutation({
    mutationFn: academicApi.deleteAcademicYear,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['academic-years'] }),
    onError: (e: any) => alert(e.response?.data?.message ?? 'Cannot delete'),
  });

  const createTermMutation = useMutation({
    mutationFn: (payload: unknown) => academicApi.createTerm(payload),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['academic-years'] }),
    onError: (e: any) => alert(e.response?.data?.message ?? 'Error creating term'),
  });

  const setCurrentTermMutation = useMutation({
    mutationFn: academicApi.setCurrentTerm,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['academic-years'] }),
  });

  const deleteTermMutation = useMutation({
    mutationFn: academicApi.deleteTerm,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['academic-years'] }),
    onError: (e: any) => alert(e.response?.data?.message ?? 'Cannot delete term'),
  });

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Academic Years</h1>
          <p className="text-sm text-gray-500 mt-1">Manage academic years and terms</p>
        </div>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition">
          + New Academic Year
        </button>
      </div>

      {/* Create Form */}
      {showCreate && (
        <div className="bg-white border border-gray-200 rounded-xl p-5 mb-6 shadow-sm">
          <h3 className="font-semibold text-gray-800 mb-4">New Academic Year</h3>
          {error && <div className="bg-red-50 text-red-700 rounded-lg px-4 py-2 text-sm mb-3">{error}</div>}
          <div className="grid grid-cols-3 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Name *</label>
              <input placeholder="e.g. 2025/2026" value={newYear.name}
                onChange={e => setNewYear(f => ({ ...f, name: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Start Date *</label>
              <input type="date" value={newYear.startDate}
                onChange={e => setNewYear(f => ({ ...f, startDate: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">End Date *</label>
              <input type="date" value={newYear.endDate}
                onChange={e => setNewYear(f => ({ ...f, endDate: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>
          <div className="flex gap-3">
            <button onClick={() => createYearMutation.mutate(newYear)}
              disabled={createYearMutation.isPending}
              className="px-4 py-2 bg-blue-700 text-white text-sm font-semibold rounded-lg hover:bg-blue-800 disabled:opacity-50 transition">
              {createYearMutation.isPending ? 'Creating...' : 'Create'}
            </button>
            <button onClick={() => { setShowCreate(false); setError(''); }}
              className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition">Cancel</button>
          </div>
        </div>
      )}

      {/* Years List */}
      {isLoading ? (
        <div className="space-y-3">
          {[1, 2].map(i => <div key={i} className="bg-white rounded-xl h-20 animate-pulse border border-gray-200" />)}
        </div>
      ) : years.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
          No academic years created yet.
        </div>
      ) : (
        <div className="space-y-3">
          {years.map((year: AcademicYear) => (
            <div key={year.id} className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
              {/* Year Header */}
              <div className="flex items-center justify-between px-5 py-4">
                <div className="flex items-center gap-4">
                  <button onClick={() => setExpandedYear(expandedYear === year.id ? null : year.id)}
                    className="text-gray-400 hover:text-gray-600 text-lg transition">
                    {expandedYear === year.id ? '▾' : '▸'}
                  </button>
                  <div>
                    <div className="flex items-center gap-2">
                      <p className="font-semibold text-gray-900">{year.name}</p>
                      {year.isCurrent && (
                        <span className="px-2 py-0.5 bg-green-100 text-green-800 text-xs font-medium rounded-full">
                          Current
                        </span>
                      )}
                      <span className={`px-2 py-0.5 text-xs font-medium rounded-full
                        ${year.status === 'Active' ? 'bg-blue-100 text-blue-800' :
                          year.status === 'Closed' ? 'bg-gray-100 text-gray-600' :
                          'bg-yellow-100 text-yellow-800'}`}>
                        {year.status}
                      </span>
                    </div>
                    <p className="text-xs text-gray-400 mt-0.5">
                      {format(new Date(year.startDate), 'MMM d, yyyy')} — {format(new Date(year.endDate), 'MMM d, yyyy')}
                      · {year.termCount} terms · {year.classCount} classes
                    </p>
                  </div>
                </div>
                <div className="flex gap-2">
                  {!year.isCurrent && (
                    <button onClick={() => setCurrentMutation.mutate(year.id)}
                      className="px-3 py-1.5 text-xs font-medium border border-blue-300 text-blue-700 rounded-lg hover:bg-blue-50 transition">
                      Set Current
                    </button>
                  )}
                  <button onClick={() => { if (confirm('Delete this academic year?')) deleteYearMutation.mutate(year.id); }}
                    className="px-3 py-1.5 text-xs font-medium border border-red-200 text-red-600 rounded-lg hover:bg-red-50 transition">
                    Delete
                  </button>
                </div>
              </div>

              {/* Terms Expansion */}
              {expandedYear === year.id && (
                <div className="border-t border-gray-100 bg-gray-50 px-5 py-4">
                  <div className="flex items-center justify-between mb-3">
                    <h4 className="text-sm font-semibold text-gray-700">Terms</h4>
                  </div>

                  {/* Existing Terms */}
                  {year.terms.length === 0 ? (
                    <p className="text-sm text-gray-400 mb-4">No terms added yet.</p>
                  ) : (
                    <div className="space-y-2 mb-4">
                      {year.terms.map(term => (
                        <div key={term.id}
                          className="flex items-center justify-between bg-white rounded-lg px-4 py-2.5 border border-gray-200">
                          <div className="flex items-center gap-3">
                            <span className="w-6 h-6 rounded-full bg-blue-100 text-blue-700 text-xs font-bold flex items-center justify-center">
                              {term.termNumber}
                            </span>
                            <div>
                              <p className="text-sm font-medium text-gray-800">{term.name}</p>
                              <p className="text-xs text-gray-400">
                                {format(new Date(term.startDate), 'MMM d')} — {format(new Date(term.endDate), 'MMM d, yyyy')}
                              </p>
                            </div>
                            {term.isCurrent && (
                              <span className="px-2 py-0.5 bg-green-100 text-green-800 text-xs font-medium rounded-full">
                                Current
                              </span>
                            )}
                          </div>
                          <div className="flex gap-2">
                            {!term.isCurrent && (
                              <button onClick={() => setCurrentTermMutation.mutate(term.id)}
                                className="text-xs text-blue-600 hover:underline">
                                Set Current
                              </button>
                            )}
                            <button onClick={() => { if (confirm('Delete this term?')) deleteTermMutation.mutate(term.id); }}
                              className="text-xs text-red-500 hover:underline">Delete</button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}

                  {/* Add Term Form */}
                  <div className="bg-white rounded-lg border border-dashed border-gray-300 p-4">
                    <p className="text-xs font-semibold text-gray-600 mb-3 uppercase tracking-wide">Add Term</p>
                    <div className="grid grid-cols-4 gap-3">
                      <div>
                        <label className="block text-xs text-gray-600 mb-1">Name *</label>
                        <input placeholder="e.g. First Term"
                          value={newTerm.name}
                          onChange={e => setNewTerm(f => ({ ...f, name: e.target.value }))}
                          className="w-full border border-gray-300 rounded px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                      </div>
                      <div>
                        <label className="block text-xs text-gray-600 mb-1">Term # *</label>
                        <input type="number" min={1} max={3}
                          value={newTerm.termNumber}
                          onChange={e => setNewTerm(f => ({ ...f, termNumber: parseInt(e.target.value) }))}
                          className="w-full border border-gray-300 rounded px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                      </div>
                      <div>
                        <label className="block text-xs text-gray-600 mb-1">Start Date *</label>
                        <input type="date" value={newTerm.startDate}
                          onChange={e => setNewTerm(f => ({ ...f, startDate: e.target.value }))}
                          className="w-full border border-gray-300 rounded px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                      </div>
                      <div>
                        <label className="block text-xs text-gray-600 mb-1">End Date *</label>
                        <input type="date" value={newTerm.endDate}
                          onChange={e => setNewTerm(f => ({ ...f, endDate: e.target.value }))}
                          className="w-full border border-gray-300 rounded px-2 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                      </div>
                    </div>
                    <button
                      onClick={() => createTermMutation.mutate({ ...newTerm, academicYearId: year.id })}
                      disabled={createTermMutation.isPending}
                      className="mt-3 px-4 py-1.5 bg-blue-700 text-white text-sm font-medium rounded-lg hover:bg-blue-800 disabled:opacity-50 transition">
                      {createTermMutation.isPending ? 'Adding...' : '+ Add Term'}
                    </button>
                  </div>
                </div>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}