import JobTable from "../components/JobQueueTable";
import AppShell from "../layout/AppShell";

export default function JobListPage() {
    return (
        <AppShell>
            <h1 className="text-2xl font-extrabold text-gray-900 mb-4">Job Lists</h1>
            <JobTable />
        </AppShell>
    );
}
