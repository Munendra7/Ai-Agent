import React, { useEffect, useState } from 'react';
import { Settings, Brain, FileText } from 'lucide-react';
import { toast } from 'react-toastify';
import axios from 'axios';
import { useMsal } from '@azure/msal-react';
import { backendAPILoginRequest } from '../authConfig';

const apiUrl = (import.meta as any).env.VITE_AIAgent_URL;

enum UploadTypeEnum {
    Knowledge = "Knowledge",
    Template = "Template"
};

const SideNavPanel: React.FC = () => {
    const [isOpen, setIsOpen] = useState(false);
    const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
    const [file, setFile] = useState<File | null>(null);
    const [description, setDescription] = useState('');
    const { instance } = useMsal();
    const activeAccount = instance.getActiveAccount();
    const [accessToken, setAccessToken] = useState<string | null>(null);
    const [UploadType, setUploadType] = useState<UploadTypeEnum>();
    const [isUploading, setIsUploading] = useState(false);

    const togglePanel = () => {
        setIsOpen(!isOpen);
    };

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files) {
            setFile(e.target.files[0]);
        }
    };

    const handleUpload = async () => {
        if (!accessToken) {
            toast.error("Access token not available");
            return;
        }

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
                ? `${apiUrl}/api/Knowledge/Upload`
                : `${apiUrl}/api/Knowledge/Upload/AddTemplate`;

            await axios.post(endpoint, formData, {
                headers: {
                    Authorization: `Bearer ${accessToken}`,
                    'Content-Type': 'multipart/form-data',
                },
            });

            toast.success(`${UploadType} uploaded successfully!`);
            setIsUploadModalOpen(false);
            setFile(null);
            setDescription('');
            setIsOpen(false);
        } catch (error: any) {
            toast.error(`Upload failed: ${error.message || error}`);
        } finally {
            setIsUploading(false);
        }
    };

    useEffect(() => {
        const fetchToken = async () => {
            try {
                const response = await instance.acquireTokenSilent({
                    ...backendAPILoginRequest,
                    account: activeAccount!,
                });
                setAccessToken(response.accessToken);
            } catch (error) {
                console.error("Token acquisition failed", error);
            }
        };

        if (activeAccount) {
            fetchToken();
        }
    }, [instance, activeAccount]);

    return (
        <div className="fixed top-16 left-0 h-full flex flex-col bg-gray-800 text-white w-16 p-2 border-r border-gray-700 z-40">
            <button onClick={togglePanel} className="p-3 rounded-lg hover:bg-gray-700 transition-all">
                <Settings size={24} />
            </button>

            {isOpen && (
                <div className="absolute left-16 top-0 w-64 h-full bg-gray-900 shadow-lg p-4 border-l border-gray-700 z-50">
                    <h2 className="text-lg font-bold mb-4">Settings</h2>
                    <button
                        onClick={() => {
                            setIsUploadModalOpen(true);
                            setUploadType(UploadTypeEnum.Knowledge);
                        }}
                        className="flex items-center gap-2 p-3 bg-blue-600 rounded-lg hover:bg-blue-700 transition-all w-full"
                    >
                        <Brain size={20} /> Upload Knowledge
                    </button>
                    <br />
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
                                        setIsOpen(false);
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