import type { AuthRepo } from "../../../domain/ports/auth-repo";

export function registerUsecase(repo: AuthRepo) {
  return async (email: string, password: string) => {

    if (!email.includes("@")) throw new Error("Invalid email");
    if (password.length < 6) throw new Error("Password must be at least 6 characters");
    await repo.register(email.trim(), password);
  };
}
