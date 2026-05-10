import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { gradesApi } from './gradesApi';
import { academicApi } from '../academic/academicApi';

const ASSESSMENT_TYPES = ['CA1', 'CA2', 'MidTerm', 'Exam', 'Final'];

export default function GradesPage() {
  const queryClient = useQueryClient();
  const [selectedClass, setSelectedClass] = useState('');
  const [selectedTerm, setSelectedTerm] = useState('');
  const [selectedSubject, setSelectedSubject] = useState('');
  const [assessmentType, setAssessmentType] = useState('CA1');
  const [maxScore, setMaxScore] = useState(100);
  const [scores, setScores] = useState<Record<string, number>>({});
  const [saved, setSaved] = useState(false);

  const { data: classes = [] } = useQuery({
    queryKey: ['classes'],
    queryFn: () => academicApi.getClasses(),
  });

  const { data: years = [] } = useQuery({
    queryKey: ['academic-years'],
    queryFn: academicApi.getAcademicYears,
  });

  const allTerms = (years as any[]).flatMap((y: any) => y.terms ?? []);

  const selectedClassObj = (classes as any[]).find((c: any) => c.id === selectedClass);

  const { data: sheet, isLoading } = useQuery({
    queryKey: ['grade-sheet', selectedClass, selectedTerm, selectedSubject, assessmentType],
    queryFn: () => gradesApi.getSheet(selectedClass, selectedTerm, selectedSubject, assessmentType),
    enabled: !!(selectedClass && selectedTerm && selectedSubject),
  });

  useEffect(() => {
    if (!sheet) return;
    const init: Record<string, number> = {};
    sheet.grades.forEach(g => { init[g.studentId] = g.score; });
    setScores(init);
    setSaved(false);
  }, [sheet]);

  const { data: classReports } = useQuery({
    queryKey: ['class-reports', selectedClass, selectedTerm],
    queryFn: () => gradesApi.getClassReports(selectedClass, selectedTerm),
    enabled: !!(selectedClass && selectedTerm),
  });

  const bulkMutation = useMutation({
    mutationFn: () => gradesApi.bulkEnter({
      subjectId: selectedSubject,
      termId: selectedTerm,
      classId: selectedClass,
      assessmentType,
      maxScore,
      entries: Object.entries(scores).map(([studentId, score]) => ({ studentId, score }))
    }),
    onSuccess: () => {
      setSaved(true);
      queryClient.invalidateQueries({ queryKey: ['grade-sheet'] });
    }
  });

  const lockMutation = useMutation({
    mutationFn: () => gradesApi.lockGrades(selectedClass, selectedTerm),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['grade-sheet'] }),
  });

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Grade Entry</h1>
          <p className="text-sm text-gray-500 mt-1">Enter and manage student grades</p>
        </div>
        {selectedClass && selectedTerm && (
          <div className="flex gap-2">
            <button onClick={() => gradesApi.downloadClassReportCards(selectedClass, selectedTerm)}
              className="px-4 py-2 bg-green-700 hover:bg-green-800 text-white text-sm font-semibold rounded-lg transition">
              ⬇ Download Report Cards
            </button>
            <button onClick={() => lockMutation.mutate()}
              disabled={lockMutation.isPending}
              className="px-4 py-2 bg-amber-600 hover:bg-amber-700 text-white text-sm font-semibold rounded-lg transition disabled:opacity-50">
              🔒 Lock Grades
            </button>
          </div>
        )}
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5 mb-6">
        <div className="grid grid-cols-5 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Class</label>
            <select value={selectedClass} onChange={e => setSelectedClass(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="">Select class</option>
              {(classes as any[]).map((c: any) => (
                <option key={c.id} value={c.id}>{c.displayName}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Term</label>
            <select value={selectedTerm} onChange={e => setSelectedTerm(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="">Select term</option>
              {allTerms.map((t: any) => (
                <option key={t.id} value={t.id}>{t.name}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Subject</label>
            <select value={selectedSubject} onChange={e => setSelectedSubject(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="">Select subject</option>
              {(selectedClassObj?.subjects ?? []).map((s: any) => (
                <option key={s.subjectId} value={s.subjectId}>{s.subjectName}</option>
              ))}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Assessment</label>
            <select value={assessmentType} onChange={e => setAssessmentType(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
              {ASSESSMENT_TYPES.map(t => <option key={t}>{t}</option>)}
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Max Score</label>
            <input type="number" min={1} value={maxScore}
              onChange={e => setMaxScore(parseInt(e.target.value))}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
        </div>
      </div>

      {/* Grade Sheet */}
      {!(selectedClass && selectedTerm && selectedSubject) ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
          Select a class, term, and subject to load the grade sheet.
        </div>
      ) : isLoading ? (
        <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400 animate-pulse">
          Loading grade sheet...
        </div>
      ) : (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="flex items-center justify-between px-5 py-3 border-b border-gray-200 bg-gray-50">
            <p className="font-semibold text-gray-800">
              {sheet?.className} · {sheet?.subjectName} · {sheet?.termName} · {sheet?.assessmentType}
            </p>
            <button onClick={() => bulkMutation.mutate()} disabled={bulkMutation.isPending}
              className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition disabled:opacity-50">
              {bulkMutation.isPending ? 'Saving...' : saved ? '✓ Saved' : 'Save Grades'}
            </button>
          </div>

          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 font-semibold text-gray-600 w-8">#</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Student</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Number</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600 w-40">Score / {maxScore}</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">%</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Status</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {sheet?.grades.length === 0 ? (
                <tr>
                  <td colSpan={6} className="text-center py-8 text-gray-400">
                    No students found in this class. Add students first.
                  </td>
                </tr>
              ) : (
                sheet?.grades.map((grade: any, idx: number) => {
                  const score = scores[grade.studentId] ?? grade.score ?? 0;
                  const pct = maxScore > 0 ? Math.round(score / maxScore * 100) : 0;
                  return (
                    <tr key={grade.studentId} className="hover:bg-gray-50 transition">
                      <td className="px-4 py-2 text-gray-400 text-xs">{idx + 1}</td>
                      <td className="px-4 py-2 font-medium text-gray-900">{grade.studentName}</td>
                      <td className="px-4 py-2 font-mono text-xs text-gray-600">{grade.studentNumber}</td>
                      <td className="px-4 py-2">
                        <input
                          type="number" min={0} max={maxScore}
                          value={scores[grade.studentId] ?? ''}
                          onChange={e => setScores(prev => ({
                            ...prev,
                            [grade.studentId]: parseFloat(e.target.value) || 0
                          }))}
                          disabled={grade.isLocked}
                          className="w-24 border border-gray-300 rounded-lg px-2 py-1 text-sm
                            focus:outline-none focus:ring-2 focus:ring-blue-500
                            disabled:bg-gray-100 disabled:cursor-not-allowed" />
                      </td>
                      <td className="px-4 py-2">
                        <span className={`text-sm font-medium ${
                          pct >= 80 ? 'text-green-700' :
                          pct >= 60 ? 'text-blue-700' :
                          pct >= 50 ? 'text-yellow-700' : 'text-red-700'
                        }`}>{isNaN(pct) ? '—' : `${pct}%`}</span>
                      </td>
                      <td className="px-4 py-2">
                        {grade.isLocked
                          ? <span className="text-xs text-gray-400">🔒 Locked</span>
                          : <span className="text-xs text-green-600">Editable</span>}
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Class Rankings */}
      {classReports && (classReports as any[]).length > 0 && (
        <div className="mt-6 bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-200 bg-gray-50">
            <p className="font-semibold text-gray-800">Class Rankings — Current Term</p>
          </div>
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Rank</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Student</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Average</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Grade</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Attendance</th>
                <th className="text-left px-4 py-3 font-semibold text-gray-600">Report Card</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {(classReports as any[]).map((r: any) => (
                <tr key={r.studentId} className="hover:bg-gray-50 transition">
                  <td className="px-4 py-3">
                    <span className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold
                      ${r.classRank === 1 ? 'bg-yellow-100 text-yellow-800' :
                        r.classRank === 2 ? 'bg-gray-100 text-gray-700' :
                        r.classRank === 3 ? 'bg-orange-100 text-orange-700' :
                        'bg-gray-50 text-gray-600'}`}>
                      {r.classRank}
                    </span>
                  </td>
                  <td className="px-4 py-3 font-medium text-gray-900">{r.studentName}</td>
                  <td className="px-4 py-3 font-bold text-blue-700">{r.overallAverage}%</td>
                  <td className="px-4 py-3">
                    <span className="px-2 py-1 bg-blue-100 text-blue-800 text-xs font-bold rounded-full">
                      {r.overallGrade}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-gray-600">{r.attendanceRate}%</td>
                  <td className="px-4 py-3">
                    <button onClick={() => gradesApi.downloadReportCard(r.studentId, selectedTerm)}
                      className="text-xs text-blue-600 hover:underline">
                      ⬇ PDF
                    </button>
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