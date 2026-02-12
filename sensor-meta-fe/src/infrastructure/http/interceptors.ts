// import { http } from "./axios-instance";
// import { tokenStore } from "./token-store";
// import { authApi } from "../api/auth.api";

// let refreshing: Promise<string | null> | null = null;

// export function setupInterceptors() {
//   http.interceptors.response.use(
//     (res) => res,
//     async (err) => {
//       const status = err?.response?.status;
//       const original = err?.config;

//       if (status !== 401 || original?._retry) throw err;
//       original._retry = true;

//       const tokens = tokenStore.get();
//       if (!tokens?.refreshToken) {
//         tokenStore.clear();
//         throw err;
//       }

//       if (!refreshing) {
//         refreshing = (async () => {
//           try {
//             const res = await authApi.refresh({ refreshToken: tokens.refreshToken });
//             // parse envelope ở repository (đúng kiến trúc). Ở đây chỉ tạm lấy thẳng nếu backend chưa bọc.
//             const next = res.data?.data ?? res.data; // sẽ chuẩn hóa ở phần Envelope repo
//             if (!next?.accessToken) return null;
//             tokenStore.set(next);
//             return next.accessToken as string;
//           } catch {
//             tokenStore.clear();
//             return null;
//           } finally {
//             refreshing = null;
//           }
//         })();
//       }

//       const newAccess = await refreshing;
//       if (!newAccess) throw err;

//       original.headers.Authorization = `Bearer ${newAccess}`;
//       return http.request(original);
//     }
//   );
// }
