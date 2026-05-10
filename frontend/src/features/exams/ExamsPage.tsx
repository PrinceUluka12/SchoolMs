import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { examsApi, type Exam, type ExamSchedule, type SeatingArrangement } from './examsApi';
import { academicApi } from '../academic/academicApi';
import { format } from 'date-fns';

const EXAM_TYPES = ['MidTerm', 'EndOfTerm', 'Mock', 'Entrance', 'CAT'];
const STATUS_COLORS: Record<string, string> = {
  Scheduled: 'bg-blue-100 text-blue-800',
  Ongoing: 'bg-green-100 text-green-800',
  Completed: 'bg-gray-100 text-gray-600',
  Cancelled: 'bg-red-100 text-red-800',
};

export default function ExamsPage() {
  const queryClient = useQueryClient();
  const [selectedTerm, setSelectedTerm] = useState('');
  const [selectedExam, setSelectedExam] = useState<Exam | null>(null);
  const [selectedSchedule, setSelectedSchedule] = useState<ExamSchedule | null>(null);
  const [showCreateExam, setShowCreateExam] = useState(false);
  const [showAddSchedule, setShowAddSchedule] = useState(false);
  const [examForm, setExamForm] = useState({ name: '', type: 'EndOfTerm', academicYearId: '', description: '' });
  const [scheduleForm, setScheduleForm] = useState({
    subjectId: '', classId: '', examDate: '', startTime: '09:00', endTime: '11:00', venue: '', totalSeats: ''
  });

  const { data: years = [] } = useQuery({ queryKey: ['academic-years'], queryFn: academicApi.getAcademicYears });
  const { data: classes = [] } = useQuery({ queryKey: ['classes'], queryFn: () => academicApi.getClasses() });
  const { data: subjects = [] } = useQuery({ queryKey: ['subjects'], queryFn: academicApi.getSubjects });
  const allTerms = (years as any[]).flatMap((y: any) => y.terms ?? []);

  const { data: exams = [], isLoading } = useQuery({
    queryKey: ['exams', selectedTerm],
    queryFn: () => examsApi.getByTerm(selectedTerm),
    enabled: !!selectedTerm,
  });

  const { data: seating = [] } = useQuery({
    queryKey: ['seating', selectedSchedule?.id],
    queryFn: () => examsApi.getSeating(selectedSchedule!.id),
    enabled: !!selectedSchedule,
  });

  const createExamMutation = useMutation({
    mutationFn: () => examsApi.createExam({ ...examForm, termId: selectedTerm }),
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['exams'] }); setShowCreateExam(false); },
    onError: (e: any) => alert(e.response?.data?.message ?? 'Error'),
  });

  const addScheduleMutation = useMutation({
    mutationFn: () => examsApi.addSchedule({
      ...scheduleForm,
      examId: selectedExam!.id,
      totalSeats: scheduleForm.totalSeats ? parseInt(scheduleForm.totalSeats) : undefined,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['exams'] });
      setShowAddSchedule(false);
      setScheduleForm({ subjectId: '', classId: '', examDate: '', startTime: '09:00', endTime: '11:00', venue: '', totalSeats: '' });
    },
    onError: (e: any) => alert(e.response?.data?.message ?? 'Error'),
  });

  const generateSeatingMutation = useMutation({
    mutationFn: (scheduleId: string) => examsApi.generateSeating(scheduleId),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['seating'] }),
  });

  const deleteExamMutation = useMutation({
    mutationFn: examsApi.deleteExam,
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['exams'] }); setSelectedExam(null); },
    onError: (e: any) => alert(e.response?.data?.message ?? 'Cannot delete'),
  });

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Exam Management</h1>
          <p className="text-sm text-gray-500 mt-1">Schedule exams, generate seating, download admit cards</p>
        </div>
        <div className="flex gap-3 items-center">
          <select value={selectedTerm} onChange={e => setSelectedTerm(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none">
            <option value="">Select Term</option>
            {allTerms.map((t: any) => <option key={t.id} value={t.id}>{t.name}</option>)}
          </select>
          {selectedTerm && (
            <button onClick={() => { setShowCreateExam(true); setExamForm(f => ({ ...f, academicYearId: '' })); }}
              className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition">
              + New Exam
            </button>
          )}
        </div>
      </div>

      {/* Create Exam Form */}
      {showCreateExam && (
        <div className="bg-white border border-gray-200 rounded-xl p-5 mb-6 shadow-sm">
          <h3 className="font-semibold text-gray-800 mb-4">New Exam</h3>
          <div className="grid grid-cols-3 gap-4 mb-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Exam Name *</label>
              <input value={examForm.name} onChange={e => setExamForm(f => ({ ...f, name: e.target.value }))}
                placeholder="e.g. First Term Exams 2026"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Type *</label>
              <select value={examForm.type} onChange={e => setExamForm(f => ({ ...f, type: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                {EXAM_TYPES.map(t => <option key={t}>{t}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Academic Year *</label>
              <select value={examForm.academicYearId} onChange={e => setExamForm(f => ({ ...f, academicYearId: e.target.value }))}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option value="">Select year</option>
                {(years as any[]).map((y: any) => <option key={y.id} value={y.id}>{y.name}</option>)}
              </select>
            </div>
          </div>
          <div className="flex gap-3">
            <button onClick={() => createExamMutation.mutate()} disabled={createExamMutation.isPending}
              className="px-4 py-2 bg-blue-700 text-white text-sm font-semibold rounded-lg hover:bg-blue-800 disabled:opacity-50 transition">
              {createExamMutation.isPending ? 'Creating...' : 'Create Exam'}
            </button>
            <button onClick={() => setShowCreateExam(false)}
              className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition">Cancel</button>
          </div>
        </div>
      )}

      {!selectedTerm ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
          Select a term to view exams.
        </div>
      ) : (
        <div className="flex gap-6">
          {/* Exam List */}
          <div className="w-72 shrink-0">
            {isLoading ? (
              <div className="space-y-2">
                {[1,2,3].map(i => <div key={i} className="bg-white rounded-xl h-20 animate-pulse border border-gray-200" />)}
              </div>
            ) : (exams as Exam[]).length === 0 ? (
              <div className="bg-white rounded-xl border border-gray-200 p-6 text-center text-gray-400 text-sm">
                No exams for this term.
              </div>
            ) : (
              <div className="space-y-2">
                {(exams as Exam[]).map(exam => (
                  <button key={exam.id} onClick={() => setSelectedExam(exam)}
                    className={`w-full text-left bg-white rounded-xl border shadow-sm p-4 hover:shadow-md transition
                      ${selectedExam?.id === exam.id ? 'border-blue-400 ring-2 ring-blue-100' : 'border-gray-200'}`}>
                    <div className="flex items-start justify-between">
                      <p className="font-semibold text-gray-900 text-sm">{exam.name}</p>
                      <span className={`px-2 py-0.5 text-xs rounded-full ${STATUS_COLORS[exam.status]}`}>
                        {exam.status}
                      </span>
                    </div>
                    <p className="text-xs text-gray-500 mt-1">{exam.type} · {exam.scheduleCount} subjects</p>
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Exam Detail */}
          <div className="flex-1">
            {!selectedExam ? (
              <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
                Select an exam to manage schedules and seating.
              </div>
            ) : (
              <div className="space-y-4">
                {/* Exam Header */}
                <div className="bg-white rounded-xl border border-gray-200 shadow-sm px-5 py-4 flex items-center justify-between">
                  <div>
                    <h2 className="font-bold text-gray-900">{selectedExam.name}</h2>
                    <p className="text-sm text-gray-500">{selectedExam.type} · {selectedExam.termName}</p>
                  </div>
                  <div className="flex gap-2">
                    <button onClick={() => setShowAddSchedule(true)}
                      className="px-3 py-1.5 bg-blue-700 text-white text-xs font-semibold rounded-lg hover:bg-blue-800 transition">
                      + Add Subject
                    </button>
                    <button onClick={() => { if (confirm('Delete this exam?')) deleteExamMutation.mutate(selectedExam.id); }}
                      className="px-3 py-1.5 border border-red-200 text-red-600 text-xs font-medium rounded-lg hover:bg-red-50 transition">
                      Delete
                    </button>
                  </div>
                </div>

                {/* Add Schedule Form */}
                {showAddSchedule && (
                  <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
                    <h3 className="font-semibold text-gray-800 mb-4">Add Subject Schedule</h3>
                    <div className="grid grid-cols-3 gap-4 mb-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Subject *</label>
                        <select value={scheduleForm.subjectId}
                          onChange={e => setScheduleForm(f => ({ ...f, subjectId: e.target.value }))}
                          className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                          <option value="">Select subject</option>
                          {(subjects as any[]).map((s: any) => (
                            <option key={s.id} value={s.id}>{s.name}</option>
                          ))}
                        </select>
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Class *</label>
                        <select value={scheduleForm.classId}
                          onChange={e => setScheduleForm(f => ({ ...f, classId: e.target.value }))}
                          className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                          <option value="">Select class</option>
                          {(classes as any[]).map((c: any) => (
                            <option key={c.id} value={c.id}>{c.displayName}</option>
                          ))}
                        </select>
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Exam Date *</label>
                        <input type="date" value={scheduleForm.examDate}
                          onChange={e => setScheduleForm(f => ({ ...f, examDate: e.target.value }))}
                          className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Start Time *</label>
                        <input type="time" value={scheduleForm.startTime}
                          onChange={e => setScheduleForm(f => ({ ...f, startTime: e.target.value }))}
                          className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">End Time *</label>
                        <input type="time" value={scheduleForm.endTime}
                          onChange={e => setScheduleForm(f => ({ ...f, endTime: e.target.value }))}
                          className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">Venue</label>
                        <input value={scheduleForm.venue}
                          onChange={e => setScheduleForm(f => ({ ...f, venue: e.target.value }))}
                          placeholder="e.g. Main Hall"
                          className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                      </div>
                    </div>
                    <div className="flex gap-3">
                      <button onClick={() => addScheduleMutation.mutate()} disabled={addScheduleMutation.isPending}
                        className="px-4 py-2 bg-blue-700 text-white text-sm font-semibold rounded-lg hover:bg-blue-800 disabled:opacity-50 transition">
                        {addScheduleMutation.isPending ? 'Adding...' : 'Add Schedule'}
                      </button>
                      <button onClick={() => setShowAddSchedule(false)}
                        className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition">Cancel</button>
                    </div>
                  </div>
                )}

                {/* Schedules Table */}
                <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
                  <div className="px-5 py-3 border-b border-gray-200 bg-gray-50">
                    <p className="font-semibold text-gray-800">Subject Schedules</p>
                  </div>
                  <table className="w-full text-sm">
                    <thead className="bg-gray-50 border-b border-gray-200">
                      <tr>
                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Subject</th>
                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Class</th>
                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Date</th>
                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Time</th>
                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Venue</th>
                        <th className="text-center px-4 py-3 font-semibold text-gray-600">Seated</th>
                        <th className="text-left px-4 py-3 font-semibold text-gray-600">Actions</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                      {selectedExam.schedules.length === 0 ? (
                        <tr>
                          <td colSpan={7} className="text-center py-6 text-gray-400">
                            No subjects scheduled yet.
                          </td>
                        </tr>
                      ) : (
                        selectedExam.schedules.map(sched => (
                          <tr key={sched.id}
                            className={`hover:bg-gray-50 transition ${selectedSchedule?.id === sched.id ? 'bg-blue-50' : ''}`}>
                            <td className="px-4 py-3">
                              <p className="font-medium text-gray-900">{sched.subjectName}</p>
                              <p className="text-xs text-gray-400">{sched.subjectCode}</p>
                            </td>
                            <td className="px-4 py-3 text-gray-600">{sched.className}</td>
                            <td className="px-4 py-3 text-gray-600">
                              {format(new Date(sched.examDate), 'dd MMM yyyy')}
                            </td>
                            <td className="px-4 py-3 text-gray-600">{sched.startTime} – {sched.endTime}</td>
                            <td className="px-4 py-3 text-gray-500">{sched.venue ?? '—'}</td>
                            <td className="px-4 py-3 text-center text-gray-600">{sched.seatedStudents}</td>
                            <td className="px-4 py-3">
                              <div className="flex gap-2">
                                <button onClick={() => {
                                    setSelectedSchedule(sched);
                                    generateSeatingMutation.mutate(sched.id);
                                  }}
                                  className="text-xs text-blue-600 hover:underline">
                                  Gen. Seating
                                </button>
                                <button onClick={() => setSelectedSchedule(sched)}
                                  className="text-xs text-purple-600 hover:underline">View</button>
                                <button onClick={() => examsApi.downloadSeatingPdf(sched.id)}
                                  className="text-xs text-green-600 hover:underline">PDF</button>
                              </div>
                            </td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>

                {/* Seating View */}
                {selectedSchedule && (seating as SeatingArrangement[]).length > 0 && (
                  <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
                    <div className="flex items-center justify-between px-5 py-3 border-b border-gray-200 bg-gray-50">
                      <p className="font-semibold text-gray-800">
                        Seating — {selectedSchedule.subjectName} · {selectedSchedule.className}
                      </p>
                      <div className="flex gap-2">
                        {selectedExam && (
                          <button onClick={() => {
                              const studentId = (seating as SeatingArrangement[])[0]?.studentId;
                              if (studentId) examsApi.downloadAdmitCard(selectedExam.id, studentId);
                            }}
                            className="text-xs text-blue-600 hover:underline">Sample Admit Card</button>
                        )}
                        <button onClick={() => examsApi.downloadSeatingPdf(selectedSchedule.id)}
                          className="text-xs text-green-600 hover:underline">Download PDF</button>
                      </div>
                    </div>
                    <div className="p-4 grid grid-cols-4 md:grid-cols-6 lg:grid-cols-8 gap-2">
                      {(seating as SeatingArrangement[]).map(sa => (
                        <div key={sa.id}
                          className="bg-blue-50 border border-blue-200 rounded-lg p-2 text-center">
                          <p className="text-xs font-bold text-blue-800">{sa.seatNumber}</p>
                          <p className="text-xs text-gray-700 mt-0.5 truncate">{sa.studentName.split(' ')[0]}</p>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}