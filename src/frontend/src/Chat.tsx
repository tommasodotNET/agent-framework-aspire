// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Button } from "@fluentui/react-components";
import {
    AIChatMessage,
    AIChatProtocolClient,
    AIChatError,
} from "@microsoft/ai-chat-protocol";
import { useEffect, useId, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import TextareaAutosize from "react-textarea-autosize";
import styles from "./Chat.module.css";
import gfm from "remark-gfm";


type ChatEntry = (AIChatMessage & { dataUrl?: string }) | AIChatError;
type ApiType = 'dotnet' | 'python' | 'groupchat';
type Theme = 'light' | 'dark' | 'system';

function isChatError(entry: unknown): entry is AIChatError {
    return (entry as AIChatError).code !== undefined;
}

export default function Chat({ style }: { style: React.CSSProperties }) {
    const [selectedApi, setSelectedApi] = useState<ApiType>('dotnet');
    const [client, setClient] = useState(() => new AIChatProtocolClient("/agent/dotnet/chat/"));

    const [messages, setMessages] = useState<ChatEntry[]>([]);
    const [input, setInput] = useState<string>("");
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasInvokedInitialAgent, setHasInvokedInitialAgent] = useState<boolean>(false);
    const inputId = useId();
    // Set initial sessionState to undefined
    const [sessionState, setSessionState] = useState<string | undefined>(undefined);
    const [theme, setTheme] = useState<Theme>('system');
    const [effectiveTheme, setEffectiveTheme] = useState<'light' | 'dark'>('light');
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const initialFetchStarted = useRef(false); // <--- aggiungi questa ref

    const invokeAgentWithEmptyMessage = async () => {
        if (isLoading || !sessionState) return;
        
        setIsLoading(true);
        try {
            const result = await client.getStreamedCompletion([], {
                sessionState: sessionState,
            });

            const latestMessage: AIChatMessage = { content: "", role: "assistant" };
            for await (const response of result) {
                if (response.sessionState) {
                    setSessionState(response.sessionState as string);
                }
                if (!response.delta) {
                    continue;
                }
                if (response.delta.role) {
                    latestMessage.role = response.delta.role;
                }
                if (response.delta.content) {
                    latestMessage.content += response.delta.content;
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
        // Generate initial session state if not present
        if (!sessionState && !initialFetchStarted.current) {
            const newSessionId = crypto.randomUUID();
            setSessionState(newSessionId);
            initialFetchStarted.current = true;
        }
    }, [sessionState]);

    // Invoke agent with empty message when session is ready and agent hasn't been invoked yet
    useEffect(() => {
        if (sessionState && !hasInvokedInitialAgent && !isLoading) {
            setHasInvokedInitialAgent(true);
            invokeAgentWithEmptyMessage();
        }
    }, [sessionState, hasInvokedInitialAgent, isLoading]);

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

    // Quando resetti la conversazione, consenti una nuova fetch iniziale
    const handleResetConversation = () => {
        const newSessionId = crypto.randomUUID();
        setSessionState(newSessionId);
        setMessages([]);
        setHasInvokedInitialAgent(false);
        initialFetchStarted.current = false; // <--- resetta la ref
    };

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    };
    useEffect(scrollToBottom, [messages]);

    const sendMessage = async () => {
        if (!input.trim() || isLoading) return;
        
        const message: AIChatMessage = {
            role: "user",
            content: input,
        };
        const updatedMessages: ChatEntry[] = [...messages, message];
        setMessages(updatedMessages);
        setInput("");
        setIsLoading(true);
        
        // Add a placeholder assistant message that will be updated
        const assistantMessage: AIChatMessage = { content: "", role: "assistant" };
        setMessages([...updatedMessages, assistantMessage]);
        
        try {
            // Build the conversation from updatedMessages, filtering out errors
            const conversation = updatedMessages
                .filter((entry) => !isChatError(entry))
                .map((msg) => msg as AIChatMessage);

            const result = await client.getStreamedCompletion(conversation, {
                sessionState: sessionState,
            });

            console.log("result", result);

            let accumulatedContent = "";
            for await (const response of result) {
                if (response.sessionState) {
                    setSessionState(response.sessionState as string);
                }
                if (!response.delta) {
                    continue;
                }
                if (response.delta.content) {
                    accumulatedContent += response.delta.content;
                    const updatedAssistantMessage: AIChatMessage = {
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

    const getErrorMessage = (message: AIChatError) => {
        return `${message.code}: ${message.message}`;
    };

    const handleThemeChange = (newTheme: Theme) => {
        setTheme(newTheme);
    };

    return (
        <div className={`${styles.chatWindow} ${effectiveTheme === 'dark' ? styles.dark : ''}`} style={style}>
            {/* Header with API selection, sessionState and reset button */}
            <div className={styles.header}>
                <h1 className={styles.headerTitle}>AI Agent Hub</h1>
                <div className={styles.headerContent}>
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
                    <div className={styles.agentSelector}>
                        <label className={styles.agentSelectorLabel}>Agent:</label>
                        <select 
                            className={styles.agentDropdown}
                            value={selectedApi}
                            onChange={(e) => {
                                const newApi = e.target.value as ApiType;
                                setSelectedApi(newApi);
                                
                                // Update the client with the new API endpoint
                                let baseUrl = '/agent/dotnet/chat/';
                                if (newApi === 'python') {
                                    baseUrl = '/agent/python/chat/';
                                } else if (newApi === 'groupchat') {
                                    baseUrl = '/agent/groupchat/chat/';
                                }
                                setClient(new AIChatProtocolClient(baseUrl));
                                
                                // Reset conversation when switching APIs and generate new session
                                const newSessionId = crypto.randomUUID();
                                setSessionState(newSessionId);
                                setMessages([]);
                                setHasInvokedInitialAgent(false);
                                initialFetchStarted.current = false;
                            }}
                        >
                            <option value="dotnet">📄 .NET Agent (Documents)</option>
                            <option value="python">📊 Python Agent (Financial)</option>
                            <option value="groupchat">👥 Group Chat (Multi-Agent)</option>
                        </select>
                    </div>
                    
                    <div className={styles.sessionInfo}>
                        <label className={styles.sessionLabel}>Session:</label>
                        <input
                            type="text"
                            value={sessionState || ''}
                            onChange={(e) => setSessionState(e.target.value)}
                            placeholder="Enter or generate session ID..."
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
                        <h2>Welcome to AI Agent Hub!</h2>
                        <p>Choose an agent from the dropdown above and start chatting!</p>
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