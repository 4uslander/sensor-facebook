import { http } from "../http/httpClient";
import type { TokenPayload } from "../../shared/lib/authStorage";

export type LoginRequest = {
  email: string;
  password: string;
  deviceInfo?: string | null;
};

export type RegisterRequest = {
  email: string;
  password: string;
  role?: string | null; // "user"
};

export async function loginApi(payload: LoginRequest): Promise<TokenPayload> {
  const res = await http.post<TokenPayload>("/api/auth/login", payload);
  return res.data;
}

export async function registerApi(payload: RegisterRequest): Promise<{ ok: boolean; error?: string }> {
  const res = await http.post<{ ok: boolean; error?: string }>("/api/auth/register", payload);
  return res.data;
}
