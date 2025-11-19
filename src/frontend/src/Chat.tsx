// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Button } from "@fluentui/react-components";
import { A2AClient } from '@a2a-js/sdk/client';
import type { MessageSendParams, Message } from '@a2a-js/sdk';
import { useEffect, useId, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import TextareaAutosize from "react-textarea-autosize";
import styles from "./Chat.module.css";
import gfm from "remark-gfm";
import { v4 as uuidv4 } from 'uuid';

type ChatMessage = {
    role: 'user' | 'assistant';
    content: string;
};

type ApiType = 'dotnet' | 'python' | 'groupchat';
type Theme = 'light' | 'dark' | 'system';

export default function Chat({ style }: { style: React.CSSProperties }) {
    const [selectedApi, setSelectedApi] = useState<ApiType>('dotnet');
    const [client, setClient] = useState<A2AClient | null>(null);

    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [input, setInput] = useState<string>("");
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [hasInvokedInitialAgent, setHasInvokedInitialAgent] = useState<boolean>(false);
    const inputId = useId();
    const [conversationId, setConversationId] = useState<string>("");
    const [theme, setTheme] = useState<Theme>('system');
    const [effectiveTheme, setEffectiveTheme] = useState<'light' | 'dark'>('light');
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const initialFetchStarted = useRef(false);

    // Initialize A2A client based on selected API
    useEffect(() => {
        const initClient = async () => {
            let cardUrl = '/agenta2a/dotnet/v1/card';
            if (selectedApi === 'python') {
                cardUrl = '/agenta2a/python/v1/card';
            } else if (selectedApi === 'groupchat') {
                cardUrl = '/agenta2a/groupchat/v1/card';
            }
            
            try {
                const a2aClient = await A2AClient.fromCardUrl(cardUrl);
                setClient(a2aClient);
            } catch (error) {
                console.error('Failed to initialize A2A client:', error);
            }
        };

        initClient();
    }, [selectedApi]);

    const invokeAgentWithEmptyMessage = async () => {
        if (isLoading || !client || !conversationId) return;
        
        setIsLoading(true);
        try {
            const params: MessageSendParams = {
                message: {
                    messageId: uuidv4(),
                    role: 'user',
                    kind: 'message',
                    parts: [{ kind: 'text', text: '' }],
                    contextId: conversationId,
                },
            };

            let accumulatedContent = '';
            for await (const event of client.sendMessageStream(params)) {
                if (event.kind === 'message') {
                    const message = event as Message;
                    for (const part of message.parts) {
                        if (part.kind === 'text') {
                            accumulatedContent += part.text;
                        }
                    }
                }
            }

            if (accumulatedContent) {
                setMessages([{ role: 'assistant', content: accumulatedContent }]);
            }
        } catch (e) {
            console.error("ERROR: ", e);
            setMessages([{ role: 'assistant', content: `Error: ${String(e)}` }]);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        // Generate initial conversation ID if not present
        if (!conversationId && !initialFetchStarted.current) {
            const newConversationId = crypto.randomUUID();
            setConversationId(newConversationId);
            initialFetchStarted.current = true;
        }
    }, [conversationId]);

    // Invoke agent with empty message when session is ready and agent hasn't been invoked yet
    useEffect(() => {
        if (conversationId && client && !hasInvokedInitialAgent && !isLoading) {
            setHasInvokedInitialAgent(true);
            invokeAgentWithEmptyMessage();
        }
    }, [conversationId, client, hasInvokedInitialAgent, isLoading]);

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

    const handleResetConversation = () => {
        const newConversationId = crypto.randomUUID();
        setConversationId(newConversationId);
        setMessages([]);
        setHasInvokedInitialAgent(false);
        initialFetchStarted.current = false;
    };

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    };
    useEffect(scrollToBottom, [messages]);

    const sendMessage = async () => {
        if (!input.trim() || isLoading || !client) return;
        
        const userMessage: ChatMessage = {
            role: "user",
            content: input,
        };
        const updatedMessages: ChatMessage[] = [...messages, userMessage];
        setMessages(updatedMessages);
        setInput("");
        setIsLoading(true);
        
        // Add a placeholder assistant message that will be updated
        const assistantMessage: ChatMessage = { content: "", role: "assistant" };
        setMessages([...updatedMessages, assistantMessage]);
        
        try {
            const params: MessageSendParams = {
                message: {
                    messageId: uuidv4(),
                    role: 'user',
                    kind: 'message',
                    parts: [{ kind: 'text', text: input }],
                    contextId: conversationId,
                },
            };

            let accumulatedContent = '';
            for await (const event of client.sendMessageStream(params)) {
                if (event.kind === 'message') {
                    const message = event as Message;
                    for (const part of message.parts) {
                        if (part.kind === 'text') {
                            accumulatedContent += part.text;
                            const updatedAssistantMessage: ChatMessage = {
                                content: accumulatedContent,
                                role: "assistant"
                            };
                            setMessages([...updatedMessages, updatedAssistantMessage]);
                        }
                    }
                }
            }
        } catch (e) {
            console.error("ERROR: ", e);
            setMessages([...updatedMessages, { role: 'assistant', content: `Error: ${String(e)}` }]);
        } finally {
            setIsLoading(false);
        }
    };

    const handleThemeChange = (newTheme: Theme) => {
        setTheme(newTheme);
    };

    return (
        <div className={`${styles.chatWindow} ${effectiveTheme === 'dark' ? styles.dark : ''}`} style={style}>
            {/* Header with API selection, conversationId and reset button */}
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
                                
                                // Reset conversation when switching APIs and generate new session
                                const newConversationId = crypto.randomUUID();
                                setConversationId(newConversationId);
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
                            value={conversationId || ''}
                            onChange={(e) => setConversationId(e.target.value)}
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
                    <div key={`message-${index}`} className={message.role === 'user' ? styles.userMessage : styles.assistantMessage}>
                        <div className={styles.messageIcon}>
                            {message.role === 'user' ? '👤' : '🤖'}
                        </div>
                        <div className={styles.messageBubble}>
                            <ReactMarkdown remarkPlugins={[gfm]}>
                                {message.content}
                            </ReactMarkdown>
                        </div>
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
