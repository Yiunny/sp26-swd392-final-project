import apiClient from './apiClient';

export interface ChatMessagePayload {
    id: string;
    sender: string;
    message: string;
    createdAt: string;
}

export interface ChatResponse {
    response: string;
}

export const chatService = {
    /**
     * POST /api/Ai/chat
     * Sends conversation messages to the AI chatbot and returns the response.
     */
    sendMessage: async (
        messages: ChatMessagePayload[],
        language: string = "Vi",
    ): Promise<ChatResponse> => {
        const res = await apiClient.post<ChatResponse>("/Ai/chat", {
            messages,
            language,
        });
        return res.data;
    },
};
