import React, { useState, useEffect } from 'react';
import { Settings, Brain, FileText, MessageSquare, Plus } from 'lucide-react';
import { toast } from 'react-toastify';
import { useNavigate, useParams } from 'react-router-dom';
import api from '../services/api';

enum UploadTypeEnum {
    Knowledge = "Knowledge",
    Template = "Template"
};

interface SessionSummary {
    sessionId: string;
    title: string;
    content: string;
    updatedAt: string;
    userId: string;
}

const SideNavPanel: React.FC = () => {
    const navigate = useNavigate();
    const { sessionid: currentSessionId } = useParams<{ sessionid: string }>();
    
    // Existing state
    const [isSettingsOpen, setIsSettingsOpen] = useState(false);
    const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
    const [file, setFile] = useState<File | null>(null);
    const [description, setDescription] = useState('');
    const [UploadType, setUploadType] = useState<UploadTypeEnum>();
    const [isUploading, setIsUploading] = useState(false);
    
    // New state for chat management
    const [isChatPanelOpen, setIsChatPanelOpen] = useState(false);
    const [chatSessions, setChatSessions] = useState<SessionSummary[]>([]);
    const [isLoadingSessions, setIsLoadingSessions] = useState(false);

    // Load chat sessions when chat panel opens
    useEffect(() => {
        if (isChatPanelOpen) {
            loadChatSessions();
        }
    }, [isChatPanelOpen]);

    const loadChatSessions = async () => {
        setIsLoadingSessions(true);
        try {
            const response = await api.get('/chatsession');
            setChatSessions(response.data);
        } catch (error: unknown) {
            const errorMessage = typeof error === 'object' && error !== null && 'message' in error
                ? (error as { message?: string }).message
                : String(error);
            toast.error(`Failed to load chat sessions: ${errorMessage}`);
            setChatSessions([]);
        } finally {
            setIsLoadingSessions(false);
        }
    };

    const toggleSettingsPanel = () => {
        setIsSettingsOpen(!isSettingsOpen);
        if (isChatPanelOpen) setIsChatPanelOpen(false); // Close chat panel if open
    };

    const toggleChatPanel = () => {
        setIsChatPanelOpen(!isChatPanelOpen);
        if (isSettingsOpen) setIsSettingsOpen(false); // Close settings panel if open
    };

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files) {
            setFile(e.target.files[0]);
        }
    };

    const handleUpload = async () => {
        if (!file || (UploadType === UploadTypeEnum.Knowledge && !description.trim())) {
            toast.error("File and description are mandatory!");
            return;
        }

        const formData = new FormData();
        formData.append('File', file);
        formData.append('FileName', file.name);
        if (UploadType === UploadTypeEnum.Knowledge) {
            formData.append('FileDescription', description);
        }

        setIsUploading(true);
        try {
            const endpoint = UploadType === UploadTypeEnum.Knowledge
                ? `/Knowledge/Upload`
                : `/Knowledge/Upload/AddTemplate`;

            await api.post(endpoint, formData,{
                headers: {
                    'Content-Type': 'multipart/form-data',
                },
            });

            toast.success(`${UploadType} uploaded successfully!`);
            setIsUploadModalOpen(false);
            setFile(null);
            setDescription('');
            setIsSettingsOpen(false);
        } catch (error: unknown) {
            const errorMessage = typeof error === 'object' && error !== null && 'message' in error
                ? (error as { message?: string }).message
                : String(error);
            toast.error(`Upload failed: ${errorMessage}`);
        } finally {
            setIsUploading(false);
        }
    };

    const handleNewChat = () => {
        const newSessionId = crypto.randomUUID();
        localStorage.setItem("chatSessionId", newSessionId);
        navigate(`/chat/${newSessionId}`);
        setIsChatPanelOpen(false);
        toast.success("New chat session started!");
    };

    const handleChatSelect = (sessionId: string) => {
        localStorage.setItem("chatSessionId", sessionId);
        navigate(`/chat/${sessionId}`);
        setIsChatPanelOpen(false);
    };

    const formatDate = (dateString: string) => {
        const date = new Date(dateString);
        const now = new Date();
        const diffTime = Math.abs(now.getTime() - date.getTime());
        const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
        
        if (diffDays === 1) return 'Today';
        if (diffDays === 2) return 'Yesterday';
        if (diffDays <= 7) return `${diffDays - 1} days ago`;
        return date.toLocaleDateString();
    };

    const truncateTitle = (title: string, maxLength: number = 30) => {
        return title.length > maxLength ? `${title.substring(0, maxLength)}...` : title;
    };

    return (
        <div className="fixed top-16 left-0 h-full flex flex-col bg-gray-800 text-white w-16 p-2 border-r border-gray-700 z-40">
            {/* Chat Button */}
            <button 
                onClick={toggleChatPanel} 
                className={`p-3 rounded-lg hover:bg-gray-700 transition-all mb-2 ${isChatPanelOpen ? 'bg-gray-700' : ''}`}
                title="Chat History"
            >
                <MessageSquare size={24} />
            </button>

            {/* Settings Button */}
            <button 
                onClick={toggleSettingsPanel} 
                className={`p-3 rounded-lg hover:bg-gray-700 transition-all ${isSettingsOpen ? 'bg-gray-700' : ''}`}
                title="Settings"
            >
                <Settings size={24} />
            </button>

            {/* Chat Panel */}
            {isChatPanelOpen && (
                <div className="absolute left-16 top-0 w-80 h-full bg-gray-900 shadow-lg border-l border-gray-700 z-50 flex flex-col">
                    <div className="p-4 border-b border-gray-700">
                        <div className="flex items-center justify-between mb-4">
                            <h2 className="text-lg font-bold">Chat History</h2>
                            <button
                                onClick={handleNewChat}
                                className="flex items-center gap-2 px-3 py-2 bg-blue-600 rounded-lg hover:bg-blue-700 transition-all text-sm"
                                title="New Chat"
                            >
                                <Plus size={16} />
                                New Chat
                            </button>
                        </div>
                    </div>
                    
                    <div className="flex-1 overflow-y-auto p-4 mb-20">
                        {isLoadingSessions ? (
                            <div className="flex items-center justify-center py-8">
                                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-400"></div>
                                <span className="ml-3 text-gray-400">Loading chats...</span>
                            </div>
                        ) : chatSessions.length === 0 ? (
                            <div className="text-center py-8 text-gray-400">
                                <MessageSquare size={48} className="mx-auto mb-4 opacity-50" />
                                <p>No chat sessions yet</p>
                                <p className="text-sm mt-2">Start a new conversation!</p>
                            </div>
                        ) : (
                            <div className="space-y-2">
                                {chatSessions.map((session) => (
                                    <div
                                        key={session.sessionId}
                                        onClick={() => handleChatSelect(session.sessionId)}
                                        className={`p-3 rounded-lg cursor-pointer transition-all hover:bg-gray-700 border border-transparent ${
                                            currentSessionId === session.sessionId 
                                                ? 'bg-blue-600/20 border-blue-600' 
                                                : 'hover:border-gray-600'
                                        }`}
                                    >
                                        <div className="flex items-start justify-between">
                                            <div className="flex-1 min-w-0">
                                                <h3 className="font-medium text-sm mb-1 truncate" title={session.title}>
                                                    {session.title || 'New Chat'}
                                                </h3>
                                                {session.content && (
                                                    <p className="text-xs text-gray-400 line-clamp-2 mb-2">
                                                        {truncateTitle(session.content, 60)}
                                                    </p>
                                                )}
                                                <p className="text-xs text-gray-500">
                                                    {formatDate(session.updatedAt)}
                                                </p>
                                            </div>
                                            {currentSessionId === session.sessionId && (
                                                <div className="flex-shrink-0 ml-2">
                                                    <div className="w-2 h-2 bg-blue-400 rounded-full"></div>
                                                </div>
                                            )}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </div>
            )}

            {/* Settings Panel */}
            {isSettingsOpen && (
                <div className="absolute left-16 top-0 w-64 h-full bg-gray-900 shadow-lg p-4 border-l border-gray-700 z-50">
                    <h2 className="text-lg font-bold mb-4">Settings</h2>
                    <button
                        onClick={() => {
                            setIsUploadModalOpen(true);
                            setUploadType(UploadTypeEnum.Knowledge);
                        }}
                        className="flex items-center gap-2 p-3 bg-blue-600 rounded-lg hover:bg-blue-700 transition-all w-full mb-3"
                    >
                        <Brain size={20} /> Upload Knowledge
                    </button>
                    <button
                        onClick={() => {
                            setIsUploadModalOpen(true);
                            setUploadType(UploadTypeEnum.Template);
                        }}
                        className="flex items-center gap-2 p-3 bg-blue-600 rounded-lg hover:bg-blue-700 transition-all w-full"
                    >
                        <FileText size={20} /> Upload Template
                    </button>
                </div>
            )}

            {/* Upload Modal */}
            {isUploadModalOpen && (
                <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center animate-fadeIn z-50">
                    <div className="relative w-96">
                        <div className={`bg-gray-900 p-6 rounded-lg shadow-lg ${isUploading ? 'blur-sm pointer-events-none' : ''}`}>
                            <h2 className="text-lg font-bold mb-4">Upload {UploadType}</h2>
                            <input
                                type="file"
                                onChange={handleFileChange}
                                className="mb-4 w-full p-2 bg-gray-800 border border-gray-700 rounded-lg"
                            />
                            {UploadType === UploadTypeEnum.Knowledge && (
                                <textarea
                                    placeholder="Enter file description..."
                                    value={description}
                                    onChange={(e) => setDescription(e.target.value)}
                                    className="w-full p-2 bg-gray-800 border border-gray-700 rounded-lg mb-4"
                                />
                            )}
                            <div className="flex justify-end gap-2">
                                <button
                                    disabled={isUploading}
                                    onClick={handleUpload}
                                    className="p-2 bg-blue-600 rounded-lg hover:bg-blue-700 flex items-center justify-center min-w-[90px]"
                                >
                                    {isUploading ? (
                                        <div className="flex items-center gap-2">
                                            <svg
                                                className="animate-spin h-5 w-5 text-white"
                                                xmlns="http://www.w3.org/2000/svg"
                                                fill="none"
                                                viewBox="0 0 24 24"
                                            >
                                                <circle
                                                    className="opacity-25"
                                                    cx="12"
                                                    cy="12"
                                                    r="10"
                                                    stroke="currentColor"
                                                    strokeWidth="4"
                                                />
                                                <path
                                                    className="opacity-75"
                                                    fill="currentColor"
                                                    d="M4 12a8 8 0 018-8v8z"
                                                />
                                            </svg>
                                            <span className="text-sm">Uploading...</span>
                                        </div>
                                    ) : (
                                        'Upload'
                                    )}
                                </button>
                                <button
                                    onClick={() => {
                                        setIsUploadModalOpen(false);
                                        setIsSettingsOpen(false);
                                        setFile(null);
                                        setDescription('');
                                    }}
                                    className="p-2 bg-gray-700 rounded-lg hover:bg-gray-600"
                                >
                                    Cancel
                                </button>
                            </div>
                        </div>

                        {isUploading && (
                            <div className="absolute inset-0 flex flex-col items-center justify-center bg-gray-950 bg-opacity-80 backdrop-blur-md rounded-lg z-50">
                                <svg
                                    className="animate-spin h-8 w-8 text-blue-400"
                                    xmlns="http://www.w3.org/2000/svg"
                                    fill="none"
                                    viewBox="0 0 24 24"
                                >
                                    <circle
                                        className="opacity-25"
                                        cx="12"
                                        cy="12"
                                        r="10"
                                        stroke="currentColor"
                                        strokeWidth="4"
                                    />
                                    <path
                                        className="opacity-75"
                                        fill="currentColor"
                                        d="M4 12a8 8 0 018-8v8z"
                                    />
                                </svg>
                                <p className="text-white text-sm mt-2">Uploading file, please wait...</p>
                            </div>
                        )}
                    </div>
                </div>
            )}
        </div>
    );
};

export default SideNavPanel;