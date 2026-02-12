import type { Tokens } from "../../infrastructure/http/token-store";

export interface AuthRepo {
  register(email: string, password: string, role?: string): Promise<void>;
  login(email: string, password: string, deviceInfo?: string): Promise<Tokens>;
}
