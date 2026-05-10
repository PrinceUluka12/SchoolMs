import api from '../../api/axiosInstance';

export interface TimetableSlot {
  id: string;
  classId: string;
  className: string;
  subjectId: string;
  subjectName: string;
  subjectCode: string;
  teacherId: string;
  teacherName: string;
  dayOfWeek: string;
  period: number;
  startTime: string;
  endTime: string;
  roomNumber?: string;
  academicYearId: string;
}

export interface ClassTimetable {
  classId: string;
  className: string;
  academicYearName: string;
  schedule: Record<string, TimetableSlot[]>;
}

export const timetableApi = {
  getClassTimetable: (classId: string, academicYearId: string) =>
    api.get<ClassTimetable>(`/timetable/class/${classId}`, {
      params: { academicYearId }
    }).then(r => r.data),

  createSlot: (data: unknown) =>
    api.post<TimetableSlot>('/timetable/slots', data).then(r => r.data),

  deleteSlot: (id: string) =>
    api.delete(`/timetable/slots/${id}`),

  checkConflict: (data: unknown) =>
    api.post('/timetable/slots/check-conflict', data).then(r => r.data),
};