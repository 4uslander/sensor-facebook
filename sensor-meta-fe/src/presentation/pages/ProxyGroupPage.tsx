import AppShell from "../layout/AppShell";
import ProxyGroupTable  from "../components/ProxyGroupTable";

export default function ProxyGroupPage() {
  return (
    <AppShell>
      <h1 className="text-2xl font-extrabold text-gray-900 mb-4">Proxy Group Lists</h1>
      <ProxyGroupTable />
    </AppShell>
  );
}
