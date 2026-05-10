import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { communicationApi, type Message, type Announcement } from './communicationApi';
import { formatDistanceToNow, format } from 'date-fns';

export default function CommunicationPage() {
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<'inbox'|'sent'|'announcements'>('inbox');
  const [showCompose, setShowCompose] = useState(false);
  const [showCreateAnnouncement, setShowCreateAnnouncement] = useState(false);
  const [selectedMessage, setSelectedMessage] = useState<Message | null>(null);
  const [composeForm, setComposeForm] = useState({ recipientId: '', subject: '', body: '' });
  const [announcementForm, setAnnouncementForm] = useState({
    title: '', body: '', audience: 'All', isPinned: false, expiresAt: ''
  });

  const { data: inbox } = useQuery({
    queryKey: ['inbox'],
    queryFn: communicationApi.getInbox,
  });

  const { data: sent = [] } = useQuery({
    queryKey: ['sent'],
    queryFn: communicationApi.getSent,
  });

  const { data: announcements = [] } = useQuery({
    queryKey: ['announcements'],
    queryFn: () => communicationApi.getAnnouncements(),
  });

  const sendMutation = useMutation({
    mutationFn: () => communicationApi.sendMessage(
      composeForm.recipientId, composeForm.subject, composeForm.body
    ),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['inbox'] });
      queryClient.invalidateQueries({ queryKey: ['sent'] });
      setShowCompose(false);
      setComposeForm({ recipientId: '', subject: '', body: '' });
    },
    onError: (e: any) => alert(e.response?.data?.message ?? 'Failed to send'),
  });

  const markReadMutation = useMutation({
    mutationFn: communicationApi.markRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['inbox'] }),
  });

  const deleteMsgMutation = useMutation({
    mutationFn: communicationApi.deleteMessage,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['inbox'] });
      queryClient.invalidateQueries({ queryKey: ['sent'] });
      setSelectedMessage(null);
    },
  });

  const createAnnouncementMutation = useMutation({
    mutationFn: () => communicationApi.createAnnouncement({
      ...announcementForm,
      expiresAt: announcementForm.expiresAt || undefined,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['announcements'] });
      setShowCreateAnnouncement(false);
      setAnnouncementForm({ title: '', body: '', audience: 'All', isPinned: false, expiresAt: '' });
    },
  });

  const deleteAnnouncementMutation = useMutation({
    mutationFn: communicationApi.deleteAnnouncement,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['announcements'] }),
  });

  const messages = tab === 'inbox' ? (inbox?.messages ?? []) : (sent as Message[]);

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Communications</h1>
          {inbox && inbox.unreadCount > 0 && (
            <p className="text-sm text-blue-600 mt-1 font-medium">
              {inbox.unreadCount} unread message{inbox.unreadCount > 1 ? 's' : ''}
            </p>
          )}
        </div>
        <div className="flex gap-3">
          {tab === 'announcements' && (
            <button onClick={() => setShowCreateAnnouncement(true)}
              className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition">
              + New Announcement
            </button>
          )}
          {(tab === 'inbox' || tab === 'sent') && (
            <button onClick={() => setShowCompose(true)}
              className="px-4 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition">
              ✉ Compose
            </button>
          )}
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 bg-gray-100 rounded-xl p-1 mb-6 w-fit">
        {(['inbox', 'sent', 'announcements'] as const).map(t => (
          <button key={t} onClick={() => setTab(t)}
            className={`px-4 py-2 text-sm font-medium rounded-lg transition capitalize
              ${tab === t ? 'bg-white text-blue-700 shadow-sm' : 'text-gray-600 hover:text-gray-800'}`}>
            {t}
            {t === 'inbox' && inbox && inbox.unreadCount > 0 && (
              <span className="ml-1.5 bg-blue-600 text-white text-xs rounded-full px-1.5 py-0.5">
                {inbox.unreadCount}
              </span>
            )}
          </button>
        ))}
      </div>

      {/* Messages */}
      {tab !== 'announcements' && (
        <div className="flex gap-4 h-[600px]">
          {/* Message List */}
          <div className="w-80 bg-white rounded-xl border border-gray-200 shadow-sm overflow-y-auto shrink-0">
            {messages.length === 0 ? (
              <div className="p-8 text-center text-gray-400 text-sm">
                {tab === 'inbox' ? 'Your inbox is empty.' : 'No sent messages.'}
              </div>
            ) : (
              messages.map((msg: Message) => (
                <button key={msg.id}
                  onClick={() => { setSelectedMessage(msg); if (!msg.isRead && tab === 'inbox') markReadMutation.mutate(msg.id); }}
                  className={`w-full text-left px-4 py-3 border-b border-gray-100 hover:bg-gray-50 transition
                    ${selectedMessage?.id === msg.id ? 'bg-blue-50 border-l-2 border-l-blue-600' : ''}
                    ${!msg.isRead && tab === 'inbox' ? 'bg-blue-50/50' : ''}`}>
                  <div className="flex items-start justify-between gap-2">
                    <p className={`text-sm truncate ${!msg.isRead && tab === 'inbox' ? 'font-bold text-gray-900' : 'font-medium text-gray-700'}`}>
                      {tab === 'inbox' ? msg.senderName : (msg.recipientName ?? 'Broadcast')}
                    </p>
                    <span className="text-xs text-gray-400 shrink-0">
                      {formatDistanceToNow(new Date(msg.createdAt), { addSuffix: true })}
                    </span>
                  </div>
                  <p className="text-xs text-gray-600 mt-0.5 truncate">{msg.subject}</p>
                  {!msg.isRead && tab === 'inbox' && (
                    <span className="inline-block w-2 h-2 bg-blue-600 rounded-full mt-1" />
                  )}
                </button>
              ))
            )}
          </div>

          {/* Message Detail */}
          <div className="flex-1 bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
            {!selectedMessage ? (
              <div className="h-full flex items-center justify-center text-gray-400">
                Select a message to read it
              </div>
            ) : (
              <div className="h-full flex flex-col">
                <div className="px-6 py-4 border-b border-gray-200 flex items-start justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900">{selectedMessage.subject}</h3>
                    <p className="text-sm text-gray-500 mt-1">
                      From: <span className="font-medium">{selectedMessage.senderName}</span>
                      {' · '}
                      {format(new Date(selectedMessage.createdAt), 'MMM d, yyyy h:mm a')}
                    </p>
                    {selectedMessage.recipientName && (
                      <p className="text-sm text-gray-400">To: {selectedMessage.recipientName}</p>
                    )}
                  </div>
                  <button
                    onClick={() => { if (confirm('Delete this message?')) deleteMsgMutation.mutate(selectedMessage.id); }}
                    className="text-xs text-red-500 hover:underline">Delete</button>
                </div>
                <div className="flex-1 p-6 overflow-y-auto">
                  <p className="text-gray-700 leading-relaxed whitespace-pre-wrap">{selectedMessage.body}</p>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Announcements */}
      {tab === 'announcements' && (
        <div className="space-y-4">
          {showCreateAnnouncement && (
            <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
              <h3 className="font-semibold text-gray-800 mb-4">New Announcement</h3>
              <div className="space-y-3">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Title *</label>
                  <input value={announcementForm.title}
                    onChange={e => setAnnouncementForm(f => ({ ...f, title: e.target.value }))}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Body *</label>
                  <textarea value={announcementForm.body} rows={4}
                    onChange={e => setAnnouncementForm(f => ({ ...f, body: e.target.value }))}
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
                <div className="grid grid-cols-3 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Audience</label>
                    <select value={announcementForm.audience}
                      onChange={e => setAnnouncementForm(f => ({ ...f, audience: e.target.value }))}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                      {['All', 'Teachers', 'Students', 'Parents', 'Finance'].map(a => (
                        <option key={a}>{a}</option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">Expires At</label>
                    <input type="date" value={announcementForm.expiresAt}
                      onChange={e => setAnnouncementForm(f => ({ ...f, expiresAt: e.target.value }))}
                      className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                  </div>
                  <div className="flex items-end">
                    <label className="flex items-center gap-2 cursor-pointer">
                      <input type="checkbox" checked={announcementForm.isPinned}
                        onChange={e => setAnnouncementForm(f => ({ ...f, isPinned: e.target.checked }))}
                        className="rounded border-gray-300" />
                      <span className="text-sm text-gray-700">Pin to top</span>
                    </label>
                  </div>
                </div>
              </div>
              <div className="flex gap-3 mt-4">
                <button onClick={() => createAnnouncementMutation.mutate()}
                  disabled={createAnnouncementMutation.isPending}
                  className="px-4 py-2 bg-blue-700 text-white text-sm font-semibold rounded-lg hover:bg-blue-800 disabled:opacity-50 transition">
                  {createAnnouncementMutation.isPending ? 'Publishing...' : 'Publish'}
                </button>
                <button onClick={() => setShowCreateAnnouncement(false)}
                  className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition">Cancel</button>
              </div>
            </div>
          )}

          {(announcements as Announcement[]).length === 0 ? (
            <div className="bg-white rounded-xl border border-gray-200 p-12 text-center text-gray-400">
              No announcements yet.
            </div>
          ) : (
            (announcements as Announcement[]).map(ann => (
              <div key={ann.id}
                className={`bg-white rounded-xl border shadow-sm p-5
                  ${ann.isPinned ? 'border-blue-300 bg-blue-50/30' : 'border-gray-200'}`}>
                <div className="flex items-start justify-between">
                  <div className="flex items-start gap-3">
                    {ann.isPinned && (
                      <span className="mt-0.5 text-blue-600 text-sm">📌</span>
                    )}
                    <div>
                      <h3 className="font-semibold text-gray-900">{ann.title}</h3>
                      <p className="text-sm text-gray-500 mt-0.5">
                        {ann.audience === 'All' ? 'Everyone' : ann.audience} ·
                        {format(new Date(ann.createdAt), ' MMM d, yyyy')} ·
                        {' '}{ann.createdByName}
                        {ann.expiresAt && (
                          <span className={ann.isExpired ? ' text-red-400' : ' text-gray-400'}>
                            {' '}· Expires {format(new Date(ann.expiresAt), 'MMM d')}
                          </span>
                        )}
                      </p>
                    </div>
                  </div>
                  <button onClick={() => { if (confirm('Delete announcement?')) deleteAnnouncementMutation.mutate(ann.id); }}
                    className="text-xs text-red-400 hover:text-red-600 transition shrink-0">Delete</button>
                </div>
                <p className="mt-3 text-gray-700 text-sm leading-relaxed whitespace-pre-wrap">{ann.body}</p>
              </div>
            ))
          )}
        </div>
      )}

      {/* Compose Modal */}
      {showCompose && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg">
            <div className="flex items-center justify-between px-6 py-4 border-b">
              <h2 className="text-lg font-bold text-gray-900">Compose Message</h2>
              <button onClick={() => setShowCompose(false)}
                className="text-gray-400 hover:text-gray-600 text-2xl leading-none">&times;</button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Recipient User ID *</label>
                <input value={composeForm.recipientId}
                  onChange={e => setComposeForm(f => ({ ...f, recipientId: e.target.value }))}
                  placeholder="Paste recipient's User ID"
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                <p className="text-xs text-gray-400 mt-1">User ID lookup will be added in Phase 5 with search.</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Subject *</label>
                <input value={composeForm.subject}
                  onChange={e => setComposeForm(f => ({ ...f, subject: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Message *</label>
                <textarea value={composeForm.body} rows={5}
                  onChange={e => setComposeForm(f => ({ ...f, body: e.target.value }))}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
              </div>
            </div>
            <div className="flex justify-end gap-3 px-6 py-4 border-t bg-gray-50 rounded-b-2xl">
              <button onClick={() => setShowCompose(false)}
                className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800 transition">Cancel</button>
              <button onClick={() => sendMutation.mutate()} disabled={sendMutation.isPending}
                className="px-5 py-2 bg-blue-700 hover:bg-blue-800 text-white text-sm font-semibold rounded-lg transition disabled:opacity-50">
                {sendMutation.isPending ? 'Sending...' : 'Send'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}