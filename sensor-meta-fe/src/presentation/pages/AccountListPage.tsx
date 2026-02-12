import FacebookTable from "../components/FacebookAccountTable";
import AppShell from "../layout/AppShell";

export default function FacebookListPage() {
    return (
        <AppShell>
            <h1 className="text-2xl font-extrabold text-gray-900 mb-4">Facebook Account Lists</h1>
            <FacebookTable />
        </AppShell>
    );
}
