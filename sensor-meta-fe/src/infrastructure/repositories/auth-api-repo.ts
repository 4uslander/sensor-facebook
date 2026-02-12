import type { AuthRepo } from "../../domain/ports/auth-repo";
import type { Tokens } from "../../infrastructure/http/token-store";
import { authApi } from "../api/auth.api";
import { RegisterResSchema, TokenResponseSchema } from "../schemas/auth.schema";

export class AuthApiRepo implements AuthRepo {
  async register(email: string, password: string, role?: string): Promise<void> {
    const res = await authApi.register({ Email: email, Password: password, Role: role ?? "user" });
    const parsed = RegisterResSchema.parse(res.data);
    if (!parsed.ok) throw new Error(parsed.error ?? "Register failed");
  }

  async login(email: string, password: string, deviceInfo?: string): Promise<Tokens> {
    const res = await authApi.login({ Email: email, Password: password, DeviceInfo: deviceInfo ?? null });
    const parsed = TokenResponseSchema.parse(res.data);

    // map về domain tokens (camelCase giữ nguyên)
    return {
      accessToken: parsed.accessToken,
      accessExpires: parsed.accessExpires,
      refreshToken: parsed.refreshToken,
      refreshExpires: parsed.refreshExpires,
    };
  }
}
