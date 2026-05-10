import api from '../../api/axiosInstance';

export interface Message {
  id: string;
  senderId: string;
  senderName: string;
  senderRole: string;
  recipientId?: string;
  recipientName?: string;
  subject: string;
  body: string;
  type: string;
  isRead: boolean;
  readAt?: string;
  createdAt: string;
}

export interface Announcement {
  id: string;
  title: string;
  body: string;
  audience: string;
  isPinned: boolean;
  expiresAt?: string;
  isExpired: boolean;
  createdById: string;
  createdByName: string;
  createdAt: string;
}

export const communicationApi = {
  getInbox: () =>
    api.get<{ unreadCount: number; messages: Message[] }>('/communications/inbox').then(r => r.data),

  getSent: () =>
    api.get<Message[]>('/communications/sent').then(r => r.data),

  sendMessage: (recipientId: string, subject: string, body: string) =>
    api.post<Message>('/communications/messages', { recipientId, subject, body }).then(r => r.data),

  markRead: (id: string) =>
    api.patch(`/communications/messages/${id}/read`),

  deleteMessage: (id: string) =>
    api.delete(`/communications/messages/${id}`),

  getAnnouncements: (audience?: string) =>
    api.get<Announcement[]>('/communications/announcements', { params: { audience } }).then(r => r.data),

  createAnnouncement: (data: {
    title: string;
    body: string;
    audience: string;
    isPinned: boolean;
    expiresAt?: string;
  }) => api.post<Announcement>('/communications/announcements', data).then(r => r.data),

  deleteAnnouncement: (id: string) =>
    api.delete(`/communications/announcements/${id}`),
};