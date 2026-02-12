// src/presentation/api/dashboardApi.ts
export type DashboardTotals = {
  products: number;
  keywords: number;
  accounts: number;
  proxies: number;
};

function buildHeaders(token?: string) {
  return {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

async function httpJson<T>(url: string, token?: string): Promise<T> {
  const res = await fetch(url, { method: "GET", headers: buildHeaders(token) });
  if (!res.ok) {
    const txt = await res.text().catch(() => "");
    throw new Error(`HTTP ${res.status}: ${txt || res.statusText}`);
  }
  return (await res.json()) as T;
}

type ListTotalResponse = { total: number };

export async function getDashboardTotals(apiBase: string, token?: string): Promise<DashboardTotals> {
  const qs = "page=1&pageSize=1";

  const [products, keywords, accounts, proxies] = await Promise.all([
    httpJson<ListTotalResponse>(`${apiBase}/api/listings?${qs}`, token),      // Total Product
    httpJson<ListTotalResponse>(`${apiBase}/api/keywords?${qs}`, token),      // Total Keyword
    httpJson<ListTotalResponse>(`${apiBase}/api/accounts?${qs}`, token),      // Total Account
    httpJson<ListTotalResponse>(`${apiBase}/api/proxy-groups?${qs}`, token),  // Total Proxy
  ]);

  return {
    products: products.total ?? 0,
    keywords: keywords.total ?? 0,
    accounts: accounts.total ?? 0,
    proxies: proxies.total ?? 0,
  };
}

export function formatNumber(n: number) {
  return new Intl.NumberFormat("en-US").format(n);
}
