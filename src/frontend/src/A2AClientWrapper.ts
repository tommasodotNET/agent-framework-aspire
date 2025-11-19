// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { A2AClient } from '@a2a-js/sdk/client';
import type { MessageSendParams, Message } from '@a2a-js/sdk';
import { v4 as uuidv4 } from 'uuid';

export interface A2AChatMessage {
    role: 'user' | 'assistant';
    content: string;
}

export interface A2AStreamEvent {
    content?: string;
    contextId?: string;
}

export class A2AClientWrapper {
    private client: A2AClient | null = null;
    private agentCardUrl: string;

    constructor(agentCardUrl: string) {
        this.agentCardUrl = agentCardUrl;
    }

    private async ensureClient(): Promise<A2AClient> {
        if (!this.client) {
            this.client = await A2AClient.fromCardUrl(this.agentCardUrl);
        }
        return this.client;
    }

    async *sendMessageStream(
        messages: A2AChatMessage[],
        contextId?: string
    ): AsyncGenerator<A2AStreamEvent, void, undefined> {
        const client = await this.ensureClient();

        // Get the last user message
        const userMessage = messages[messages.length - 1];
        
        if (!userMessage || userMessage.role !== 'user') {
            throw new Error('Last message must be from user');
        }

        // Build the message params using A2A SDK v0.3.x format
        const params: MessageSendParams = {
            message: {
                messageId: uuidv4(),
                role: 'user',
                kind: 'message',
                parts: [{ kind: 'text', text: userMessage.content }],
                // Use contextId to maintain conversation context
                contextId: contextId,
            },
        };

        try {
            // Stream the response
            const stream = client.sendMessageStream(params);
            
            for await (const event of stream) {
                // Handle Message events (text responses)
                if (event.kind === 'message') {
                    const message = event as Message;
                    if (message.parts && Array.isArray(message.parts)) {
                        for (const part of message.parts) {
                            if (part.kind === 'text' && part.text) {
                                yield {
                                    content: part.text,
                                    contextId: message.contextId,
                                };
                            }
                        }
                    }
                }
                
                // Handle Task events (for tracking async operations)
                if (event.kind === 'task') {
                    // Task created - track contextId
                    if (event.contextId) {
                        yield {
                            contextId: event.contextId,
                        };
                    }
                }

                // Handle status updates
                if (event.kind === 'status-update') {
                    // Track contextId from status updates
                    if (event.contextId) {
                        yield {
                            contextId: event.contextId,
                        };
                    }
                }
            }
        } catch (error) {
            console.error('Error streaming message:', error);
            throw error;
        }
    }

    async sendMessage(
        messages: A2AChatMessage[],
        contextId?: string
    ): Promise<{ content: string; contextId?: string }> {
        const client = await this.ensureClient();

        // Get the last user message
        const userMessage = messages[messages.length - 1];
        
        if (!userMessage || userMessage.role !== 'user') {
            throw new Error('Last message must be from user');
        }

        // Build the message params using A2A SDK v0.3.x format
        const params: MessageSendParams = {
            message: {
                messageId: uuidv4(),
                role: 'user',
                kind: 'message',
                parts: [{ kind: 'text', text: userMessage.content }],
                contextId: contextId,
            },
        };

        const response = await client.sendMessage(params);

        if ('error' in response) {
            throw new Error(response.error.message || 'Unknown error');
        }

        // Extract text content from the response
        let content = '';
        let responseContextId: string | undefined;

        if ('result' in response && response.result) {
            const result = response.result;
            
            // Handle Message response
            if (result.kind === 'message') {
                const message = result as Message;
                responseContextId = message.contextId;
                
                if (message.parts && Array.isArray(message.parts)) {
                    for (const part of message.parts) {
                        if (part.kind === 'text' && part.text) {
                            content += part.text;
                        }
                    }
                }
            }
            
            // Handle Task response (get final message from history)
            if (result.kind === 'task') {
                responseContextId = result.contextId;
                
                // Get the last message from task history
                if (result.history && result.history.length > 0) {
                    const lastMessage = result.history[result.history.length - 1];
                    if (lastMessage.kind === 'message' && lastMessage.parts) {
                        for (const part of lastMessage.parts) {
                            if (part.kind === 'text' && part.text) {
                                content += part.text;
                            }
                        }
                    }
                }
            }
        }

        return {
            content,
            contextId: responseContextId,
        };
    }
}
