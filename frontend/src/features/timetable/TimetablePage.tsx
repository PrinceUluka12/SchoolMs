import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { timetableApi, type TimetableSlot } from './timetableApi';
import { academicApi } from '../academic/academicApi';

const DAYS = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];
const PERIODS = [1, 2, 3, 4, 5, 6, 7, 8];

const SUBJECT_COLORS = [
  'bg-blue-100 text-blue-800 border-blue-200',
  'bg-purple-100 text-purple-800 border-purple-200',
  'bg-green-100 text-green-800 border-green-200',
  'bg-amber-100 text-amber-800 border-amber-200',
  'bg-pink-100 text-pink-800 border-pink-200',
  'bg-teal-100 text-teal-800 border-teal-200',
];

export default function TimetablePage() {
  const queryClient = useQueryClient();
  const [selectedClass, setSelectedClass] = useState('');
  const [selectedYear, setSelectedYear] = useState('');
  const [showAddSlot, setShowAddSlot] = useState(false);
  const [newSlot, setNewSlot] = useState({
    dayOfWeek: 'Monday', period: 1, subjectId: '',
    teacherId: '', startTime: '08:00', endTime: '08:45', roomNumber: ''
  });

  const { data: classes = [] } = useQuery({
    queryKey: ['classes'],
    queryFn: () => academicApi.getClasses(),
  });

  const { data: years = [] } = useQuery({
    queryKey: ['academic-years'],
    queryFn: academicApi.getAcademicYears,
  });

  const { data: timetable, isLoading } = useQuery({
    queryKey: ['timetable', selectedClass, selectedYear],
    queryFn: () => timetableApi.getClassTimetable(selectedClass, selectedYear),
    enabled: !!(selectedClass && selectedYear),
  });

  const selectedClassObj = (classes as any[]).find((c: any) => c.id === selectedClass);

  const createMutation = useMutation({
    mutationFn: () => timetableApi.createSlot({
      ...newSlot,
      classId: selectedClass,
      academicYearId: selectedYear,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timetable'] });
      setShowAddSlot(false);
    },
    onError: (e: any) => alert(e.response?.data?.message ?? 'Error: check for conflicts.'),
  });

  const deleteMutation = useMutation({
    mutationFn: timetableApi.deleteSlot,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['timetable'] }),
  });

  // Map subject to color index
  const subjectColorMap: Record<string, number> = {};
  let colorIdx = 0;
  const getSubjectColor = (subjectId: string) => {
    if (!(subjectId in subjectColorMap)) {
      subjectColorMap[subjectId] = colorIdx++ % SUBJECT_COLORS.length;
    }
    return SUBJECT_COLORS[subjectColorMap[subjectId]];
  };

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Timetable</h1>
          <p className="text-sm text-gray-500 mt-1">View and manage class schedules</p>
        </div>
        {selectedClass && selectedYear && (
          <button onClick={() => setShowAddSlot(true)}
            className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition">
            + Add Slot
          </button>
        )}
      </div>

      {/* Controls */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5 mb-6">
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Class</label>
            <select value={selectedClass} onChange={e => setSelectedClass(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="">Select a class</option>
              {(classes as any[]).map((c: any) => (
                <option key={c.id} value={c.id}>{c.displayName}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Academic Year</label>
            <select value={selectedYear} onChange={e => setSelectedYear(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="">Select academic year</option>
              {(years as any[]).map((y: any) => (
                <option key={y.id} value={y.id}>{y.name}</option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Add Slot Form */}
      {showAddSlot && (
        <div className="bg-white border border-gray-200 rounded-xl p-5 mb-6 shadow-sm">
          <h3 className="font-semibold text-gray-800 mb-4">Add Timetable Slot</h3>
          <div className="grid grid-cols-4 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Day</label>
              <select value={newSlot.dayOfWeek}
                onChange={e => setNewSlot(f => ({ ...f, dayOfWeek: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                {DAYS.map(d => <option key={d}>{d}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Period</label>
              <select value={newSlot.period}
                onChange={e => setNewSlot(f => ({ ...f, period: parseInt(e.target.value) }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                {PERIODS.map(p => <option key={p}>{p}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Subject</label>
              <select value={newSlot.subjectId}
                onChange={e => setNewSlot(f => ({ ...f, subjectId: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option value="">Select subject</option>
                {(selectedClassObj?.subjects ?? []).map((s: any) => (
                  <option key={s.subjectId} value={s.subjectId}>{s.subjectName}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Room</label>
              <input value={newSlot.roomNumber}
                onChange={e => setNewSlot(f => ({ ...f, roomNumber: e.target.value }))}
                placeholder="e.g. Room 1A"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Start Time</label>
              <input type="time" value={newSlot.startTime}
                onChange={e => setNewSlot(f => ({ ...f, startTime: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">End Time</label>
              <input type="time" value={newSlot.endTime}
                onChange={e => setNewSlot(f => ({ ...f, endTime: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>
          <div className="flex gap-3">
            <button onClick={() => createMutation.mutate()} disabled={createMutation.isPending}
              className="px-4 py-2 bg-blue-700 text-white text-sm font-semibold rounded-lg hover:bg-blue-800 disabled:opacity-50 transition">
              {createMutation.isPending ? 'Adding...' : 'Add Slot'}
            </button>
            <button onClick={() => setShowAddSlot(false)}
              className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition">Cancel</button>
          </div>
        </div>
      )}

      {/* Timetable Grid */}
      {!(selectedClass && selectedYear) ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
          Select a class and academic year to view the timetable.
        </div>
      ) : isLoading ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400 animate-pulse">
          Loading timetable...
        </div>
      ) : timetable ? (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-x-auto">
          <div className="px-5 py-3 border-b border-gray-200 bg-gray-50">
            <p className="font-semibold text-gray-800">
              {timetable.className} · {timetable.academicYearName}
            </p>
          </div>
          <table className="w-full text-sm min-w-[700px]">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-3 py-2 font-semibold text-gray-600 w-16">Period</th>
                {DAYS.map(day => (
                  <th key={day} className="text-center px-3 py-2 font-semibold text-gray-600">{day}</th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {PERIODS.map(period => (
                <tr key={period} className="hover:bg-gray-50 transition">
                  <td className="px-3 py-2 text-center">
                    <span className="w-7 h-7 rounded-full bg-blue-100 text-blue-700 text-xs font-bold flex items-center justify-center">
                      {period}
                    </span>
                  </td>
                  {DAYS.map(day => {
                    const slot = timetable.schedule[day]?.find((s: TimetableSlot) => s.period === period);
                    return (
                      <td key={day} className="px-2 py-2">
                        {slot ? (
                          <div className={`rounded-lg border p-2 text-xs ${getSubjectColor(slot.subjectId)} relative group`}>
                            <p className="font-semibold truncate">{slot.subjectName}</p>
                            <p className="text-xs opacity-70 truncate">{slot.teacherName}</p>
                            {slot.roomNumber && (
                              <p className="text-xs opacity-60">{slot.roomNumber}</p>
                            )}
                            <p className="text-xs opacity-60">{slot.startTime}–{slot.endTime}</p>
                            <button
                              onClick={() => { if (confirm('Remove this slot?')) deleteMutation.mutate(slot.id); }}
                              className="absolute top-1 right-1 opacity-0 group-hover:opacity-100 text-red-500 hover:text-red-700 text-xs transition">
                              ✕
                            </button>
                          </div>
                        ) : (
                          <div className="rounded-lg border border-dashed border-gray-200 p-2 text-center text-gray-300 text-xs h-16 flex items-center justify-center">
                            —
                          </div>
                        )}
                      </td>
                    );
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  );
}