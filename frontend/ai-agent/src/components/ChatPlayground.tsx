import React, { useState, useRef, useEffect } from "react";
import { Send, Loader2, Bot } from "lucide-react";
import ReactMarkdown from "react-markdown";
import { useMsal } from "@azure/msal-react";
import { backendAPILoginRequest } from "../authConfig";

const apiUrl = (import.meta as any).env.VITE_AIAgent_URL;

const ChatPlayground: React.FC = () => {
  const { instance } = useMsal();
  const activeAccount = instance.getActiveAccount();
  const sessionId = useRef<string>(crypto.randomUUID());
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [messages, setMessages] = useState<
    { text: string; type: "user" | "bot"; persona: string; isLoading?: boolean }[]
  >([
    {
      text: `Hi ${activeAccount?.name}, how can I assist you?`,
      type: "bot",
      persona: "AI Agent",
    },
  ]);
  const [input, setInput] = useState("");
  const [isWaitingForResponse, setIsWaitingForResponse] = useState(false);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

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
  }, [activeAccount, instance]);

  const formatChatResponse = (text: string): string => {
    return text.replace(/- \*\*(.*?)\*\*/g, "\n- **`$1`**").replace(/ - /g, "\n- ").trim();
  };

  const fetchAgentStreamResponse = async (query: string) => {
    if (!accessToken) {
      console.error("Access token not available");
      return;
    }

    // Add initial bot message with thinking animation
    setMessages((prev) => [
      ...prev,
      { text: "", type: "bot", persona: "AI Agent", isLoading: true },
    ]);

    try {
      const response = await fetch(`${apiUrl}/api/Agent/StreamAgentChat`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${accessToken}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ sessionId: sessionId.current, query }),
      });

      if (!response.body) throw new Error("No response body");

      const reader = response.body.getReader();
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

          await new Promise((r) => setTimeout(r, 5)); // Typing animation speed
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

  const handleSendMessage = () => {
    if (input.trim() === "") return;

    const currentInput = input;
    setInput(""); // Clear immediately
    setIsWaitingForResponse(true); // Disable textarea immediately

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
            </div>
          </div>
        ))}
        <div ref={messagesEndRef} />
      </div>

      <div className="flex items-center bg-gray-800 p-4 rounded-lg shadow-lg border border-gray-700">
        <textarea
          rows={4}
          maxLength={3000}
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