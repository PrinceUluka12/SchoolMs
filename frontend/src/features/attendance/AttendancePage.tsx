import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { attendanceApi, type AttendanceStudentRow } from './attendanceApi';
import { academicApi } from '../academic/academicApi';
import { format } from 'date-fns';

const STATUS_OPTIONS = ['Present', 'Absent', 'Late', 'Excused'];
const STATUS_COLORS: Record<string, string> = {
  Present: 'bg-green-100 text-green-800 border-green-300',
  Absent:  'bg-red-100 text-red-800 border-red-300',
  Late:    'bg-yellow-100 text-yellow-800 border-yellow-300',
  Excused: 'bg-blue-100 text-blue-800 border-blue-300',
};

export default function AttendancePage() {
  const queryClient = useQueryClient();
  const [selectedClass, setSelectedClass] = useState('');
  const [selectedDate, setSelectedDate] = useState(format(new Date(), 'yyyy-MM-dd'));
  const [statuses, setStatuses] = useState<Record<string, string>>({});
  const [saved, setSaved] = useState(false);

  const { data: classes = [] } = useQuery({
    queryKey: ['classes'],
    queryFn: () => academicApi.getClasses(),
  });

  const { data: register, isLoading } = useQuery({
    queryKey: ['attendance-register', selectedClass, selectedDate],
    queryFn: () => attendanceApi.getRegister(selectedClass, selectedDate),
    enabled: !!selectedClass,
  });

  useEffect(() => {
    if (!register) return;
    const initial: Record<string, string> = {};
    register.students.forEach(s => {
      if (s.status) initial[s.studentId] = s.status;
    });
    setStatuses(initial);
    setSaved(false);
  }, [register]);

  const markMutation = useMutation({
    mutationFn: () => attendanceApi.markBulk({
      classId: selectedClass,
      date: selectedDate,
      entries: (register?.students ?? []).map((s: AttendanceStudentRow) => ({
        studentId: s.studentId,
        status: statuses[s.studentId] ?? 'Present',
      }))
    }),
    onSuccess: () => {
      setSaved(true);
      queryClient.invalidateQueries({ queryKey: ['attendance-register'] });
    }
  });

  const setAll = (status: string) => {
    const all: Record<string, string> = {};
    register?.students.forEach((s: AttendanceStudentRow) => { all[s.studentId] = status; });
    setStatuses(all);
  };

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Attendance Register</h1>
          <p className="text-sm text-gray-500 mt-1">Mark daily class attendance</p>
        </div>
      </div>

      {/* Controls */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5 mb-6">
        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Class *</label>
            <select value={selectedClass} onChange={e => setSelectedClass(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="">Select a class</option>
              {(classes as any[]).map((c: any) => (
                <option key={c.id} value={c.id}>{c.displayName}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Date *</label>
            <input type="date" value={selectedDate}
              onChange={e => setSelectedDate(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div className="flex items-end">
            {register && (
              <div className="flex gap-2 flex-wrap">
                {STATUS_OPTIONS.map(s => (
                  <button key={s} onClick={() => setAll(s)}
                    className={`px-3 py-1.5 text-xs font-medium rounded-lg border ${STATUS_COLORS[s]} hover:opacity-80 transition`}>
                    All {s}
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Register Table */}
      {!selectedClass ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
          Select a class to load the attendance register.
        </div>
      ) : isLoading ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400 animate-pulse">
          Loading register...
        </div>
      ) : register ? (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="flex items-center justify-between px-5 py-3 border-b border-gray-200 bg-gray-50">
            <div>
              <p className="font-semibold text-gray-800">{register.className}</p>
              <p className="text-sm text-gray-500">
                {format(new Date(selectedDate), 'EEEE, MMMM d, yyyy')} ·
                {register.students.length} students ·
                {register.isMarked
                  ? <span className="text-green-600 font-medium"> ✓ Already marked</span>
                  : <span className="text-amber-600 font-medium"> Not yet marked</span>
                }
              </p>
            </div>
            <button
              onClick={() => markMutation.mutate()}
              disabled={markMutation.isPending || register.students.length === 0}
              className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition disabled:opacity-50">
              {markMutation.isPending ? 'Saving...' : saved ? '✓ Saved' : 'Save Attendance'}
            </button>
          </div>

          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 font-semibold text-gray-600 w-8">#</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Student</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Number</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {register.students.map((student: AttendanceStudentRow, idx: number) => (
                <tr key={student.studentId}
                  className={`hover:bg-gray-50 transition ${
                    statuses[student.studentId] === 'Absent' ? 'bg-red-50' : ''
                  }`}>
                  <td className="px-4 py-3 text-gray-400 text-xs">{idx + 1}</td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      {student.photoUrl ? (
                        <img src={`http://localhost:5000/uploads/${student.photoUrl}`}
                          className="w-8 h-8 rounded-full object-cover border border-gray-200" alt="" />
                      ) : (
                        <div className="w-8 h-8 rounded-full bg-blue-100 flex items-center
                          justify-center text-blue-700 font-bold text-xs shrink-0">
                          {student.studentName.split(' ').map(n => n[0]).join('').slice(0, 2)}
                        </div>
                      )}
                      <span className="font-medium text-gray-900">{student.studentName}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3 font-mono text-xs text-gray-600">{student.studentNumber}</td>
                  <td className="px-4 py-3">
                    <div className="flex gap-2">
                      {STATUS_OPTIONS.map(s => (
                        <button key={s}
                          onClick={() => setStatuses(prev => ({ ...prev, [student.studentId]: s }))}
                          className={`px-3 py-1 text-xs font-medium rounded-lg border transition
                            ${statuses[student.studentId] === s
                              ? STATUS_COLORS[s] + ' ring-2 ring-offset-1 ring-current'
                              : 'bg-white text-gray-500 border-gray-200 hover:bg-gray-50'}`}>
                          {s}
                        </button>
                      ))}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  );
}