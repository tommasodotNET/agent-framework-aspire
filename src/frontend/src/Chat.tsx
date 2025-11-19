// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Button } from "@fluentui/react-components";
import { useEffect, useId, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import TextareaAutosize from "react-textarea-autosize";
import styles from "./Chat.module.css";
import gfm from "remark-gfm";
import { A2AClientWrapper, A2AChatMessage } from "./A2AClientWrapper.ts";

type ChatEntry = A2AChatMessage | ChatError;
type Theme = 'light' | 'dark' | 'system';
type AgentType = 'dotnet' | 'python' | 'groupchat';

interface ChatError {
    code: string;
    message: string;
}

function isChatError(entry: unknown): entry is ChatError {
    return (entry as ChatError).code !== undefined;
}

const AGENTS: Record<AgentType, { name: string; endpoint: string; icon: string }> = {
    dotnet: { name: '.NET Agent (Documents)', endpoint: '/agenta2a/dotnet/v1/card', icon: '📄' },
    python: { name: 'Python Agent (Financial)', endpoint: '/agenta2a/python/.well-known/agent-card.json', icon: '📊' },
    groupchat: { name: 'Group Chat (Multi-Agent)', endpoint: '/agenta2a/groupchat/v1/card', icon: '👥' },
};

export default function Chat({ style }: { style: React.CSSProperties }) {
    const [selectedAgent, setSelectedAgent] = useState<AgentType>('dotnet');
    // Initialize A2A client with the orchestrator agent card URL
    const [client, setClient] = useState(() => new A2AClientWrapper(AGENTS.dotnet.endpoint));

    const [messages, setMessages] = useState<ChatEntry[]>([]);
    const [input, setInput] = useState<string>("");
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasInvokedInitialAgent, setHasInvokedInitialAgent] = useState<boolean>(false);
    const inputId = useId();
    // Use contextId for conversation management (A2A terminology)
    const [contextId, setContextId] = useState<string | undefined>(undefined);
    const [theme, setTheme] = useState<Theme>('system');
    const [effectiveTheme, setEffectiveTheme] = useState<'light' | 'dark'>('light');
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const initialFetchStarted = useRef(false);

    const invokeAgentWithEmptyMessage = async () => {
        if (isLoading || !contextId) return;
        
        setIsLoading(true);
        try {
            // Send an initial message to get the greeting
            const initialMessage: A2AChatMessage = { 
                role: "user", 
                content: "Hello" 
            };
            
            const latestMessage: A2AChatMessage = { content: "", role: "assistant" };
            
            for await (const event of client.sendMessageStream([initialMessage], contextId)) {
                if (event.contextId) {
                    setContextId(event.contextId);
                }
                if (event.content) {
                    latestMessage.content += event.content;
                    setMessages([latestMessage]);
                }
            }
        } catch (e) {
            console.log("ERROR: ", e);
            if (isChatError(e)) {
                setMessages([e]);
            } else {
                setMessages([
                    { code: "unknown_error", message: String(e) },
                ]);
            }
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        // Generate initial contextId if not present
        if (!contextId && !initialFetchStarted.current) {
            const newContextId = crypto.randomUUID();
            setContextId(newContextId);
            initialFetchStarted.current = true;
        }
    }, [contextId]);

    // Invoke agent with empty message when context is ready and agent hasn't been invoked yet
    useEffect(() => {
        if (contextId && !hasInvokedInitialAgent && !isLoading) {
            setHasInvokedInitialAgent(true);
            invokeAgentWithEmptyMessage();
        }
    }, [contextId, hasInvokedInitialAgent, isLoading]);

    // Load saved theme
    useEffect(() => {
        const savedTheme = localStorage.getItem('theme') as Theme;
        if (savedTheme && ['light', 'dark', 'system'].includes(savedTheme)) {
            setTheme(savedTheme);
        }
    }, []);

    // Handle theme changes and system preference
    useEffect(() => {
        const updateEffectiveTheme = () => {
            if (theme === 'system') {
                const systemPrefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
                setEffectiveTheme(systemPrefersDark ? 'dark' : 'light');
            } else {
                setEffectiveTheme(theme);
            }
        };

        updateEffectiveTheme();
        localStorage.setItem('theme', theme);

        const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
        if (theme === 'system') {
            mediaQuery.addEventListener('change', updateEffectiveTheme);
            return () => mediaQuery.removeEventListener('change', updateEffectiveTheme);
        }
    }, [theme]);

    // Reset conversation with new contextId
    const handleResetConversation = () => {
        const newContextId = crypto.randomUUID();
        setContextId(newContextId);
        setMessages([]);
        setHasInvokedInitialAgent(false);
        initialFetchStarted.current = false;
    };

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    };
    useEffect(scrollToBottom, [messages]);

    const sendMessage = async () => {
        if (!input.trim() || isLoading) return;
        
        const message: A2AChatMessage = {
            role: "user",
            content: input,
        };
        const updatedMessages: ChatEntry[] = [...messages, message];
        setMessages(updatedMessages);
        setInput("");
        setIsLoading(true);
        
        // Add a placeholder assistant message that will be updated
        const assistantMessage: A2AChatMessage = { content: "", role: "assistant" };
        setMessages([...updatedMessages, assistantMessage]);
        
        try {
            // Build the conversation from updatedMessages, filtering out errors
            const conversation = updatedMessages
                .filter((entry) => !isChatError(entry))
                .map((msg) => msg as A2AChatMessage);

            let accumulatedContent = "";
            
            // Stream the response using A2A protocol
            for await (const event of client.sendMessageStream(conversation, contextId)) {
                if (event.contextId) {
                    setContextId(event.contextId);
                }
                if (event.content) {
                    accumulatedContent += event.content;
                    const updatedAssistantMessage: A2AChatMessage = {
                        content: accumulatedContent,
                        role: "assistant"
                    };
                    setMessages([...updatedMessages, updatedAssistantMessage]);
                }
            }
        } catch (e) {
            console.log("ERROR: ", e);

            if (isChatError(e)) {
                setMessages([...updatedMessages, e]);
            }
            else {
                setMessages([
                    ...updatedMessages,
                    { code: "unknown_error", message: String(e) },
                ]);
            }
        } finally {
            setIsLoading(false);
        }
    };

    const getClassName = (message: ChatEntry) => {
        if (isChatError(message)) {
            return styles.caution;
        }
        return message.role === "user"
            ? styles.userMessage
            : styles.assistantMessage;
    };

    const getErrorMessage = (message: ChatError) => {
        return `${message.code}: ${message.message}`;
    };

    const handleThemeChange = (newTheme: Theme) => {
        setTheme(newTheme);
    };

    const handleAgentChange = (agent: AgentType) => {
        setSelectedAgent(agent);
        setClient(new A2AClientWrapper(AGENTS[agent].endpoint));
        handleResetConversation();
    };

    return (
        <div className={`${styles.chatWindow} ${effectiveTheme === 'dark' ? styles.dark : ''}`} style={style}>
            {/* Header with sessionState and reset button */}
            <div className={styles.header}>
                <h1 className={styles.headerTitle}>City Assistant</h1>
                <div className={styles.headerContent}>
                    <div className={styles.agentSelector}>
                        <label className={styles.agentLabel}>Agent:</label>
                        <select 
                            value={selectedAgent} 
                            onChange={(e) => handleAgentChange(e.target.value as AgentType)}
                            className={styles.agentDropdown}
                        >
                            {Object.entries(AGENTS).map(([key, { name, icon }]) => (
                                <option key={key} value={key}>{icon} {name}</option>
                            ))}
                        </select>
                    </div>
                    <div className={styles.themeSelector}>
                        <button 
                            className={`${styles.themeButton} ${theme === 'light' ? styles.active : ''}`}
                            onClick={() => handleThemeChange('light')}
                            title="Light theme"
                        >
                            ☀️
                        </button>
                        <button 
                            className={`${styles.themeButton} ${theme === 'system' ? styles.active : ''}`}
                            onClick={() => handleThemeChange('system')}
                            title="System theme"
                        >
                            💻
                        </button>
                        <button 
                            className={`${styles.themeButton} ${theme === 'dark' ? styles.active : ''}`}
                            onClick={() => handleThemeChange('dark')}
                            title="Dark theme"
                        >
                            🌙
                        </button>
                    </div>
                    
                    <div className={styles.sessionInfo}>
                        <label className={styles.sessionLabel}>Context:</label>
                        <input
                            type="text"
                            value={contextId || ''}
                            onChange={(e) => setContextId(e.target.value)}
                            placeholder="Enter or generate context ID..."
                            className={styles.sessionInput}
                        />
                        <Button onClick={handleResetConversation} className={styles.resetButton}>
                            🔄 Reset
                        </Button>
                    </div>
                </div>
            </div>
            <div className={styles.messages}>
                {messages.length === 0 && !isLoading && (
                    <div className={styles.welcomeMessage}>
                        <div className={styles.welcomeIcon}>🤖</div>
                        <h2>Welcome to Agent Framework demo!</h2>
                        <p>Start chatting with me!</p>
                    </div>
                )}
                {messages.map((message, index) => (
                    <div key={`message-${index}`} className={getClassName(message)}>
                        {isChatError(message) ? (
                            <>{getErrorMessage(message)}</>
                        ) : (
                            <>
                                <div className={styles.messageIcon}>
                                    {message.role === 'user' ? '👤' : '🤖'}
                                </div>
                                <div className={styles.messageBubble}>
                                    <ReactMarkdown remarkPlugins={[gfm]}>
                                        {message.content}
                                    </ReactMarkdown>
                                </div>
                            </>
                        )}
                    </div>
                ))}
                {isLoading && (
                    <div className={styles.assistantMessage}>
                        <div className={styles.messageIcon}>
                            🤖
                        </div>
                        <div className={styles.messageBubble}>
                            <div className={styles.typingIndicator}>
                                <span>AI is thinking</span>
                                <div className={styles.typingDots}>
                                    <span></span>
                                    <span></span>
                                    <span></span>
                                </div>
                            </div>
                        </div>
                    </div>
                )}
                <div ref={messagesEndRef} />
            </div>
            <div className={styles.inputArea}>
                <div className={styles.inputContainer}>
                    <TextareaAutosize
                        id={inputId}
                        className={styles.inputField}
                        value={input}
                        onChange={(e) => setInput(e.target.value)}
                        onKeyDown={(e) => {
                            if (e.key === "Enter" && e.shiftKey && !isLoading && input.trim()) {
                                e.preventDefault();
                                sendMessage();
                            }
                        }}
                        minRows={1}
                        maxRows={4}
                        placeholder="Type your message here... (Shift+Enter to send)"
                    />
                    <Button onClick={sendMessage} className={styles.sendButton} disabled={isLoading || !input.trim()}>
                        {isLoading ? "⋯" : "➤"}
                    </Button>
                </div>
            </div>
        </div>
    );
}