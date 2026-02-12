// src/shared/auth/logout.ts
import { authStorage } from "../../shared/lib/authStorage";

// OPTIONAL: nếu project có token-store thì mở comment dòng import + clearToken()
// import { clearToken } from "../../infrastructure/http/token-store";

export function logout() {
  // 1) clear tokens in localStorage (KEY = "sf_auth_tokens")
  authStorage.clear();

  // 2) OPTIONAL: clear token cache in memory (nếu interceptor đọc từ token-store)
  // clearToken();

  // 3) OPTIONAL: clear react-query cache / app state thì làm ở nơi gọi (UI layer)
}
