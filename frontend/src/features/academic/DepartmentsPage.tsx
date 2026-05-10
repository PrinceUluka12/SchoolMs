import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { academicApi, type Department } from './academicApi';

export default function DepartmentsPage() {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [form, setForm] = useState({ name: '', description: '' });
  const [editId, setEditId] = useState<string | null>(null);
  const [editForm, setEditForm] = useState({ name: '', description: '' });
  const [error, setError] = useState('');

  const { data: departments = [], isLoading } = useQuery({
    queryKey: ['departments'],
    queryFn: academicApi.getDepartments,
  });

  const createMutation = useMutation({
    mutationFn: academicApi.createDepartment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['departments'] });
      setShowCreate(false);
      setForm({ name: '', description: '' });
    },
    onError: (e: any) => setError(e.response?.data?.message ?? 'Error'),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: unknown }) =>
      academicApi.updateDepartment(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['departments'] });
      setEditId(null);
    },
    onError: (e: any) => alert(e.response?.data?.message ?? 'Error updating'),
  });

  const deleteMutation = useMutation({
    mutationFn: academicApi.deleteDepartment,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['departments'] }),
    onError: (e: any) => alert(e.response?.data?.message ?? 'Cannot delete'),
  });

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Departments</h1>
          <p className="text-sm text-gray-500 mt-1">{departments.length} departments</p>
        </div>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition">
          + New Department
        </button>
      </div>

      {showCreate && (
        <div className="bg-white border border-gray-200 rounded-xl p-5 mb-6 shadow-sm">
          <h3 className="font-semibold text-gray-800 mb-4">New Department</h3>
          {error && <div className="bg-red-50 text-red-700 rounded-lg px-4 py-2 text-sm mb-3">{error}</div>}
          <div className="grid grid-cols-2 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Name *</label>
              <input value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
                placeholder="e.g. Mathematics"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
              <input value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))}
                placeholder="Optional description"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>
          <div className="flex gap-3">
            <button onClick={() => createMutation.mutate(form)} disabled={createMutation.isPending}
              className="px-4 py-2 bg-blue-700 text-white text-sm font-semibold rounded-lg hover:bg-blue-800 disabled:opacity-50 transition">
              {createMutation.isPending ? 'Creating...' : 'Create'}
            </button>
            <button onClick={() => { setShowCreate(false); setError(''); }}
              className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition">Cancel</button>
          </div>
        </div>
      )}

      {isLoading ? (
        <div className="grid grid-cols-3 gap-4">
          {[1, 2, 3].map(i => <div key={i} className="bg-white rounded-xl h-32 animate-pulse border border-gray-200" />)}
        </div>
      ) : departments.length === 0 ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
          No departments created yet.
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {departments.map((dept: Department) => (
            <div key={dept.id} className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
              {editId === dept.id ? (
                <div className="space-y-3">
                  <input value={editForm.name}
                    onChange={e => setEditForm(f => ({ ...f, name: e.target.value }))}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                  <input value={editForm.description}
                    onChange={e => setEditForm(f => ({ ...f, description: e.target.value }))}
                    placeholder="Description"
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                  <div className="flex gap-2">
                    <button onClick={() => updateMutation.mutate({ id: dept.id, data: editForm })}
                      className="px-3 py-1.5 bg-blue-700 text-white text-xs font-medium rounded-lg hover:bg-blue-800 transition">
                      Save
                    </button>
                    <button onClick={() => setEditId(null)}
                      className="px-3 py-1.5 text-xs text-gray-500 hover:text-gray-700 transition">
                      Cancel
                    </button>
                  </div>
                </div>
              ) : (
                <>
                  <div className="flex items-start justify-between mb-3">
                    <div className="w-10 h-10 rounded-lg bg-blue-100 flex items-center justify-center text-blue-700 font-bold text-sm">
                      {dept.name.slice(0, 2).toUpperCase()}
                    </div>
                    <div className="flex gap-2">
                      <button onClick={() => { setEditId(dept.id); setEditForm({ name: dept.name, description: dept.description ?? '' }); }}
                        className="text-xs text-blue-600 hover:underline">Edit</button>
                      <button onClick={() => { if (confirm('Delete this department?')) deleteMutation.mutate(dept.id); }}
                        className="text-xs text-red-500 hover:underline">Delete</button>
                    </div>
                  </div>
                  <h3 className="font-semibold text-gray-900">{dept.name}</h3>
                  {dept.description && <p className="text-sm text-gray-500 mt-1">{dept.description}</p>}
                  <div className="flex gap-4 mt-3">
                    <div className="text-center">
                      <p className="text-lg font-bold text-blue-700">{dept.staffCount}</p>
                      <p className="text-xs text-gray-400">Staff</p>
                    </div>
                    <div className="text-center">
                      <p className="text-lg font-bold text-purple-700">{dept.subjectCount}</p>
                      <p className="text-xs text-gray-400">Subjects</p>
                    </div>
                  </div>
                  {dept.headOfDepartmentName && (
                    <p className="text-xs text-gray-400 mt-2">HoD: {dept.headOfDepartmentName}</p>
                  )}
                </>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}