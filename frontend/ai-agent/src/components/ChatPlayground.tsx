import React, { useState, useRef, useEffect } from "react";
import { Send, Loader2, Bot } from "lucide-react";
import ReactMarkdown from "react-markdown";
import "./ChatPlayground.css";
import { useAppSelector } from "../app/hooks";
import api from '../services/api';
import { useNavigate, useParams } from "react-router-dom";
import { fetchWithInterceptors } from "../services/fetchClient";

const starterPrompts = [
  "List all documents in your knowledge base",
  "Use your knowledge base to answer my query",
  "Summarize the document",
  "Search the Internet to answer my query",
  "How is the weather?",
  "List all templates you have and create a doc for me",
  "Draft and send an email",
];

const guidRegex =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

interface ChatMessage {
  text: string;
  type: "user" | "bot";
  persona: string;
  isLoading?: boolean;
  timestamp?: string;
}

interface ChatHistoryResponse {
  data: Array<{
    message: string;
    sender: string;
  }>; 
}

const ChatPlayground: React.FC = () => {
  const sessionId = useParams<{ sessionid: string }>().sessionid;
  const navigate = useNavigate();
  const {user} = useAppSelector(state => state.auth);

  const messagesEndRef = useRef<HTMLDivElement>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const [isWaitingForResponse, setIsWaitingForResponse] = useState(false);
  const [isLoadingHistory, setIsLoadingHistory] = useState(true);

  // Scroll to bottom when messages change
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  // Load chat history when sessionId is available
  useEffect(() => {
    const loadChatHistory = async () => {
      if (!sessionId || !guidRegex.test(sessionId)) {
        return;
      }

      setIsLoadingHistory(true);
      
      try {
        const response = await api.get(`/chatsession/${sessionId}`) as ChatHistoryResponse;
        if (response && response.data && response.data.length > 0) {
          const historicalMessages: ChatMessage[] = response.data.map(msg => ({
            text: msg.message,
            type: msg.sender==="Assistant" ? "bot":"user",
            persona: msg.sender=="Assistant" ? "AI Agent":"You"
          }));
          setMessages(historicalMessages);
        } else {
          setMessages([
            {
              text: `Hi ${user?.firstName || "there"}, how can I assist you?`,
              type: "bot",
              persona: "AI Agent",
            },
          ]);
        }
      } catch (error) {
        console.error("Error loading chat history:", error);
        // On error, show welcome message
        setMessages([
          {
            text: `Hi ${user?.firstName || "there"}, how can I assist you?`,
            type: "bot",
            persona: "AI Agent",
          },
        ]);
      } finally {
        setIsLoadingHistory(false);
      }
    };

    loadChatHistory();
  }, [sessionId, user]);

  // Handle session ID validation and navigation
  useEffect(() => {
    if (!sessionId || !guidRegex.test(sessionId)) {
      const storedId = localStorage.getItem("chatSessionId");
      if (storedId && guidRegex.test(storedId)) {
        navigate(`/chat/${storedId}`, { replace: true });
      } else {
        const newId = crypto.randomUUID();
        localStorage.setItem("chatSessionId", newId);
        navigate(`/chat/${newId}`, { replace: true });
      }
    } else {
      // Valid sessionId, update localStorage
      localStorage.setItem("chatSessionId", sessionId);
    }
  }, [navigate, sessionId]);

  const formatChatResponse = (text: string): string => {
    return text.replace(/- \*\*(.*?)\*\*/g, "\n- **`$1`**").replace(/ - /g, "\n- ").trim();
  };

  const fetchAgentStreamResponse = async (query: string) => {
    setMessages((prev) => [
      ...prev,
      { text: "", type: "bot", persona: "AI Agent", isLoading: true },
    ]);

    try {
      const stream = await fetchWithInterceptors<ReadableStream<Uint8Array>>(
        "/Agent/StreamAgentChat",
        { method: "POST", body: JSON.stringify({ sessionId: sessionId, query }), stream: true }
      );
      if (!stream) throw new Error("No response from server");

      const reader = stream.getReader();
      const decoder = new TextDecoder();
      let fullText = "";
      let isFirstChunk = true;

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        const chunk = decoder.decode(value, { stream: true });

        for (const char of chunk) {
          fullText += char;

          setMessages((prev) => {
            const updated = [...prev];
            const lastIndex = updated.length - 1;

            if (isFirstChunk && updated[lastIndex].isLoading) {
              updated[lastIndex].isLoading = false;
              isFirstChunk = false;
            }

            updated[lastIndex] = {
              ...updated[lastIndex],
              text: formatChatResponse(fullText),
            };

            return updated;
          });
        }
      }
    } catch (error: any) {
      console.error("Error streaming agent response", error);
      setMessages((prev) => [
        ...prev.slice(0, -1),
        { text: error.message, type: "bot", persona: "AI Agent" },
      ]);
    } finally {
      setIsWaitingForResponse(false);
    }
  };

  const handleSendMessage = (customInput?: string) => {
    const currentInput = customInput || input;
    if (currentInput.trim() === "") return;

    if (!customInput) setInput(""); // clear only if typed manually
    setIsWaitingForResponse(true);

    setMessages((prev) => [
      ...prev,
      { text: currentInput, type: "user", persona: "You" },
    ]);

    fetchAgentStreamResponse(currentInput);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey && !isWaitingForResponse) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  // Show loading state while fetching history
  if (isLoadingHistory) {
    return (
      <div className="chat-container flex flex-col h-screen pt-16 ml-16 p-4">
        <div className="flex-1 flex items-center justify-center">
          <div className="flex items-center space-x-3">
            <Loader2 className="animate-spin text-white" size={24} />
            <span className="text-white text-lg">Loading chat history...</span>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="chat-container flex flex-col h-screen pt-16 ml-16 p-4">
      <div className="flex-1 overflow-y-auto space-y-4 p-4">
        {messages.map((msg, index) => (
          <div
            key={index}
            className={`glass-bubble ${msg.type === "bot" ? "bubble-bot" : "bubble-user"}`}
          >
            {msg.type === "bot" && (
              <div className="flex items-center justify-center w-8 h-8 rounded-full bg-white/20 mr-3">
                <Bot size={20} className="text-white" />
              </div>
            )}
            <div className="break-words w-full">
              <span className="block font-semibold mb-1">{msg.persona}</span>
              {msg.isLoading ? (
                <div className="flex items-center space-x-2">
                  <Loader2 className="animate-spin text-white" size={20} />
                  <span>Thinking...</span>
                </div>
              ) : (
                <ReactMarkdown
                  components={{
                    a: (props) => (
                      <a
                        {...props}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="underline text-blue-400 hover:text-blue-300"
                      />
                    ),
                  }}
                >
                  {msg.text.replace(/\n/g, "  \n")}
                </ReactMarkdown>
              )}
              {msg.timestamp && (
                <span className="text-xs text-white/60 mt-1 block">
                  {new Date(msg.timestamp).toLocaleString()}
                </span>
              )}
            </div>
          </div>
        ))}

        {/* Starter Prompts UI - Only show when no user messages and history is loaded */}
        {!messages.some((m) => m.type === "user") && (
          <div className="mt-6">
            <h3 className="text-lg font-semibold mb-3 text-white">
              Try asking me:
            </h3>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
              {starterPrompts.map((prompt, i) => (
                <button
                  key={i}
                  onClick={() => handleSendMessage(prompt)}
                  className="p-4 rounded-2xl bg-white/10 text-white hover:bg-white/20 transition shadow-sm text-left"
                >
                  {prompt}
                </button>
              ))}
            </div>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* Input Box */}
      <div className="glass-input flex items-center mt-4 gap-4">
        <textarea
          rows={3}
          maxLength={3000}
          className="chat-textarea"
          placeholder="Type a message..."
          value={input}
          onChange={(e) => setInput(e.target.value)}
          onKeyDown={handleKeyDown}
          disabled={isWaitingForResponse || isLoadingHistory}
        />
        <button
          className={`p-3 rounded-full flex items-center justify-center w-12 h-12 transition-all duration-200 
            ${(isWaitingForResponse || isLoadingHistory) ? "bg-gray-600 cursor-not-allowed" : "bg-blue-600 hover:bg-blue-700"}`}
          onClick={() => handleSendMessage()}
          disabled={isWaitingForResponse || isLoadingHistory}
        >
          <Send size={20} />
        </button>
      </div>
    </div>
  );
};

export default ChatPlayground;