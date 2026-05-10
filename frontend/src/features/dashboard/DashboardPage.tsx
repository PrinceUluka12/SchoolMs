import { useQuery } from '@tanstack/react-query';
import { dashboardApi } from './dashboardApi';
import {
  BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer,
  PieChart, Pie, Cell, Legend
} from 'recharts';
import { formatDistanceToNow } from 'date-fns';

const GENDER_COLORS = ['#2E75B6', '#E879A0', '#94A3B8'];
const ROLE_COLORS = ['#2E75B6', '#10B981', '#F59E0B', '#8B5CF6', '#EF4444', '#14B8A6'];

function StatCard({
  label, value, sub, color
}: { label: string; value: number | string; sub?: string; color: string }) {
  return (
    <div className={`bg-white rounded-xl border border-gray-200 shadow-sm p-5`}>
      <p className="text-sm text-gray-500 font-medium">{label}</p>
      <p className={`text-3xl font-bold mt-1 ${color}`}>{value}</p>
      {sub && <p className="text-xs text-gray-400 mt-1">{sub}</p>}
    </div>
  );
}

export default function DashboardPage() {
  const { data, isLoading } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: dashboardApi.getStats,
    refetchInterval: 60_000,
  });

  if (isLoading) {
    return (
      <div className="p-6 grid grid-cols-4 gap-4">
        {Array.from({ length: 8 }).map((_, i) => (
          <div key={i} className="bg-white rounded-xl border border-gray-200 h-24 animate-pulse" />
        ))}
      </div>
    );
  }

  const genderData = data?.studentsByGender.map(g => ({ name: g.gender, value: g.count })) ?? [];
  const roleData = data?.staffByRole.map(r => ({ name: r.role, value: r.count })) ?? [];
  const classData = data?.studentsByClass.map(c => ({ name: c.className, count: c.count })) ?? [];

  return (
    <div className="p-6 space-y-6">
      {/* Academic Context Banner */}
      {(data?.currentAcademicYear || data?.currentTerm) && (
        <div className="bg-blue-50 border border-blue-200 rounded-xl px-5 py-3 flex items-center gap-4">
          <span className="text-blue-700 text-sm font-semibold">📅 Current Period:</span>
          {data.currentAcademicYear && (
            <span className="text-blue-900 text-sm font-medium">{data.currentAcademicYear}</span>
          )}
          {data.currentTerm && (
            <>
              <span className="text-blue-300">·</span>
              <span className="text-blue-900 text-sm">{data.currentTerm}</span>
            </>
          )}
        </div>
      )}

      {/* KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <StatCard label="Total Students" value={data?.totalStudents ?? 0}
          sub={`${data?.activeStudents ?? 0} active`} color="text-blue-700" />
        <StatCard label="Total Staff" value={data?.totalStaff ?? 0}
          sub={`${data?.activeStaff ?? 0} active`} color="text-purple-700" />
        <StatCard label="Total Classes" value={data?.totalClasses ?? 0}
          sub="Current academic year" color="text-green-700" />
        <StatCard label="Attendance Rate" value="—"
          sub="Available from Sprint 5" color="text-amber-600" />
      </div>

      {/* Charts Row */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Students by Class */}
        <div className="md:col-span-2 bg-white rounded-xl border border-gray-200 shadow-sm p-5">
          <h3 className="text-sm font-semibold text-gray-700 mb-4">Students by Class</h3>
          {classData.length === 0 ? (
            <div className="h-48 flex items-center justify-center text-gray-400 text-sm">
              No class data yet
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={200}>
              <BarChart data={classData} margin={{ top: 0, right: 0, left: -20, bottom: 0 }}>
                <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} allowDecimals={false} />
                <Tooltip />
                <Bar dataKey="count" fill="#2E75B6" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          )}
        </div>

        {/* Students by Gender */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
          <h3 className="text-sm font-semibold text-gray-700 mb-4">Students by Gender</h3>
          {genderData.length === 0 ? (
            <div className="h-48 flex items-center justify-center text-gray-400 text-sm">
              No data yet
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={200}>
              <PieChart>
                <Pie data={genderData} cx="50%" cy="50%" innerRadius={50}
                  outerRadius={80} dataKey="value" paddingAngle={3}>
                  {genderData.map((_, i) => (
                    <Cell key={i} fill={GENDER_COLORS[i % GENDER_COLORS.length]} />
                  ))}
                </Pie>
                <Legend iconType="circle" iconSize={10} />
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      {/* Bottom Row */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {/* Staff by Role */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
          <h3 className="text-sm font-semibold text-gray-700 mb-4">Staff by Role</h3>
          {roleData.length === 0 ? (
            <div className="h-40 flex items-center justify-center text-gray-400 text-sm">
              No staff data yet
            </div>
          ) : (
            <div className="space-y-2">
              {roleData.map((r, i) => {
                const total = roleData.reduce((a, b) => a + b.value, 0);
                const pct = total > 0 ? Math.round((r.value / total) * 100) : 0;
                return (
                  <div key={r.name}>
                    <div className="flex justify-between text-xs text-gray-600 mb-1">
                      <span>{r.name}</span>
                      <span>{r.value} ({pct}%)</span>
                    </div>
                    <div className="w-full bg-gray-100 rounded-full h-2">
                      <div className="h-2 rounded-full transition-all"
                        style={{
                          width: `${pct}%`,
                          backgroundColor: ROLE_COLORS[i % ROLE_COLORS.length]
                        }} />
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Recently Added Students */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
          <h3 className="text-sm font-semibold text-gray-700 mb-4">Recently Enrolled</h3>
          {!data?.recentStudents?.length ? (
            <div className="h-40 flex items-center justify-center text-gray-400 text-sm">
              No students enrolled yet
            </div>
          ) : (
            <div className="space-y-3">
              {data.recentStudents.map(s => (
                <div key={s.id} className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 rounded-full bg-blue-100 flex items-center
                      justify-center text-blue-700 font-bold text-xs shrink-0">
                      {s.fullName.split(' ').map(n => n[0]).join('').slice(0, 2)}
                    </div>
                    <div>
                      <p className="text-sm font-medium text-gray-900">{s.fullName}</p>
                      <p className="text-xs text-gray-400">
                        {s.studentNumber} · {s.className ?? 'Unassigned'}
                      </p>
                    </div>
                  </div>
                  <span className="text-xs text-gray-400">
                    {formatDistanceToNow(new Date(s.createdAt), { addSuffix: true })}
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}