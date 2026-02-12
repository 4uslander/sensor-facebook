// src/presentation/api/searchJobsApi.ts
export type JobListItemDto = {
  id: string; // guid
  keywordId: number;
  status: string; // "queued" | "running" | "failed" | "completed" ...
  attempts: number;
  scheduledAt: string | null;
  startedAt: string | null;
  finishedAt: string | null;
};

export type ListResponse = {
  total: number;
  page: number;
  pageSize: number;
  items: JobListItemDto[];
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
  // POST retry returns json {ok:true}
  return (await res.json()) as T;
}

export function searchJobsApi(apiBase: string, token?: string) {
  return {
    list: (params: {
      page?: number;
      pageSize?: number;
      status?: string | null;
      keywordId?: number | null;
      from?: string | null; // ISO DateTimeOffset string
      to?: string | null;   // ISO DateTimeOffset string
    }) => {
      const usp = new URLSearchParams();
      usp.set("page", String(params.page ?? 1));
      usp.set("pageSize", String(params.pageSize ?? 20));
      if (params.status) usp.set("status", params.status);
      if (params.keywordId != null) usp.set("keywordId", String(params.keywordId));
      if (params.from) usp.set("from", params.from);
      if (params.to) usp.set("to", params.to);

      return http<ListResponse>(`${apiBase}/api/jobs/search?${usp.toString()}`, {
        method: "GET",
        headers: buildHeaders(token),
      });
    },

    get: (jobId: string) =>
      http<any>(`${apiBase}/api/jobs/search/${jobId}`, {
        method: "GET",
        headers: buildHeaders(token),
      }),

    retry: (jobId: string) =>
      http<{ ok: boolean }>(`${apiBase}/api/jobs/search/${jobId}/retry`, {
        method: "POST",
        headers: buildHeaders(token),
      }),

    runNow: (keywordId: number, priority: "high" | "low" = "high") =>
      http<{ jobId: string; priority: string }>(
        `${apiBase}/api/jobs/search/run-now/${keywordId}?priority=${encodeURIComponent(priority)}`,
        {
          method: "POST",
          headers: buildHeaders(token),
        }
      ),
  };
}
