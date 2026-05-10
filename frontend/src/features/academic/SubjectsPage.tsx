import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { academicApi, type Subject } from './academicApi';

export default function SubjectsPage() {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [form, setForm] = useState({
    name: '', code: '', type: 'Core', creditHours: 1, description: ''
  });
  const [error, setError] = useState('');

  const { data: subjects = [], isLoading } = useQuery({
    queryKey: ['subjects'],
    queryFn: academicApi.getSubjects,
  });

  const createMutation = useMutation({
    mutationFn: academicApi.createSubject,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['subjects'] });
      setShowCreate(false);
      setForm({ name: '', code: '', type: 'Core', creditHours: 1, description: '' });
    },
    onError: (e: any) => setError(e.response?.data?.message ?? 'Error creating subject'),
  });

  const deleteMutation = useMutation({
    mutationFn: academicApi.deleteSubject,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['subjects'] }),
    onError: (e: any) => alert(e.response?.data?.message ?? 'Cannot delete'),
  });

  const TYPE_COLORS: Record<string, string> = {
    Core: 'bg-blue-100 text-blue-800',
    Elective: 'bg-purple-100 text-purple-800',
  };

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Subjects</h1>
          <p className="text-sm text-gray-500 mt-1">{subjects.length} subjects</p>
        </div>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition">
          + New Subject
        </button>
      </div>

      {showCreate && (
        <div className="bg-white border border-gray-200 rounded-xl p-5 mb-6 shadow-sm">
          <h3 className="font-semibold text-gray-800 mb-4">New Subject</h3>
          {error && <div className="bg-red-50 text-red-700 rounded-lg px-4 py-2 text-sm mb-3">{error}</div>}
          <div className="grid grid-cols-3 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Name *</label>
              <input placeholder="e.g. Mathematics" value={form.name}
                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Code *</label>
              <input placeholder="e.g. MTH101" value={form.code}
                onChange={e => setForm(f => ({ ...f, code: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Type *</label>
              <select value={form.type} onChange={e => setForm(f => ({ ...f, type: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option>Core</option>
                <option>Elective</option>
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Credit Hours</label>
              <input type="number" min={1} value={form.creditHours}
                onChange={e => setForm(f => ({ ...f, creditHours: parseInt(e.target.value) }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div className="col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
              <input placeholder="Optional" value={form.description}
                onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>
          <div className="flex gap-3">
            <button onClick={() => createMutation.mutate(form)} disabled={createMutation.isPending}
              className="px-4 py-2 bg-blue-700 text-white text-sm font-semibold rounded-lg hover:bg-blue-800 disabled:opacity-50 transition">
              {createMutation.isPending ? 'Creating...' : 'Create Subject'}
            </button>
            <button onClick={() => { setShowCreate(false); setError(''); }}
              className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition">Cancel</button>
          </div>
        </div>
      )}

      {isLoading ? (
        <div className="space-y-2">
          {[1, 2, 3].map(i => <div key={i} className="bg-white rounded-xl h-16 animate-pulse border border-gray-200" />)}
        </div>
      ) : subjects.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
          No subjects created yet.
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Subject</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Code</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Type</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Credit Hours</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Department</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Classes</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {(subjects as Subject[]).map(subject => (
                <tr key={subject.id} className="hover:bg-gray-50 transition">
                  <td className="px-4 py-3">
                    <p className="font-medium text-gray-900">{subject.name}</p>
                    {subject.description && (
                      <p className="text-xs text-gray-400 truncate max-w-xs">{subject.description}</p>
                    )}
                  </td>
                  <td className="px-4 py-3 font-mono text-xs text-gray-700">{subject.code}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-1 rounded-full text-xs font-medium ${TYPE_COLORS[subject.type] ?? 'bg-gray-100'}`}>
                      {subject.type}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-600">{subject.creditHours}</td>
                  <td className="px-4 py-3 text-gray-600">{subject.departmentName ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-600">{subject.classCount}</td>
                  <td className="px-4 py-3">
                    <button onClick={() => { if (confirm('Delete this subject?')) deleteMutation.mutate(subject.id); }}
                      className="text-xs text-red-500 hover:underline">Delete</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}