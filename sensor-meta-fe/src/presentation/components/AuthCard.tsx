export function AuthCard({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="w-full max-w-md">
      <h2 className="mb-6 text-2xl font-semibold text-gray-900">{title}</h2>
      <div className="rounded-xl bg-white p-2">
        {children}
      </div>
    </div>
  );
}
