// src/presentation/api/proxyGroupsApi.ts
export type ProxyGroupDto = {
  id: number;
  name: string;
  region: string | null;
  status: string;
  protocol: string;
  host: string;
  port: number;
  hasAuth: boolean;

  provider: string | null;
  isRotating: boolean;
  maxConcurrency: number | null;
  rateLimitRpm: number | null;

  lastChecked: string | null;
  lastOkAt: string | null;
  successCount: number | null;
  failCount: number | null;

  latencyMs: number | null;
  lastStatus: string | null;

  endpoint: string | null;

  // ✅ thêm để drawer hiển thị đầy đủ (nếu BE có trả)
  metadataJson?: any | null;
  authUsername?: string | null; // nếu BE có trả
};

export type ListResponse = {
  total: number;
  page: number;
  pageSize: number;
  items: ProxyGroupDto[];
};

export type CreateProxyGroupRequest = {
  name: string;
  region?: string | null;
  status?: string | null;
  protocol: string;
  host: string;
  port: number;

  authUsername?: string | null;
  authPasswordPlain?: string | null;

  provider?: string | null;
  isRotating?: boolean | null;
  maxConcurrency?: number | null;
  rateLimitRpm?: number | null;

  metadataJson?: any | null;
};

export type UpdateProxyGroupRequest = Partial<CreateProxyGroupRequest>;

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

export function proxyGroupsApi(apiBase: string, token?: string) {
  return {
    list: (params: { page?: number; pageSize?: number; q?: string | null; status?: string | null; region?: string | null }) => {
      const usp = new URLSearchParams();
      usp.set("page", String(params.page ?? 1));
      usp.set("pageSize", String(params.pageSize ?? 20));
      if (params.q) usp.set("q", params.q);
      if (params.status) usp.set("status", params.status);
      if (params.region) usp.set("region", params.region);

      return http<ListResponse>(`${apiBase}/api/proxy-groups?${usp.toString()}`, {
        method: "GET",
        headers: buildHeaders(token),
      });
    },

    get: (id: number) =>
      http<ProxyGroupDto>(`${apiBase}/api/proxy-groups/${id}`, {
        method: "GET",
        headers: buildHeaders(token),
      }),

    create: (body: CreateProxyGroupRequest) =>
      http<{ id: number }>(`${apiBase}/api/proxy-groups`, {
        method: "POST",
        headers: buildHeaders(token),
        body: JSON.stringify(body),
      }),

    update: (id: number, body: UpdateProxyGroupRequest) =>
      http<{ ok: boolean }>(`${apiBase}/api/proxy-groups/${id}`, {
        method: "PUT",
        headers: buildHeaders(token),
        body: JSON.stringify(body),
      }),

    del: (id: number) =>
      http<{ ok: boolean }>(`${apiBase}/api/proxy-groups/${id}`, {
        method: "DELETE",
        headers: buildHeaders(token),
      }),
  };
}
