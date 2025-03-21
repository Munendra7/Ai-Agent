import React, { useState, useRef, useEffect } from "react";
import { Send, Loader2, Bot } from "lucide-react";
import axios from "axios";
import ReactMarkdown from "react-markdown";
import { useMsal } from "@azure/msal-react";

const apiUrl = (import.meta as any).env.VITE_AIAgent_URL;

const ChatPlayground: React.FC = () => {
  const { accounts } = useMsal();
  const sessionId = useRef<string>("");
  const [messages, setMessages] = useState<{ text: string; type: "user" | "bot"; persona: string; isLoading?: boolean }[]>
  ([
    {
      text: `Hi ${accounts.length > 0 ? accounts[0]?.name ?? "" : ""}, how can I assist you?`,
      type: "bot",
      persona: "AI Agent",
    },
  ]);
  const [input, setInput] = useState("");
  const [isWaitingForResponse, setIsWaitingForResponse] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  useEffect(() => {
    sessionId.current = crypto.randomUUID();
  }, []);

  function formatChatResponse(text: string): string {
    return text.replace(/- \*\*(.*?)\*\*/g, "\n- **`$1`**").replace(/ - /g, "\n- ").trim();
  }

  const fetchAgentResponse = async () => {
    setIsWaitingForResponse(true);
    setMessages((prev) => [...prev, { text: "", type: "bot", persona: "AI Agent", isLoading: true }]);

    try {
      const response = await axios.post(
        `${apiUrl}/api/chat`,
        { sessionId: sessionId.current, query: input },
        { headers: { "Content-Type": "application/json" } }
      );
      setMessages((prev) => [...prev.slice(0, -1), { text: formatChatResponse(response.data.response), type: "bot", persona: "AI Agent" }]);
    } catch (error: any) {
      setMessages((prev) => [...prev.slice(0, -1), { text: error.message, type: "bot", persona: "AI Agent" }]);
    } finally {
      setInput("");
      setIsWaitingForResponse(false);
    }
  };

  const handleSendMessage = () => {
    if (input.trim() === "") return;
    setMessages([...messages, { text: input, type: "user", persona: "You" }]);
    fetchAgentResponse();
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter" && !isWaitingForResponse) {
      handleSendMessage();
    }
  };

  return (
    <div className="flex flex-col h-screen pt-16 ml-16 bg-gray-900 text-white p-4">
      <div className="flex-1 overflow-y-auto space-y-4 p-4 custom-scrollbar">
        {messages.map((msg, index) => (
          <div
            key={index}
            className={`flex items-start max-w-lg p-4 rounded-xl shadow-md transition-all duration-300 
              ${msg.type === "bot" ? "bg-gradient-to-r from-indigo-600 to-blue-500 text-white self-start text-left" : "bg-gray-700 text-gray-200 self-end ml-auto"}`}
          >
            {msg.type === "bot" && (
              <div className="flex items-center justify-center w-8 h-8 rounded-full bg-opacity-20 mr-2">
                <Bot size={24} className="text-white" />
              </div>
            )}
            <div>
              <span className="block font-semibold mb-1">{msg.persona}</span>
              {msg.isLoading ? (
                <div className="flex items-center space-x-2">
                  <Loader2 className="animate-spin text-white" size={20} />
                  <span>Thinking...</span>
                </div>
              ) : (
                <ReactMarkdown>{msg.text}</ReactMarkdown>
              )}
            </div>
          </div>
        ))}
        <div ref={messagesEndRef} />
      </div>

      <div className="flex items-center bg-gray-800 p-4 rounded-lg shadow-lg border border-gray-700">
        <input
          type="text"
          maxLength={499}
          className="flex-1 p-3 bg-gray-900 outline-none text-white placeholder-gray-400 rounded-lg shadow-md focus:ring-2 focus:ring-blue-500"
          placeholder="Type a message..."
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={isWaitingForResponse}
        />
        <button
          className={`ml-4 p-3 rounded-full transition-all shadow-md flex items-center justify-center w-12 h-12 
            ${isWaitingForResponse ? "bg-gray-500" : "bg-blue-600 hover:bg-blue-700"}`}
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