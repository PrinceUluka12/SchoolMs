import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { studentApi } from './studentApi';
import type { StudentFilter } from './types';
import CreateStudentModal from './CreateStudentModal';

const STATUS_COLORS: Record<string, string> = {
  Active: 'bg-green-100 text-green-800',
  Inactive: 'bg-gray-100 text-gray-700',
  Transferred: 'bg-blue-100 text-blue-800',
  Graduated: 'bg-purple-100 text-purple-800',
  Withdrawn: 'bg-red-100 text-red-800',
};

export default function StudentsPage() {
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<StudentFilter>({ page: 1, pageSize: 20 });
  const [search, setSearch] = useState('');
  const [showCreate, setShowCreate] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ['students', filter],
    queryFn: () => studentApi.getAll(filter),
  });

  const deleteMutation = useMutation({
    mutationFn: studentApi.delete,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['students'] }),
  });

  const bulkMutation = useMutation({
    mutationFn: (file: File) => studentApi.bulkImport(file),
    onSuccess: (result) => {
      alert(`Import complete. Succeeded: ${result.succeeded}, Failed: ${result.failed}`);
      queryClient.invalidateQueries({ queryKey: ['students'] });
    },
  });

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setFilter(f => ({ ...f, search, page: 1 }));
  };

  return (
    <div className="p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Students</h1>
          <p className="text-sm text-gray-500 mt-1">
            {data?.totalCount ?? 0} total students
          </p>
        </div>
        <div className="flex gap-3">
          {/* Bulk Import */}
          <label className="cursor-pointer px-4 py-2 bg-gray-100 hover:bg-gray-200 text-gray-700 text-sm font-medium rounded-lg transition">
            Import CSV
            <input type="file" accept=".csv" className="hidden"
              onChange={e => {
                const f = e.target.files?.[0];
                if (f) bulkMutation.mutate(f);
              }} />
          </label>
          <button
            onClick={() => setShowCreate(true)}
            className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition">
            + Add Student
          </button>
        </div>
      </div>

      {/* Search & Filters */}
      <form onSubmit={handleSearch} className="flex gap-3 mb-6">
        <input
          type="text" placeholder="Search by name or student number..."
          value={search} onChange={e => setSearch(e.target.value)}
          className="flex-1 border border-gray-300 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
        <select
          onChange={e => setFilter(f => ({ ...f, status: e.target.value || undefined, page: 1 }))}
          className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none">
          <option value="">All Status</option>
          <option>Active</option>
          <option>Inactive</option>
          <option>Transferred</option>
          <option>Graduated</option>
          <option>Withdrawn</option>
        </select>
        <select
          onChange={e => setFilter(f => ({ ...f, gender: e.target.value || undefined, page: 1 }))}
          className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none">
          <option value="">All Gender</option>
          <option>Male</option>
          <option>Female</option>
          <option>Other</option>
        </select>
        <button type="submit"
          className="px-4 py-2 bg-blue-700 text-white text-sm font-medium rounded-lg hover:bg-blue-800 transition">
          Search
        </button>
      </form>

      {/* Table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Student</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Number</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Class</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Guardian</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Status</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isLoading ? (
              Array.from({ length: 8 }).map((_, i) => (
                <tr key={i} className="animate-pulse">
                  {Array.from({ length: 6 }).map((_, j) => (
                    <td key={j} className="px-4 py-3">
                      <div className="h-4 bg-gray-200 rounded w-3/4" />
                    </td>
                  ))}
                </tr>
              ))
            ) : data?.items.length === 0 ? (
              <tr>
                <td colSpan={6} className="text-center py-12 text-gray-400">
                  No students found. Add your first student to get started.
                </td>
              </tr>
            ) : (
              data?.items.map(student => (
                <tr key={student.id} className="hover:bg-gray-50 transition">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      {student.photoUrl ? (
                        <img src={`http://localhost:5000/uploads/${student.photoUrl}`}
                          alt={student.fullName}
                          className="w-9 h-9 rounded-full object-cover border border-gray-200" />
                      ) : (
                        <div className="w-9 h-9 rounded-full bg-blue-100 flex items-center justify-center text-blue-700 font-semibold text-sm">
                          {student.firstName[0]}{student.lastName[0]}
                        </div>
                      )}
                      <div>
                        <p className="font-medium text-gray-900">{student.fullName}</p>
                        <p className="text-xs text-gray-400">{student.gender} · Age {student.age}</p>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-gray-700 font-mono text-xs">{student.studentNumber}</td>
                  <td className="px-4 py-3 text-gray-600">{student.className ?? '—'}</td>
                  <td className="px-4 py-3">
                    {student.guardian ? (
                      <div>
                        <p className="text-gray-700">{student.guardian.fullName}</p>
                        <p className="text-xs text-gray-400">{student.guardian.phoneNumber}</p>
                      </div>
                    ) : '—'}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-1 rounded-full text-xs font-medium ${STATUS_COLORS[student.status] ?? 'bg-gray-100'}`}>
                      {student.status}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button className="text-xs text-blue-600 hover:underline">View</button>
                      <button
                        onClick={() => { if (confirm('Delete this student?')) deleteMutation.mutate(student.id); }}
                        className="text-xs text-red-500 hover:underline">
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>

        {/* Pagination */}
        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-gray-200 bg-gray-50">
            <p className="text-sm text-gray-500">
              Page {data.page} of {data.totalPages} · {data.totalCount} students
            </p>
            <div className="flex gap-2">
              <button
                disabled={!data.hasPrevious}
                onClick={() => setFilter(f => ({ ...f, page: f.page - 1 }))}
                className="px-3 py-1 text-sm border rounded-lg disabled:opacity-40 hover:bg-gray-100 transition">
                Previous
              </button>
              <button
                disabled={!data.hasNext}
                onClick={() => setFilter(f => ({ ...f, page: f.page + 1 }))}
                className="px-3 py-1 text-sm border rounded-lg disabled:opacity-40 hover:bg-gray-100 transition">
                Next
              </button>
            </div>
          </div>
        )}
      </div>

      {showCreate && <CreateStudentModal onClose={() => setShowCreate(false)} />}
    </div>
  );
}