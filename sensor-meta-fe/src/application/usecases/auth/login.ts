import type { AuthRepo } from "../../../domain/ports/auth-repo";
import { tokenStore } from "../../../infrastructure/http/token-store";

export function loginUsecase(repo: AuthRepo) {
  return async (email: string, password: string) => {
    const deviceInfo = navigator.userAgent;
    const tokens = await repo.login(email.trim(), password, deviceInfo);
    tokenStore.set(tokens);
    return tokens;
  };
}
