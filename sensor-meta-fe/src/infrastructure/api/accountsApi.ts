// src/presentation/api/accountsApi.ts
export type AccountStatus = "active" | "inactive" | "checkpoint" | "disabled";

export type FbAccountDto = {
  id: string; // guid
  email: string;
  displayName: string | null;
  proxyGroupId: number | null;

  proxyGroupName: string | null;
  region: string | null;

  status: AccountStatus | string;

  checkpointCount: number;
  lastCheckpoint: string | null;

  createdBy: string | null;
  createdAt: string | null;

  hasCookie: boolean;
  profileDir: string | null;
};

export type ListResponse = {
  total: number;
  page: number;
  pageSize: number;
  items: FbAccountDto[];
};

export type CreateOrUpdateAccountRequest = {
  id?: string | null;
  email: string;
  displayName?: string | null;
  proxyGroupId?: number | null;
  profileDir?: string | null;
  cookiePlain?: string | null;
  status?: string | null; // active|inactive|checkpoint|disabled
};

function buildHeaders(token?: string) {
  return {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

async function http<T>(url: string, init: RequestInit): Promise<T> {
  const res = await fetch(url, init);
  if (!res.ok) {
    const txt = await res.text().catch(() => "");
    throw new Error(`HTTP ${res.status}: ${txt || res.statusText}`);
  }
  return (await res.json()) as T;
}

export function accountsApi(apiBase: string, token?: string) {
  return {
    list: (params: {
      page?: number;
      pageSize?: number;
      q?: string | null;
      status?: string | null;
      region?: string | null;
    }) => {
      const usp = new URLSearchParams();
      usp.set("page", String(params.page ?? 1));
      usp.set("pageSize", String(params.pageSize ?? 20));
      if (params.q) usp.set("q", params.q);
      if (params.status) usp.set("status", params.status);
      if (params.region) usp.set("region", params.region);

      return http<ListResponse>(`${apiBase}/api/accounts?${usp.toString()}`, {
        method: "GET",
        headers: buildHeaders(token),
      });
    },

    get: (id: string) =>
      http<FbAccountDto>(`${apiBase}/api/accounts/${id}`, {
        method: "GET",
        headers: buildHeaders(token),
      }),

    // POST /api/accounts : create or update
    upsert: (body: CreateOrUpdateAccountRequest) =>
      http<{ id: string }>(`${apiBase}/api/accounts`, {
        method: "POST",
        headers: buildHeaders(token),
        body: JSON.stringify(body),
      }),

    // PUT /api/accounts/{id}?status=active|inactive|checkpoint|disabled
    updateStatus: (id: string, status: string) =>
      http<{ ok: boolean }>(`${apiBase}/api/accounts/${id}?status=${encodeURIComponent(status)}`, {
        method: "PUT",
        headers: buildHeaders(token),
      }),

    lock: (id: string) =>
      http<{ ok: boolean }>(`${apiBase}/api/accounts/${id}/lock`, {
        method: "POST",
        headers: buildHeaders(token),
      }),

    unlock: (id: string) =>
      http<{ ok: boolean }>(`${apiBase}/api/accounts/${id}/unlock`, {
        method: "POST",
        headers: buildHeaders(token),
      }),

    events: (id: string, params?: { page?: number; pageSize?: number }) => {
      const usp = new URLSearchParams();
      usp.set("page", String(params?.page ?? 1));
      usp.set("pageSize", String(params?.pageSize ?? 50));
      return http<{
        total: number;
        page: number;
        pageSize: number;
        items: Array<{
          id: number;
          accountId: string;
          eventType: string;
          payload: string | null;
          occurredAt: string | null;
        }>;
      }>(`${apiBase}/api/accounts/${id}/events?${usp.toString()}`, {
        method: "GET",
        headers: buildHeaders(token),
      });
    },
  };
}
