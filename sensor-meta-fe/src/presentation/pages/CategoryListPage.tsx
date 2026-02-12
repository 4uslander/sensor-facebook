import CategoryTable from "../components/CategoryTable";
import AppShell from "../layout/AppShell";

export default function CategoryListPage() {
    return (
        <AppShell>
            <h1 className="text-2xl font-extrabold text-gray-900 mb-4">Category Lists</h1>
            <CategoryTable />
        </AppShell>
    );
}
