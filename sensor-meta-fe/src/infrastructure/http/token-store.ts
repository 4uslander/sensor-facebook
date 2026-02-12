export type Tokens = {
  accessToken: string;
  accessExpires: string;
  refreshToken: string;
  refreshExpires: string;
};

const KEY = "sensor_tokens";

export const tokenStore = {
  get(): Tokens | null {
    const raw = localStorage.getItem(KEY);
    if (!raw) return null;
    try { return JSON.parse(raw) as Tokens; } catch { return null; }
  },
  set(t: Tokens) { localStorage.setItem(KEY, JSON.stringify(t)); },
  clear() { localStorage.removeItem(KEY); },
};
