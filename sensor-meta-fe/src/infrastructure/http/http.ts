import axios from "axios";
import { tokenStore } from "./token-store";

export const http = axios.create({
  baseURL: `${import.meta.env.VITE_API_BASE_URL}/api`,
  timeout: 20000,
});

http.interceptors.request.use((config) => {
  const t = tokenStore.get();
  if (t?.accessToken) config.headers.Authorization = `Bearer ${t.accessToken}`;
  return config;
});
