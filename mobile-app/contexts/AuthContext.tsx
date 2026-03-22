import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { authService } from '../services/authService';
import type { User, LoginRequest, GoogleLoginRequest, LoginResponse } from '../types/auth';

interface AuthContextType {
    user: User | null;
    token: string | null;
    isLoading: boolean;
    isAuthenticated: boolean;
    login: (data: LoginRequest) => Promise<LoginResponse>;
    loginWithGoogle: (data: GoogleLoginRequest) => Promise<LoginResponse>;
    logout: () => Promise<void>;
    refreshUser: () => Promise<void>;
    validateSession: () => Promise<boolean>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
    const [user, setUser] = useState<User | null>(null);
    const [token, setToken] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    // Load stored auth on mount and validate current session
    useEffect(() => {
        (async () => {
            try {
                const [storedToken, storedUser] = await Promise.all([
                    AsyncStorage.getItem('token'),
                    AsyncStorage.getItem('user'),
                ]);

                if (storedToken) {
                    setToken(storedToken);
                }

                if (storedUser) {
                    try {
                        setUser(JSON.parse(storedUser));
                    } catch {
                        // ignore JSON parse errors
                    }
                }

                if (storedToken) {
                    const isValid = await authService.validateSession();
                    if (!isValid) {
                        setToken(null);
                        setUser(null);
                    } else {
                        const latestUser = await authService.getUser();
                        setUser(latestUser);
                    }
                }
            } finally {
                setIsLoading(false);
            }
        })();
    }, []);

    const login = useCallback(async (data: LoginRequest): Promise<LoginResponse> => {
        const response = await authService.login(data);
        if (response.Success && response.Data) {
            setToken(response.Data.Token);
            setUser(response.Data.User);
        }
        return response;
    }, []);

    const loginWithGoogle = useCallback(async (data: GoogleLoginRequest): Promise<LoginResponse> => {
        const response = await authService.loginWithGoogle(data);
        if (response.Success && response.Data) {
            setToken(response.Data.Token);
            setUser(response.Data.User);
        }
        return response;
    }, []);

    const logout = useCallback(async () => {
        await authService.logout();
        setToken(null);
        setUser(null);
    }, []);

    const refreshUser = useCallback(async () => {
        const u = await authService.getUser();
        setUser(u);
    }, []);

    const validateSession = useCallback(async () => {
        const isValid = await authService.validateSession();
        if (!isValid) {
            setToken(null);
            setUser(null);
            return false;
        }

        const latestUser = await authService.getUser();
        setUser(latestUser);
        if (!token) {
            const latestToken = await authService.getToken();
            setToken(latestToken);
        }
        return true;
    }, [token]);

    return (
        <AuthContext.Provider
            value={{
                user,
                token,
                isLoading,
                isAuthenticated: !!token,
                login,
                loginWithGoogle,
                logout,
                refreshUser,
                validateSession,
            }}
        >
            {children}
        </AuthContext.Provider>
    );
}

export function useAuth(): AuthContextType {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
}
