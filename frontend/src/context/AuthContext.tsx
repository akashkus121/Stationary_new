import React, { createContext, useContext, useEffect } from 'react';
import type { User } from '../types';
import { api } from '../services/api';
import { useAppDispatch, useAppSelector } from '../store';
import { setCredentials, logout as logoutAction } from '../store/authSlice';

interface AuthContextType {
  user: User | null;
  token: string | null;
  accessToken: string | null;
  refreshToken: string | null;
  loading: boolean;
  isAdmin: boolean;
  login: (token: string, user: User, refreshToken?: string) => void;
  logout: () => void;
  refetchUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const dispatch = useAppDispatch();
  const { user, accessToken, refreshToken, loading } = useAppSelector((state) => state.auth);

  const refetchUser = async () => {
    try {
      if (!accessToken && !refreshToken) {
        dispatch(logoutAction());
        return;
      }
      const userData = await api.getCurrentUser();
      if (userData) {
        const storedAccess = localStorage.getItem('accessToken') || localStorage.getItem('token') || '';
        const storedRefresh = localStorage.getItem('refreshToken') || '';
        dispatch(setCredentials({ user: userData, accessToken: storedAccess, refreshToken: storedRefresh }));
      } else {
        dispatch(logoutAction());
      }
    } catch {
      dispatch(logoutAction());
    }
  };

  useEffect(() => {
    refetchUser();
  }, []);

  const login = (newToken: string, newUser: User, newRefreshToken?: string) => {
    dispatch(
      setCredentials({
        user: newUser,
        accessToken: newToken,
        refreshToken: newRefreshToken || localStorage.getItem('refreshToken') || '',
      })
    );
  };

  const logout = () => {
    dispatch(logoutAction());
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token: accessToken,
        accessToken,
        refreshToken,
        loading,
        isAdmin: user?.role?.toLowerCase() === 'admin',
        login,
        logout,
        refetchUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within AuthProvider');
  return context;
};
