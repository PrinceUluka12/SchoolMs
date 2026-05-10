import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { academicApi, type ClassItem, type AcademicYear } from './academicApi';

export default function ClassesPage() {
  const queryClient = useQueryClient();
  const [selectedYear, setSelectedYear] = useState<string>('');
  const [showCreate, setShowCreate] = useState(false);
  const [form, setForm] = useState({
    name: '', section: '', level: 1, capacity: 40, academicYearId: ''
  });
  const [error, setError] = useState('');

  const { data: years = [] } = useQuery({
    queryKey: ['academic-years'],
    queryFn: academicApi.getAcademicYears,
  });

  const { data: classes = [], isLoading } = useQuery({
    queryKey: ['classes', selectedYear],
    queryFn: () => academicApi.getClasses(selectedYear || undefined),
  });

  const createMutation = useMutation({
    mutationFn: academicApi.createClass,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classes'] });
      setShowCreate(false);
      setForm({ name: '', section: '', level: 1, capacity: 40, academicYearId: '' });
    },
    onError: (e: any) => setError(e.response?.data?.message ?? 'Error creating class'),
  });

  const deleteMutation = useMutation({
    mutationFn: academicApi.deleteClass,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['classes'] }),
    onError: (e: any) => alert(e.response?.data?.message ?? 'Cannot delete'),
  });

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Classes</h1>
          <p className="text-sm text-gray-500 mt-1">{classes.length} classes</p>
        </div>
        <div className="flex gap-3">
          <select value={selectedYear}
            onChange={e => setSelectedYear(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none">
            <option value="">All Academic Years</option>
            {(years as AcademicYear[]).map(y => (
              <option key={y.id} value={y.id}>{y.name}</option>
            ))}
          </select>
          <button onClick={() => setShowCreate(true)}
            className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition">
            + New Class
          </button>
        </div>
      </div>

      {showCreate && (
        <div className="bg-white border border-gray-200 rounded-xl p-5 mb-6 shadow-sm">
          <h3 className="font-semibold text-gray-800 mb-4">New Class</h3>
          {error && <div className="bg-red-50 text-red-700 rounded-lg px-4 py-2 text-sm mb-3">{error}</div>}
          <div className="grid grid-cols-3 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Name *</label>
              <input placeholder="e.g. Grade 5" value={form.name}
                onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Section</label>
              <input placeholder="e.g. A" value={form.section}
                onChange={e => setForm(f => ({ ...f, section: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Level *</label>
              <input type="number" min={1} max={12} value={form.level}
                onChange={e => setForm(f => ({ ...f, level: parseInt(e.target.value) }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Capacity</label>
              <input type="number" min={1} value={form.capacity}
                onChange={e => setForm(f => ({ ...f, capacity: parseInt(e.target.value) }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Academic Year *</label>
              <select value={form.academicYearId}
                onChange={e => setForm(f => ({ ...f, academicYearId: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option value="">Select year</option>
                {(years as AcademicYear[]).map(y => (
                  <option key={y.id} value={y.id}>{y.name}</option>
                ))}
              </select>
            </div>
          </div>
          <div className="flex gap-3">
            <button onClick={() => createMutation.mutate(form)} disabled={createMutation.isPending}
              className="px-4 py-2 bg-blue-700 text-white text-sm font-semibold rounded-lg hover:bg-blue-800 disabled:opacity-50 transition">
              {createMutation.isPending ? 'Creating...' : 'Create Class'}
            </button>
            <button onClick={() => { setShowCreate(false); setError(''); }}
              className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition">Cancel</button>
          </div>
        </div>
      )}

      {isLoading ? (
        <div className="grid grid-cols-3 gap-4">
          {[1, 2, 3, 4].map(i => <div key={i} className="bg-white rounded-xl h-32 animate-pulse border border-gray-200" />)}
        </div>
      ) : classes.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
          No classes found. Create your first class to get started.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {(classes as ClassItem[]).map(cls => (
            <div key={cls.id} className="bg-white rounded-xl border border-gray-200 shadow-sm p-5 hover:shadow-md transition">
              <div className="flex items-start justify-between mb-3">
                <div className="w-10 h-10 rounded-lg bg-green-100 flex items-center justify-center text-green-700 font-bold text-sm">
                  L{cls.level}
                </div>
                <button onClick={() => { if (confirm('Delete this class?')) deleteMutation.mutate(cls.id); }}
                  className="text-xs text-red-400 hover:text-red-600 transition">Delete</button>
              </div>
              <h3 className="font-semibold text-gray-900">{cls.displayName}</h3>
              <p className="text-xs text-gray-400 mt-0.5">{cls.academicYearName}</p>
              {cls.classTeacherName && (
                <p className="text-xs text-gray-500 mt-1">Teacher: {cls.classTeacherName}</p>
              )}
              <div className="flex gap-4 mt-3 pt-3 border-t border-gray-100">
                <div>
                  <p className="text-lg font-bold text-blue-700">{cls.studentCount}</p>
                  <p className="text-xs text-gray-400">Students</p>
                </div>
                <div>
                  <p className="text-lg font-bold text-purple-700">{cls.subjectCount}</p>
                  <p className="text-xs text-gray-400">Subjects</p>
                </div>
                <div>
                  <p className="text-lg font-bold text-gray-500">{cls.capacity}</p>
                  <p className="text-xs text-gray-400">Capacity</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}