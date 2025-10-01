// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Button, Dropdown, Option, SelectionEvents, OptionOnSelectData } from "@fluentui/react-components";
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
type ApiType = 'dotnet' | 'python';

function isChatError(entry: unknown): entry is AIChatError {
    return (entry as AIChatError).code !== undefined;
}

export default function Chat({ style }: { style: React.CSSProperties }) {
    const [selectedApi, setSelectedApi] = useState<ApiType>('dotnet');
    const [client, setClient] = useState(() => new AIChatProtocolClient("/agent/chat/"));

    const [messages, setMessages] = useState<ChatEntry[]>([]);
    const [input, setInput] = useState<string>("");
    const inputId = useId();
    // Set initial sessionState to undefined
    const [sessionState, setSessionState] = useState<string | undefined>(undefined);
    const [initialFetchDone, setInitialFetchDone] = useState(false);
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const initialFetchStarted = useRef(false); // <--- aggiungi questa ref

    useEffect(() => {
        // Evita chiamate multiple quando sessionState è undefined
        if (sessionState !== undefined || initialFetchDone || initialFetchStarted.current) return;

        initialFetchStarted.current = true; // <--- segna che la fetch è partita

        const fetchInitialData = async () => {
            try {
                const result = await client.getStreamedCompletion([], {
                    sessionState: sessionState,
                });
                const latestMessage: AIChatMessage = { content: "", role: "assistant" };
                for await (const response of result) {
                    if (response.sessionState && response.sessionState !== sessionState) {
                        setSessionState(response.sessionState);
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
                if (isChatError(e)) {
                    setMessages([{ code: e.code, message: e.message }]);
                }
            } finally {
                setInitialFetchDone(true);
            }
        };

        fetchInitialData();
    }, [sessionState, initialFetchDone]); // Dipendenze corrette

    // Quando resetti la conversazione, consenti una nuova fetch iniziale
    const handleResetConversation = () => {
        setSessionState(undefined);
        setMessages([]);
        setInitialFetchDone(false);
        initialFetchStarted.current = false; // <--- resetta la ref
    };

    const handleApiChange = (_: SelectionEvents, data: OptionOnSelectData) => {
        const newApi = data.optionValue as ApiType;
        setSelectedApi(newApi);
        
        // Update the client with the new API endpoint
        const baseUrl = newApi === 'python' ? '/agent/python/chat/' : '/agent/dotnet/chat/';
        setClient(new AIChatProtocolClient(baseUrl));
        
        // Reset conversation when switching APIs
        handleResetConversation();
    };

    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
    };
    useEffect(scrollToBottom, [messages]);

    const sendMessage = async () => {
        const message: AIChatMessage = {
            role: "user",
            content: input,
        };
        const updatedMessages: ChatEntry[] = [...messages, message];
        setMessages(updatedMessages);
        setInput("");
        try {
            // Build the conversation from updatedMessages, filtering out errors
            const conversation = updatedMessages
                .filter((entry) => !isChatError(entry))
                .map((msg) => msg as AIChatMessage);

            const result = await client.getStreamedCompletion(conversation, {
                sessionState: sessionState,
            });

            console.log("result", result);

            const latestMessage: AIChatMessage = { content: "", role: "assistant" };
            for await (const response of result) {
                if (response.sessionState) {
                    setSessionState(response.sessionState);
                }
                if (!response.delta) {
                    continue;
                }
                if (response.delta.role) {
                    latestMessage.role = response.delta.role;
                }
                if (response.delta.content) {
                    latestMessage.content += response.delta.content;
                    setMessages([...updatedMessages, latestMessage]);
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
                    { code: "unknown_error", message: e },
                ]);
            }
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

    return (
        <div className={styles.chatWindow} style={style}>
            {/* Header with API selection, sessionState and reset button */}
            <div style={{ display: "flex", alignItems: "center", marginBottom: 12, gap: 12 }}>
                <label>Agent API:</label>
                <Dropdown
                    value={selectedApi === 'dotnet' ? '.NET Agent (Documents)' : 'Python Agent (Financial)'}
                    onOptionSelect={handleApiChange}
                >
                    <Option key="dotnet" value="dotnet">
                        .NET Agent (Documents)
                    </Option>
                    <Option key="python" value="python">
                        Python Agent (Financial)
                    </Option>
                </Dropdown>
                
                <label style={{ marginRight: 8 }}>Session State:</label>
                <input
                    type="text"
                    value={sessionState || ''}
                    readOnly
                    style={{ width: 200 }}
                />
                <Button onClick={handleResetConversation} appearance="secondary">
                    Reset Conversation
                </Button>
            </div>
            <div className={styles.messages}>
                {messages.map((message) => (
                    <div key={crypto.randomUUID()} className={getClassName(message)}>
                        {isChatError(message) ? (
                            <>{getErrorMessage(message)}</>
                        ) : (
                            <>
                                <div className={styles.messageBubble}>
                                    <ReactMarkdown remarkPlugins={[gfm]}>
                                        {message.content}
                                    </ReactMarkdown>
                                </div>
                            </>
                        )}
                    </div>
                ))}
                <div ref={messagesEndRef} />
            </div>
            <div className={styles.inputArea}>
                <TextareaAutosize
                    id={inputId}
                    value={input}
                    onChange={(e) => setInput(e.target.value)}
                    onKeyDown={(e) => {
                        if (e.key === "Enter" && e.shiftKey) {
                            e.preventDefault();
                            sendMessage();
                        }
                    }}
                    minRows={1}
                    maxRows={4}
                />
                <Button onClick={sendMessage}>Send</Button>
            </div>
        </div>
    );
}