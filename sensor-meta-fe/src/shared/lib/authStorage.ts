export type TokenPayload = {
  accessToken: string;
  accessExpires: string;  // ISO string
  refreshToken: string;
  refreshExpires: string; // ISO string
};

const KEY = "sf_auth_tokens";

export const authStorage = {
  get(): TokenPayload | null {
    const raw = localStorage.getItem(KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as TokenPayload;
    } catch {
      return null;
    }
  },

  set(tokens: TokenPayload) {
    localStorage.setItem(KEY, JSON.stringify(tokens));
  },

  clear() {
    localStorage.removeItem(KEY);
  },

  getAccessToken(): string | null {
    return this.get()?.accessToken ?? null;
  },

  getRefreshToken(): string | null {
    return this.get()?.refreshToken ?? null;
  },

  isAccessExpired(skewSeconds = 15): boolean {
    const t = this.get();
    if (!t?.accessExpires) return true;
    const exp = new Date(t.accessExpires).getTime();
    return Date.now() + skewSeconds * 1000 >= exp;
  },
};
