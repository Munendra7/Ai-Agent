import React, { useState, useRef, useEffect } from "react";
import { Send, Loader2, Bot } from "lucide-react";
import axios from "axios";
import ReactMarkdown from "react-markdown";
import { useMsal } from "@azure/msal-react";
import { backendAPILoginRequest } from "../authConfig";

const apiUrl = (import.meta as any).env.VITE_AIAgent_URL;

const ChatPlayground: React.FC = () => {
  const { instance} = useMsal();
  const activeAccount = instance.getActiveAccount();
  const sessionId = useRef<string>(crypto.randomUUID());
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [messages, setMessages] = useState<{ text: string; type: "user" | "bot"; persona: string; isLoading?: boolean }[]>
  ([
    {
      text: `Hi ${activeAccount?.name}, how can I assist you?`,
      type: "bot",
      persona: "AI Agent",
    },
  ]);
  const [input, setInput] = useState("");
  const [isWaitingForResponse, setIsWaitingForResponse] = useState(false);

  // Scroll to latest message
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  // Fetch Access Token Once
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
  }, []);

  function formatChatResponse(text: string): string {
    return text.replace(/- \*\*(.*?)\*\*/g, "\n- **`$1`**").replace(/ - /g, "\n- ").trim();
  }

  const fetchAgentResponse = async () => {
    if (!accessToken) {
      console.error("Access token not available");
      return;
    }

    setIsWaitingForResponse(true);
    setMessages((prev) => [...prev, { text: "", type: "bot", persona: "AI Agent", isLoading: true }]);

    try {
      const response = await axios.post(
        `${apiUrl}/api/Agent/SingleAgentChat`,
        { sessionId: sessionId.current, query: input },
        {
          headers: {
            Authorization: `Bearer ${accessToken}`,
            "Content-Type": "application/json",
          },
        }
      );

      setMessages((prev) => [
        ...prev.slice(0, -1),
        { text: formatChatResponse(response.data.response), type: "bot", persona: "AI Agent" },
      ]);
    } catch (error: any) {
      console.error("Error fetching agent response", error);
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

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey && !isWaitingForResponse) {
      handleSendMessage();
    }
  };

  return (
    <div className="flex flex-col h-screen pt-16 ml-16 bg-gray-900 text-white p-4">
      <div className="flex-1 overflow-y-auto space-y-4 p-4 custom-scrollbar">
        {messages.map((msg, index) => (
          <div
            key={index}
            className={`flex items-start overflow-x-auto custom-scrollbar p-4 rounded-xl shadow-md transition-all duration-300 
              ${msg.type === "bot" ? "bg-gray-800 text-white self-start text-left max-w-max" : "bg-blue-600 text-white self-end ml-auto max-w-lg"}`}
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
                <ReactMarkdown>{msg.text.replace(/\n/g, "  \n")}</ReactMarkdown>
              )}
            </div>
          </div>
        ))}
        <div ref={messagesEndRef} />
      </div>

      <div className="flex items-center bg-gray-800 p-4 rounded-lg shadow-lg border border-gray-700">
        <textarea
          rows={2}
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