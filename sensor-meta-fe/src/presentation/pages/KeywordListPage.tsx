import AppShell from "../layout/AppShell";
import KeywordTable from "../components/KeywordTable";

export default function KeywordListPage() {
  return (
    <AppShell>
      <h1 className="text-2xl font-extrabold text-gray-900 mb-4">Keyword Lists</h1>
      <KeywordTable />
    </AppShell>
  );
}
