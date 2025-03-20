import React, { useState, useRef, useEffect } from 'react';
import { Send } from 'lucide-react';
import axios from 'axios';
import ReactMarkdown from 'react-markdown';
import { useMsal } from '@azure/msal-react';

const apiUrl = (import.meta as any).env.VITE_AIAgent_URL;

const ChatPlayground: React.FC = () => {
    const { accounts } = useMsal();
    const sessionId = useRef<string>('');
    const [messages, setMessages] = useState<{ text: string; type: 'user' | 'bot'; persona: string }[]>([{ text: `Hi ${accounts.length>0?accounts[0]?.name??"":""}, how can i assist you?`, type: 'bot' as 'bot', persona: 'AI Agent' }]);
    const [input, setInput] = useState('');
    const [isWaitingForResponse, setIsWaitingForResponse] = useState(false);
    const messagesEndRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    useEffect(() => {
        sessionId.current = crypto.randomUUID();
    }, []);

    function formatChatResponse(text: string): string {
        return text
          .replace(/- \*\*(.*?)\*\*/g, "\n- **`$1`**") // Ensure backticks around commands
          .replace(/ - /g, "\n- ") // Ensure new lines for each list item
          .trim();
    }

    const fetchAgentResponse = async()=>{
        setIsWaitingForResponse(true);
        try{
            const response = await axios.post(`${apiUrl}/api/chat`,
                { 
                    sessionId: sessionId.current, 
                    query: input 
                },
                {
                    headers: {
                        "Content-Type": "application/json"
                    }
                }
            );
            const data = await response.data;
            const botResponse = { text: formatChatResponse(data.response), type: 'bot' as 'bot', persona: 'AI Agent' };
            setMessages(prev => [...prev, botResponse]);
            setInput('');
            setIsWaitingForResponse(false);
        }catch(error:any){
            const errorMessage = { text: error.message, type: 'bot' as 'bot', persona: 'AI Agent' };
            setMessages(prev => [...prev, errorMessage]);
            setInput('');
            setIsWaitingForResponse(false);
        }
    }

    const handleSendMessage = () => {
        if (input.trim() === '') return;

        const userMessage = { text: input, type: 'user' as 'user', persona: 'You' };
        setMessages([...messages, userMessage]);
        fetchAgentResponse();
    };

    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Enter' && !isWaitingForResponse) {
            handleSendMessage();
        }
    };

    return (
        <div className="flex flex-col h-screen pt-16 ml-16 bg-gray-900 text-white p-4">
            <div className="flex-1 overflow-y-auto space-y-4 p-4 custom-scrollbar">
                {messages.map((msg, index) => (
                    <div
                        key={index}
                        className={`max-w-lg p-4 rounded-xl shadow-lg ${msg.type === 'user' ? 'bg-blue-500 text-white self-start text-left' : 'bg-gray-800 text-gray-200 self-end ml-auto'}`}
                    >
                        <span className="block font-bold mb-1">{msg.persona}</span>
                        <ReactMarkdown>{msg.text}</ReactMarkdown>
                    </div>
                ))}
                <div ref={messagesEndRef} />
            </div>
            <div className="flex items-center bg-gray-800 p-4 rounded-lg shadow-lg border border-gray-700">
                <input
                    type="text"
                    className="flex-1 p-3 bg-gray-900 outline-none text-white placeholder-gray-400 rounded-lg shadow-md focus:ring-2 focus:ring-blue-500"
                    placeholder="Type a message..."
                    value={input}
                    onChange={(e) => setInput(e.target.value)}
                    onKeyDown={handleKeyDown}
                    disabled={isWaitingForResponse}
                />
                <button
                    className="ml-4 p-3 bg-blue-600 rounded-full hover:bg-blue-700 transition-all shadow-md flex items-center justify-center w-12 h-12"
                    onClick={handleSendMessage}
                    disabled={isWaitingForResponse}
                >
                    <Send size={20} />
                </button>
            </div>
        </div>
    );
};

export default ChatPlayground;