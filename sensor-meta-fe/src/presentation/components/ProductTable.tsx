import { useMemo, useState } from "react";
// import { ChevronDown } from "lucide-react";
import { useListingsQuery } from "../../presentation/hooks/useListingsQuery";

type UiRow = {
  name: string;
  location: string;
  dateTime: string;
  link: string | null; // ✅ NEW
  linkLabel: string;
  price: string;
  status: "Active" | "Inactive";
};

function StatusPill({ status }: { status: UiRow["status"] }) {
  const cls =
    status === "Active"
      ? "bg-emerald-500/15 text-emerald-600"
      : "bg-red-500/15 text-red-600";

  return (
    <span className={`inline-flex items-center justify-center px-4 py-1.5 rounded-full text-xs font-semibold ${cls}`}>
      {status}
    </span>
  );
}

function fmtDateTime(iso: string) {
  const d = new Date(iso);
  const dd = String(d.getDate()).padStart(2, "0");
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const yyyy = d.getFullYear();
  let hh = d.getHours();
  const min = String(d.getMinutes()).padStart(2, "0");
  const ampm = hh >= 12 ? "PM" : "AM";
  hh = hh % 12;
  if (hh === 0) hh = 12;
  return `${dd}.${mm}.${yyyy} - ${hh}.${min} ${ampm}`;
}

function fmtPrice(price: number | null, currency: string | null) {
  if (price == null) return "-";
  const cur = (currency || "USD").toUpperCase();
  try {
    return new Intl.NumberFormat("en-US", { style: "currency", currency: cur }).format(price);
  } catch {
    return `${price} ${cur}`;
  }
}

export default function ProductTable() {
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const [q, setQ] = useState("");
  const [active, setActive] = useState<"" | "true" | "false">("");

  const queryParams = useMemo(() => {
    return {
      page,
      pageSize,
      q: q.trim() ? q.trim() : undefined,
      active: active === "" ? undefined : active === "true",
    };
  }, [page, pageSize, q, active]);

  const { data, isLoading, isError, error, isFetching } = useListingsQuery(queryParams);

  const rows: UiRow[] = useMemo(() => {
    const items = data?.items ?? [];
    return items.map((it) => ({
      name: it.title || "(No title)",
      location: it.location || "-",
      dateTime: fmtDateTime(it.lastSeen || it.firstSeen),
      link: (it as any).link ?? null, // ✅ lấy từ backend (dto đã update thì bỏ any)
      linkLabel: "View",
      price: fmtPrice(it.price, it.currency),
      status: it.isActive ? "Active" : "Inactive",
    }));
  }, [data]);

  const total = data?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  return (
    <div className="rounded-2xl bg-white border border-gray-100 shadow-[0_20px_70px_-45px_rgba(0,0,0,0.25)]">
      <div className="px-6 py-5 flex items-center justify-between">
        <div className="text-lg font-bold text-gray-900">
          Product List {isFetching ? <span className="text-xs text-gray-400 font-medium">(loading)</span> : null}
        </div>

        {/* <button className="flex items-center gap-2 text-sm text-gray-500 border border-gray-100 rounded-lg px-3 py-2 hover:bg-gray-50">
          October
          <ChevronDown size={16} />
        </button> */}
      </div>

      <div className="px-6 pb-3 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <input
          className="w-full sm:max-w-sm rounded-lg bg-indigo-50/60 px-4 py-2.5 text-sm text-gray-900 outline-none ring-1 ring-indigo-100 focus:ring-2 focus:ring-indigo-200"
          value={q}
          onChange={(e) => {
            setPage(1);
            setQ(e.target.value);
          }}
          placeholder="Search title..."
        />

        <select
          className="w-full sm:w-44 rounded-lg bg-white px-3 py-2.5 text-sm text-gray-700 ring-1 ring-gray-200 outline-none"
          value={active}
          onChange={(e) => {
            setPage(1);
            setActive(e.target.value as any);
          }}
        >
          <option value="">All</option>
          <option value="true">Active</option>
          <option value="false">Inactive</option>
        </select>
      </div>

      <div className="px-6 pb-6">
        <div className="rounded-xl bg-[#F7F9FC] px-4 py-3">
          <div className="grid grid-cols-12 text-xs font-semibold text-gray-500">
            <div className="col-span-4">Product Name</div>
            <div className="col-span-2">Location</div>
            <div className="col-span-3">Date - Time</div>
            <div className="col-span-1">Link</div>
            <div className="col-span-1 text-right">Price</div>
            <div className="col-span-1 text-right">Status</div>
          </div>
        </div>

        {isLoading ? (
          <div className="py-10 text-sm text-gray-500">Loading...</div>
        ) : isError ? (
          <div className="py-10 text-sm text-red-600">
            {(error as any)?.response?.data?.error || (error as any)?.message || "Failed to load listings"}
          </div>
        ) : rows.length === 0 ? (
          <div className="py-10 text-sm text-gray-500">No data</div>
        ) : (
          <div className="divide-y divide-gray-100">
            {rows.map((r, idx) => (
              <div key={idx} className="grid grid-cols-12 items-center px-2 py-4">
                <div className="col-span-4 flex items-center gap-3">
                  {/* <div className="h-9 w-9 rounded-full bg-gray-200" /> */}
                  <div className="text-sm font-medium text-gray-900 line-clamp-1">{r.name}</div>
                </div>

                <div className="col-span-2 text-sm text-gray-600 line-clamp-1">{r.location}</div>
                <div className="col-span-3 text-sm text-gray-600">{r.dateTime}</div>

                <div className="col-span-1 text-sm">
                  {r.link ? (
                    <a
                      href={r.link}
                      target="_blank"
                      rel="noreferrer"
                      className="text-blue-600 hover:underline"
                    >
                      {r.linkLabel}
                    </a>
                  ) : (
                    <span className="text-gray-400">-</span>
                  )}
                </div>

                <div className="col-span-1 text-sm text-gray-700 text-right">{r.price}</div>

                <div className="col-span-1 flex justify-end">
                  <StatusPill status={r.status} />
                </div>
              </div>
            ))}
          </div>
        )}

        <div className="mt-4 flex items-center justify-between text-sm text-gray-600">
          <div>
            Page <span className="font-semibold text-gray-900">{data?.page ?? page}</span> / {totalPages} • Total{" "}
            <span className="font-semibold text-gray-900">{total}</span>
          </div>

          <div className="flex items-center gap-2">
            <button
              className="rounded-lg border border-gray-200 px-3 py-2 hover:bg-gray-50 disabled:opacity-50"
              disabled={page <= 1}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
            >
              Prev
            </button>
            <button
              className="rounded-lg border border-gray-200 px-3 py-2 hover:bg-gray-50 disabled:opacity-50"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            >
              Next
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
