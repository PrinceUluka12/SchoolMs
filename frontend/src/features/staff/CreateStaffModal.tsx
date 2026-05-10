import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { staffApi } from './staffApi';

interface Props { onClose: () => void; }

export default function CreateStaffModal({ onClose }: Props) {
  const queryClient = useQueryClient();
  const [form, setForm] = useState({
    firstName: '', lastName: '', email: '', phoneNumber: '',
    gender: 'Male', dateOfBirth: '', role: 'Teacher',
    contractType: 'FullTime', joinDate: '', address: '', qualifications: '',
  });
  const [error, setError] = useState('');

  const mutation = useMutation({
    mutationFn: () => staffApi.create({
      ...form,
      dateOfBirth: form.dateOfBirth,
      joinDate: form.joinDate,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['staff'] });
      onClose();
    },
    onError: (err: any) => setError(err.response?.data?.message ?? 'Failed to create staff member.'),
  });

  const set = (field: string) =>
    (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) =>
      setForm(f => ({ ...f, [field]: e.target.value }));

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between px-6 py-4 border-b">
          <h2 className="text-lg font-bold text-gray-900">Add Staff Member</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-2xl leading-none">&times;</button>
        </div>

        <div className="p-6 space-y-4">
          {error && <div className="bg-red-50 text-red-700 rounded-lg px-4 py-3 text-sm">{error}</div>}

          <div className="grid grid-cols-2 gap-4">
            {[
              ['First Name', 'firstName', 'text'],
              ['Last Name', 'lastName', 'text'],
              ['Email', 'email', 'email'],
              ['Phone Number', 'phoneNumber', 'text'],
              ['Date of Birth', 'dateOfBirth', 'date'],
              ['Join Date', 'joinDate', 'date'],
            ].map(([label, field, type]) => (
              <div key={field}>
                <label className="block text-sm font-medium text-gray-700 mb-1">{label} *</label>
                <input type={type} value={(form as any)[field]} onChange={set(field)} required
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
            ))}

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Gender *</label>
              <select value={form.gender} onChange={set('gender')}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option>Male</option><option>Female</option><option>Other</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Role *</label>
              <select value={form.role} onChange={set('role')}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                {['Admin','Teacher','Finance','Librarian','Transport','Warden'].map(r =>
                  <option key={r}>{r}</option>)}
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Contract Type *</label>
              <select value={form.contractType} onChange={set('contractType')}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option value="FullTime">Full Time</option>
                <option value="PartTime">Part Time</option>
                <option value="Contract">Contract</option>
              </select>
            </div>

            <div className="col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">Address</label>
              <input value={form.address} onChange={set('address')}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>

            <div className="col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">Qualifications</label>
              <textarea value={form.qualifications} onChange={set('qualifications')} rows={2}
                placeholder="e.g. BSc Mathematics, PGDE"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>
        </div>

        <div className="flex justify-end gap-3 px-6 py-4 border-t bg-gray-50 rounded-b-2xl">
          <button onClick={onClose}
            className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 font-medium transition">
            Cancel
          </button>
          <button onClick={() => mutation.mutate()} disabled={mutation.isPending}
            className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition disabled:opacity-50">
            {mutation.isPending ? 'Creating...' : 'Create Staff Member'}
          </button>
        </div>
      </div>
    </div>
  );
}