import { useEffect, useMemo, useState } from "react";
import AppShell from "../layout/AppShell";
import StatCard from "../components/StatCard";
import ProductTable from "../components/ProductTable";
import { Box, BarChart3, Users, Timer } from "lucide-react";
import { authStorage } from "../../shared/lib/authStorage";
import { getDashboardTotals, formatNumber } from "../../infrastructure/api/dashboardApi";

export default function DashboardPage() {
  const baseUrl = import.meta.env.VITE_API_URL || "https://localhost:7141";
  const token = authStorage.getAccessToken() || undefined;

  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const [totals, setTotals] = useState({
    products: 0,
    keywords: 0,
    accounts: 0,
    proxies: 0,
  });

  useEffect(() => {
    let alive = true;

    async function load() {
      setLoading(true);
      setErr(null);
      try {
        const t = await getDashboardTotals(baseUrl, token);
        if (!alive) return;
        setTotals(t);
      } catch (e: any) {
        if (!alive) return;
        setErr(e?.message || "Load dashboard totals failed");
      } finally {
        if (!alive) return;
        setLoading(false);
      }
    }

    load();
    return () => {
      alive = false;
    };
  }, [baseUrl, token]);

  return (
    <AppShell>
      <h1 className="text-3xl font-extrabold text-gray-900 mb-6">Dashboard</h1>

      {err ? (
        <div className="mb-5 rounded-xl border border-red-100 bg-red-50 px-4 py-3 text-sm text-red-700">
          {err}
        </div>
      ) : null}

      {/* KPI cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-6 mb-8">
        <StatCard
          title="Total Product"
          value={loading ? "…" : formatNumber(totals.products)}
          icon={<Box size={18} />}
          tone="amber"
        />
        <StatCard
          title="Total Keyword"
          value={loading ? "…" : formatNumber(totals.keywords)}
          icon={<BarChart3 size={18} />}
          tone="green"
        />
        <StatCard
          title="Total Account"
          value={loading ? "…" : formatNumber(totals.accounts)}
          icon={<Users size={18} />}
          tone="indigo"
        />
        <StatCard
          title="Total Proxy"
          value={loading ? "…" : formatNumber(totals.proxies)}
          icon={<Timer size={18} />}
          tone="orange"
        />
      </div>

      {/* Product List */}
      <ProductTable />
    </AppShell>
  );
}
