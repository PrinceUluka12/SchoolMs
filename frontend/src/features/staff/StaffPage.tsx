import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { staffApi } from './staffApi';
import type { StaffFilter } from './types';
import CreateStaffModal from './CreateStaffModal';

const ROLE_COLORS: Record<string, string> = {
  Admin: 'bg-purple-100 text-purple-800',
  Teacher: 'bg-blue-100 text-blue-800',
  Finance: 'bg-green-100 text-green-800',
  Librarian: 'bg-yellow-100 text-yellow-800',
  Transport: 'bg-orange-100 text-orange-800',
  Warden: 'bg-pink-100 text-pink-800',
};

const STATUS_COLORS: Record<string, string> = {
  Active: 'bg-green-100 text-green-800',
  Inactive: 'bg-gray-100 text-gray-700',
  Terminated: 'bg-red-100 text-red-800',
  OnLeave: 'bg-yellow-100 text-yellow-800',
};

export default function StaffPage() {
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<StaffFilter>({ page: 1, pageSize: 20 });
  const [search, setSearch] = useState('');
  const [showCreate, setShowCreate] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ['staff', filter],
    queryFn: () => staffApi.getAll(filter),
  });

  const deleteMutation = useMutation({
    mutationFn: staffApi.delete,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['staff'] }),
  });

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setFilter(f => ({ ...f, search, page: 1 }));
  };

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Staff</h1>
          <p className="text-sm text-gray-500 mt-1">{data?.totalCount ?? 0} total staff members</p>
        </div>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition">
          + Add Staff
        </button>
      </div>

      <form onSubmit={handleSearch} className="flex gap-3 mb-6">
        <input type="text" placeholder="Search by name, number, or email..."
          value={search} onChange={e => setSearch(e.target.value)}
          className="flex-1 border border-gray-300 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
        <select onChange={e => setFilter(f => ({ ...f, role: e.target.value || undefined, page: 1 }))}
          className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none">
          <option value="">All Roles</option>
          {['Admin','Teacher','Finance','Librarian','Transport','Warden'].map(r =>
            <option key={r}>{r}</option>)}
        </select>
        <select onChange={e => setFilter(f => ({ ...f, status: e.target.value || undefined, page: 1 }))}
          className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none">
          <option value="">All Status</option>
          {['Active','Inactive','Terminated','OnLeave'].map(s => <option key={s}>{s}</option>)}
        </select>
        <button type="submit"
          className="px-4 py-2 bg-blue-700 text-white text-sm font-medium rounded-lg hover:bg-blue-800 transition">
          Search
        </button>
      </form>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Staff Member</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Number</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Role</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Department</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Contract</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Status</th>
              <th className="text-left px-4 py-3 font-semibold text-gray-600">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isLoading ? (
              Array.from({ length: 6 }).map((_, i) => (
                <tr key={i} className="animate-pulse">
                  {Array.from({ length: 7 }).map((_, j) => (
                    <td key={j} className="px-4 py-3">
                      <div className="h-4 bg-gray-200 rounded w-3/4" />
                    </td>
                  ))}
                </tr>
              ))
            ) : data?.items.length === 0 ? (
              <tr>
                <td colSpan={7} className="text-center py-12 text-gray-400">
                  No staff found. Add your first staff member to get started.
                </td>
              </tr>
            ) : (
              data?.items.map(staff => (
                <tr key={staff.id} className="hover:bg-gray-50 transition">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      {staff.photoUrl ? (
                        <img src={`http://localhost:5000/uploads/${staff.photoUrl}`}
                          alt={staff.fullName}
                          className="w-9 h-9 rounded-full object-cover border border-gray-200" />
                      ) : (
                        <div className="w-9 h-9 rounded-full bg-purple-100 flex items-center justify-center text-purple-700 font-semibold text-sm">
                          {staff.firstName[0]}{staff.lastName[0]}
                        </div>
                      )}
                      <div>
                        <p className="font-medium text-gray-900">{staff.fullName}</p>
                        <p className="text-xs text-gray-400">{staff.email}</p>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3 font-mono text-xs text-gray-700">{staff.staffNumber}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-1 rounded-full text-xs font-medium ${ROLE_COLORS[staff.role] ?? 'bg-gray-100'}`}>
                      {staff.role}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-600">{staff.departmentName ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-600">{staff.contractType}</td>
                  <td className="px-4 py-3">
                    <span className={`px-2 py-1 rounded-full text-xs font-medium ${STATUS_COLORS[staff.status] ?? 'bg-gray-100'}`}>
                      {staff.status}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      <button className="text-xs text-blue-600 hover:underline">View</button>
                      <button
                        onClick={() => { if (confirm('Delete this staff member?')) deleteMutation.mutate(staff.id); }}
                        className="text-xs text-red-500 hover:underline">Delete</button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>

        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-between px-4 py-3 border-t border-gray-200 bg-gray-50">
            <p className="text-sm text-gray-500">Page {data.page} of {data.totalPages}</p>
            <div className="flex gap-2">
              <button disabled={!data.hasPrevious}
                onClick={() => setFilter(f => ({ ...f, page: f.page - 1 }))}
                className="px-3 py-1 text-sm border rounded-lg disabled:opacity-40 hover:bg-gray-100 transition">
                Previous
              </button>
              <button disabled={!data.hasNext}
                onClick={() => setFilter(f => ({ ...f, page: f.page + 1 }))}
                className="px-3 py-1 text-sm border rounded-lg disabled:opacity-40 hover:bg-gray-100 transition">
                Next
              </button>
            </div>
          </div>
        )}
      </div>

      {showCreate && <CreateStaffModal onClose={() => setShowCreate(false)} />}
    </div>
  );
}