/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useMemo, useState } from "react";
import { Navigate, useLocation } from "react-router-dom";
import {
  clearSession,
  getSession,
  login,
  registrarEmpresa,
  saveSession,
} from "../lib/api";
import type { ApiRecord, AuthSession } from "../lib/api";
import type { ReactNode } from "react";

type AuthContextValue = {
  session: AuthSession | null;
  isAuthenticated: boolean;
  entrar: (email: string, senha: string) => Promise<void>;
  registrar: (payload: ApiRecord) => Promise<void>;
  sair: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(() => getSession());

  async function entrar(email: string, senha: string) {
    const nextSession = await login(email, senha);
    saveSession(nextSession);
    setSession(nextSession);
  }

  async function registrar(payload: ApiRecord) {
    const nextSession = await registrarEmpresa(payload);
    saveSession(nextSession);
    setSession(nextSession);
  }

  function sair() {
    clearSession();
    setSession(null);
  }

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      isAuthenticated: Boolean(session),
      entrar,
      registrar,
      sair,
    }),
    [session],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth deve ser usado dentro de AuthProvider.");
  return context;
}

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const location = useLocation();
  const { isAuthenticated } = useAuth();

  if (!isAuthenticated) {
    return <Navigate to="/entrar" replace state={{ from: location }} />;
  }

  return children;
}
