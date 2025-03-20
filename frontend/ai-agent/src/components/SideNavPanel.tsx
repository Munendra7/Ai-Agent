import React, { useState } from 'react';
import { Settings, Upload } from 'lucide-react';
import { toast } from 'react-toastify';
import axios from 'axios';

const apiUrl = (import.meta as any).env.VITE_AIAgent_URL;

const SideNavPanel: React.FC = () => {
    const [isOpen, setIsOpen] = useState(false);
    const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
    const [file, setFile] = useState<File | null>(null);
    const [description, setDescription] = useState('');
    const togglePanel = () => {
        setIsOpen(!isOpen);
    };

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files) {
            setFile(e.target.files[0]);
        }
    };

    const handleUpload = async () => {
        if (!file || !description.trim()) {
            toast.error("File and description are mandatory!");
            return;
        }

        const formData = new FormData();
        formData.append('File', file);
        formData.append('FileName', file.name);
        formData.append('FileDescription', description);

        try {
            await axios.post(`${apiUrl}/api/Knowledge/Upload`, formData, {
                headers: {
                    'Content-Type': 'multipart/form-data',
                },
            });
            toast.success("File uploaded successfully!");
            setIsUploadModalOpen(false);
            setFile(null);
            setDescription('');
            setIsOpen(false);
        } catch (error) {
            toast.error("File upload failed!");
        }
    };

    return (
        <div className="fixed top-16 left-0 h-full flex flex-col bg-gray-800 text-white w-16 p-2 border-r border-gray-700">
            <button onClick={togglePanel} className="p-3 rounded-lg hover:bg-gray-700 transition-all">
                <Settings size={24} />
            </button>
            {isOpen && (
                <div className="absolute left-16 top-0 w-64 h-full bg-gray-900 shadow-lg p-4 border-l border-gray-700">
                    <h2 className="text-lg font-bold mb-4">Settings</h2>
                    <button onClick={() => setIsUploadModalOpen(true)} className="flex items-center gap-2 p-3 bg-blue-600 rounded-lg hover:bg-blue-700 transition-all w-full">
                        <Upload size={20} /> Upload Knowledge
                    </button>
                </div>
            )}

            {isUploadModalOpen && (
                <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center">
                    <div className="bg-gray-900 p-6 rounded-lg shadow-lg w-96">
                        <h2 className="text-lg font-bold mb-4">Upload Knowledge</h2>
                        <input type="file" onChange={handleFileChange} className="mb-4 w-full p-2 bg-gray-800 border border-gray-700 rounded-lg" />
                        <textarea 
                            placeholder="Enter file description..." 
                            value={description} 
                            onChange={(e) => setDescription(e.target.value)}
                            className="w-full p-2 bg-gray-800 border border-gray-700 rounded-lg mb-4"
                        ></textarea>
                        <div className="flex justify-end gap-2">
                            <button onClick={() => {setIsUploadModalOpen(false); setIsOpen(false);}} className="p-2 bg-gray-700 rounded-lg hover:bg-gray-600">Cancel</button>
                            <button onClick={handleUpload} className="p-2 bg-blue-600 rounded-lg hover:bg-blue-700">Upload</button>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};

export default SideNavPanel;