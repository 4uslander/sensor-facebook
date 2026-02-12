import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";
import { authStorage } from "../../shared/lib/authStorage";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || "https://localhost:7141";

export const http = axios.create({
    baseURL: API_BASE_URL,
    withCredentials: false,
    headers: { "Content-Type": "application/json" },
});

// ===== Request: attach access token =====
http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    const token = authStorage.getAccessToken();

    if (token) {
        // axios v1: headers có thể là AxiosHeaders (có hàm set)
        if (config.headers && typeof (config.headers as any).set === "function") {
            (config.headers as any).set("Authorization", `Bearer ${token}`);
        } else {
            config.headers = config.headers ?? {};
            (config.headers as any).Authorization = `Bearer ${token}`;
        }
    }

    return config;
});


let isRefreshing = false;
let refreshQueue: Array<(token: string | null) => void> = [];

function enqueueRefresh(cb: (token: string | null) => void) {
    refreshQueue.push(cb);
}
function flushQueue(token: string | null) {
    refreshQueue.forEach((cb) => cb(token));
    refreshQueue = [];
}

// ===== Response: if 401 => refresh token then retry =====
http.interceptors.response.use(
    (res) => res,
    async (error: AxiosError) => {
        const original = error.config as any;
        const status = error.response?.status;

        // chỉ retry 1 lần
        if (status !== 401 || original?._retry) throw error;

        const refreshToken = authStorage.getRefreshToken();
        if (!refreshToken) {
            authStorage.clear();
            throw error;
        }

        // Nếu đang refresh, chờ token mới rồi retry
        if (isRefreshing) {
            return new Promise((resolve, reject) => {
                enqueueRefresh((newToken) => {
                    if (!newToken) return reject(error);
                    original.headers = original.headers ?? {};
                    original.headers.Authorization = `Bearer ${newToken}`;
                    resolve(http(original));
                });
            });
        }

        isRefreshing = true;
        original._retry = true;

        try {
            const resp = await axios.post(
                `${API_BASE_URL}/api/auth/refresh-token`,
                { refreshToken },
                { headers: { "Content-Type": "application/json" } }
            );

            const data = resp.data as {
                accessToken: string;
                accessExpires: string;
                refreshToken: string;
                refreshExpires: string;
            };

            authStorage.set({
                accessToken: data.accessToken,
                accessExpires: data.accessExpires,
                refreshToken: data.refreshToken,
                refreshExpires: data.refreshExpires,
            });

            flushQueue(data.accessToken);

            original.headers = original.headers ?? {};
            original.headers.Authorization = `Bearer ${data.accessToken}`;
            return http(original);
        } catch (e) {
            authStorage.clear();
            flushQueue(null);
            throw error;
        } finally {
            isRefreshing = false;
        }
    }
);
