// src/presentation/components/JobQueueTable.tsx
import { ChevronDown, ChevronLeft, ChevronRight, Filter, Search, Play } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import type { MutableRefObject, RefObject } from "react";

import { searchJobsApi, type JobListItemDto } from "../../infrastructure/api/searchJobsApi";

/* -------------------- utils -------------------- */
function cn(...s: Array<string | false | null | undefined>) {
  return s.filter(Boolean).join(" ");
}

function useOnClickOutside(
  refs: Array<RefObject<HTMLElement | null> | MutableRefObject<HTMLElement | null>>,
  handler: () => void
) {
  useEffect(() => {
    function onDown(e: MouseEvent) {
      const target = e.target as Node;
      const inside = refs.some((r) => r.current && r.current.contains(target));
      if (!inside) handler();
    }
    document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, [refs, handler]);
}

function pad2(n: number) {
  return String(n).padStart(2, "0");
}

function getAccessTokenFromLocalStorage(): string | null {
  const raw = localStorage.getItem("sf_auth_tokens");
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as { accessToken?: string };
    return parsed?.accessToken ?? null;
  } catch {
    return null;
  }
}

function safeLower(s: string | null | undefined) {
  return String(s || "").toLowerCase();
}

function statusLabel(raw: string) {
  const s = safeLower(raw);
  if (s === "queued") return "Queued";
  if (s === "running") return "Running";
  if (s === "failed") return "Failed";
  if (s === "completed") return "Completed";
  return raw || "-";
}

function toIsoStartOfDay(dateISO: string) {
  return new Date(`${dateISO}T00:00:00.000Z`).toISOString();
}
function toIsoEndOfDay(dateISO: string) {
  return new Date(`${dateISO}T23:59:59.999Z`).toISOString();
}

function shortGuid(g: string) {
  if (!g) return "-";
  return g.length > 8 ? `${g.slice(0, 8)}…` : g;
}

function fmtTime(s: string | null) {
  if (!s) return "-";
  const d = new Date(s);
  if (Number.isNaN(d.getTime())) return s;
  return d.toLocaleString("en-US", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

/* -------------------- Date picker (UI demo giữ nguyên) -------------------- */
function daysInMonth(year: number, month0: number) {
  return new Date(year, month0 + 1, 0).getDate();
}
function firstWeekday(year: number, month0: number) {
  return new Date(year, month0, 1).getDay();
}
function fmtMonthYear(year: number, month0: number) {
  return new Date(year, month0, 1).toLocaleString("en-US", { month: "long", year: "numeric" });
}
function toISODate(y: number, m0: number, d: number) {
  return `${y}-${pad2(m0 + 1)}-${pad2(d)}`;
}

function CalendarPopover({
  value,
  onChange,
  onApply,
}: {
  value: string[];
  onChange: (v: string[]) => void;
  onApply: () => void;
}) {
  const [year, setYear] = useState(2019);
  const [month0, setMonth0] = useState(1);

  const total = daysInMonth(year, month0);
  const start = firstWeekday(year, month0);

  const cells: Array<number | null> = [];
  for (let i = 0; i < start; i++) cells.push(null);
  for (let d = 1; d <= total; d++) cells.push(d);
  while (cells.length % 7 !== 0) cells.push(null);

  function toggleDay(d: number) {
    const iso = toISODate(year, month0, d);
    onChange(value.includes(iso) ? value.filter((x) => x !== iso) : [...value, iso]);
  }

  return (
    <div className="w-[340px] max-w-[calc(100vw-40px)] rounded-2xl bg-white shadow-[0_25px_70px_-40px_rgba(0,0,0,0.35)] border border-gray-100 p-6">
      <div className="flex items-center justify-between mb-3">
        <div className="text-xs font-semibold text-gray-700">{fmtMonthYear(year, month0)}</div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => {
              const nm = month0 - 1;
              if (nm < 0) {
                setYear((y) => y - 1);
                setMonth0(11);
              } else setMonth0(nm);
            }}
            className="h-7 w-7 rounded-md border border-gray-100 grid place-items-center hover:bg-gray-50"
          >
            <ChevronLeft size={14} className="text-gray-500" />
          </button>
          <button
            type="button"
            onClick={() => {
              const nm = month0 + 1;
              if (nm > 11) {
                setYear((y) => y + 1);
                setMonth0(0);
              } else setMonth0(nm);
            }}
            className="h-7 w-7 rounded-md border border-gray-100 grid place-items-center hover:bg-gray-50"
          >
            <ChevronRight size={14} className="text-gray-500" />
          </button>
        </div>
      </div>

      <div className="grid grid-cols-7 gap-1 text-[10px] text-gray-400 mb-2">
        {["S", "M", "T", "W", "T", "F", "S"].map((x) => (
          <div key={x} className="text-center font-semibold">
            {x}
          </div>
        ))}
      </div>

      <div className="grid grid-cols-7 gap-1">
        {cells.map((d, idx) => {
          if (!d) return <div key={idx} className="h-8" />;
          const iso = toISODate(year, month0, d);
          const selected = value.includes(iso);
          return (
            <button
              key={idx}
              type="button"
              onClick={() => toggleDay(d)}
              className={cn(
                "h-8 rounded-lg text-xs font-semibold transition",
                selected ? "bg-blue-600 text-white" : "text-gray-700 hover:bg-gray-50"
              )}
            >
              {d}
            </button>
          );
        })}
      </div>

      <div className="mt-3 text-[11px] text-gray-400">*You can choose multiple date</div>

      <div className="mt-4 flex justify-center">
        <button
          type="button"
          onClick={onApply}
          className="h-9 min-w-[120px] rounded-lg bg-blue-600 text-white text-xs font-semibold hover:bg-blue-700"
        >
          Apply Now
        </button>
      </div>
    </div>
  );
}

/* -------------------- small UI -------------------- */
type OpenKey = null | "date" | "status";

function SelectPill({
  label,
  active,
  onClick,
}: {
  label: string;
  active?: boolean;
  onClick?: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "inline-flex items-center gap-2 rounded-lg border px-3 py-2 text-xs font-medium",
        active ? "border-blue-100 bg-blue-50 text-blue-700" : "border-gray-100 bg-white text-gray-600 hover:bg-gray-50"
      )}
    >
      {label}
      <ChevronDown size={14} className={cn(active ? "text-blue-600" : "text-gray-400")} />
    </button>
  );
}

function Chip({
  label,
  selected,
  onClick,
}: {
  label: string;
  selected?: boolean;
  onClick?: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "min-w-[110px] rounded-full border px-4 py-1.5 text-xs font-semibold transition",
        selected ? "bg-blue-600 border-blue-600 text-white" : "bg-white border-gray-200 text-gray-700 hover:bg-gray-50"
      )}
    >
      {label}
    </button>
  );
}

function PopoverCard({
  title,
  note,
  children,
  onApply,
}: {
  title: string;
  note?: string;
  children: React.ReactNode;
  onApply: () => void;
}) {
  return (
    <div className="w-[520px] max-w-[calc(100vw-40px)] rounded-2xl bg-white shadow-[0_25px_70px_-40px_rgba(0,0,0,0.35)] border border-gray-100 p-6">
      <div className="text-sm font-bold text-gray-900 mb-4">{title}</div>
      {children}
      {note ? <div className="mt-3 text-[11px] text-gray-400">{note}</div> : null}
      <div className="mt-5 flex justify-center">
        <button
          type="button"
          onClick={onApply}
          className="h-9 min-w-[120px] rounded-lg bg-blue-600 text-white text-xs font-semibold hover:bg-blue-700"
        >
          Apply Now
        </button>
      </div>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const s = safeLower(status);
  const cls =
    s === "completed"
      ? "bg-green-500 text-white"
      : s === "running"
      ? "bg-blue-500 text-white"
      : s === "failed"
      ? "bg-red-500 text-white"
      : "bg-amber-500 text-white";

  return (
    <span className={cn("inline-flex h-8 items-center justify-center rounded-lg px-4 text-xs font-semibold", cls)}>
      {statusLabel(status)}
    </span>
  );
}

/* -------------------- main -------------------- */
export default function JobQueueTable() {
  const baseUrl = import.meta.env.VITE_API_URL || "https://localhost:7141";
  const token = getAccessTokenFromLocalStorage();

  const [items, setItems] = useState<JobListItemDto[]>([]);
  const [total, setTotal] = useState(0);

  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);

  const [q, setQ] = useState("");
  const [open, setOpen] = useState<OpenKey>(null);

  const [draftDates, setDraftDates] = useState<string[]>([]);
  const [dates, setDates] = useState<string[]>([]);

  const [draftStatus, setDraftStatus] = useState<Array<"queued" | "running" | "failed" | "completed">>([]);
  const [status, setStatus] = useState<Array<"queued" | "running" | "failed" | "completed">>([]);

  const [loading, setLoading] = useState(false);
  const [actionLoadingKeyword, setActionLoadingKeyword] = useState<number | null>(null);
  const [err, setErr] = useState<string | null>(null);

  const containerRef = useRef<HTMLDivElement | null>(null);
  const anchorDate = useRef<HTMLDivElement | null>(null);
  const anchorStatus = useRef<HTMLDivElement | null>(null);
  const popoverRef = useRef<HTMLDivElement | null>(null);
  const [popPos, setPopPos] = useState<{ left: number; top: number }>({ left: 0, top: 0 });

  function calcPos(anchor: RefObject<HTMLDivElement | null>, popWidth: number) {
    const container = containerRef.current;
    const el = anchor.current;
    if (!container || !el) return;

    const c = container.getBoundingClientRect();
    const a = el.getBoundingClientRect();

    const top = a.bottom - c.top + 10;
    let left = a.left - c.left;

    const minLeft = 12;
    const maxLeft = Math.max(minLeft, c.width - popWidth - 12);
    left = Math.max(minLeft, Math.min(left, maxLeft));

    setPopPos({ left, top });
  }

  useOnClickOutside([popoverRef, anchorDate, anchorStatus], () => setOpen(null));

  useEffect(() => {
    function onRelayout() {
      if (open === "date") calcPos(anchorDate, 340);
      if (open === "status") calcPos(anchorStatus, 520);
    }
    window.addEventListener("resize", onRelayout);
    window.addEventListener("scroll", onRelayout, true);
    return () => {
      window.removeEventListener("resize", onRelayout);
      window.removeEventListener("scroll", onRelayout, true);
    };
  }, [open]);

  function resetFilters() {
    setQ("");
    setDates([]);
    setStatus([]);
    setDraftDates([]);
    setDraftStatus([]);
    setOpen(null);
    setPage(1);
  }

  function openDate() {
    const next = open === "date" ? null : "date";
    setDraftDates(dates);
    setOpen(next);
    if (next) requestAnimationFrame(() => calcPos(anchorDate, 340));
  }

  function openStatus() {
    const next = open === "status" ? null : "status";
    setDraftStatus(status);
    setOpen(next);
    if (next) requestAnimationFrame(() => calcPos(anchorStatus, 520));
  }

  const dateRange = useMemo(() => {
    if (!dates.length) return { from: null as string | null, to: null as string | null };
    const sorted = [...dates].sort();
    return { from: toIsoStartOfDay(sorted[0]), to: toIsoEndOfDay(sorted[sorted.length - 1]) };
  }, [dates]);

  async function fetchList() {
    setLoading(true);
    setErr(null);
    try {
      const api = searchJobsApi(baseUrl, token || undefined);
      const statusParam = status.length === 1 ? status[0] : null;

      const res = await api.list({
        page,
        pageSize,
        status: statusParam,
        keywordId: null,
        from: dateRange.from,
        to: dateRange.to,
      });

      setItems(res.items);
      setTotal(res.total);
    } catch (e: any) {
      setErr(e?.message || "Load failed");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    fetchList();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, status.join(","), dateRange.from, dateRange.to]);

  async function runNow(keywordId: number) {
    if (!keywordId || keywordId <= 0) return;
    try {
      setErr(null);
      setActionLoadingKeyword(keywordId);

      const api = searchJobsApi(baseUrl, token || undefined);

      // priority mặc định high theo BE
      await api.runNow(keywordId, "high");

      await fetchList();
    } catch (e: any) {
      setErr(e?.message || "Run now failed");
    } finally {
      setActionLoadingKeyword(null);
    }
  }

  const filteredRows = useMemo(() => {
    const qq = q.trim().toLowerCase();
    if (!qq) return items;

    return items.filter((r) => {
      const k = String(r.keywordId ?? "");
      return (
        r.id.toLowerCase().includes(qq) ||
        k.includes(qq) ||
        safeLower(r.status).includes(qq) ||
        String(r.attempts ?? "").includes(qq)
      );
    });
  }, [items, q]);

  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  const showingText = useMemo(() => {
    const start = total === 0 ? 0 : (page - 1) * pageSize + 1;
    const end = Math.min(page * pageSize, total);
    return `Showing ${start}-${String(end).padStart(2, "0")} of ${total}`;
  }, [page, pageSize, total]);

  return (
    <div ref={containerRef} className="relative rounded-2xl bg-white border border-gray-100 shadow-[0_20px_70px_-45px_rgba(0,0,0,0.25)]">
      {/* Filters bar */}
      <div className="px-6 pt-5">
        <div className="flex items-center justify-between gap-4">
          <div className="flex flex-wrap items-center gap-0 rounded-xl border border-gray-100 overflow-hidden bg-white">
            <div className="h-11 px-4 grid place-items-center border-r border-gray-100">
              <Filter size={16} className="text-gray-500" />
            </div>

            <div className="h-11 px-4 flex items-center gap-2 border-r border-gray-100">
              <Search size={14} className="text-gray-400" />
              <input
                value={q}
                onChange={(e) => setQ(e.target.value)}
                placeholder="Search (jobId / keywordId / status)"
                className="h-9 w-56 bg-transparent text-xs outline-none"
              />
            </div>

            <div className="h-11 px-5 flex items-center text-xs font-semibold text-gray-600 border-r border-gray-100">
              Filter By
            </div>

            <div ref={anchorDate} className="h-11 px-3 flex items-center border-r border-gray-100">
              <SelectPill label="Date" active={open === "date"} onClick={openDate} />
            </div>

            <div ref={anchorStatus} className="h-11 px-3 flex items-center border-r border-gray-100">
              <SelectPill label="Status" active={open === "status"} onClick={openStatus} />
            </div>

            <button
              type="button"
              onClick={resetFilters}
              className="h-11 px-4 inline-flex items-center gap-2 text-xs font-semibold text-red-500 hover:bg-red-50"
            >
              Reset Filter
            </button>
          </div>

          <button
            type="button"
            onClick={fetchList}
            className="h-11 px-4 rounded-xl border border-gray-100 bg-white text-xs font-semibold text-gray-700 hover:bg-gray-50 inline-flex items-center gap-2"
            disabled={loading}
          >
            Refresh
          </button>
        </div>

        {err ? (
          <div className="mt-4 rounded-xl border border-red-100 bg-red-50 px-4 py-3 text-sm text-red-700">
            {err}
          </div>
        ) : null}
      </div>

      {/* Table */}
      <div className="px-6 pb-2 pt-4">
        <div className="overflow-hidden rounded-xl border border-gray-100">
          <div className="grid grid-cols-12 bg-gray-50 px-4 py-3 text-[11px] font-semibold text-gray-500 border-b border-gray-100">
            <div className="col-span-2">Job ID</div>
            <div className="col-span-2 text-center">KeywordId</div>
            <div className="col-span-1 text-center">Attempts</div>
            <div className="col-span-2 text-center">Status</div>
            <div className="col-span-3 text-center">Scheduled At</div>
            <div className="col-span-2 text-center">Action</div>
          </div>

          <div className="divide-y divide-gray-100 bg-white">
            {loading ? <div className="px-4 py-10 text-sm text-gray-500">Loading...</div> : null}

            {!loading &&
              filteredRows.map((r) => {
                const keywordId = r.keywordId ?? 0;
                const canRun = keywordId > 0;
                const actionLoading = actionLoadingKeyword === keywordId && canRun;

                return (
                  <div key={r.id} className="grid grid-cols-12 items-center px-4 py-4 text-sm">
                    <div className="col-span-2 text-gray-700" title={r.id}>
                      {shortGuid(r.id)}
                    </div>

                    <div className="col-span-2 text-center text-gray-700">{canRun ? keywordId : "-"}</div>
                    <div className="col-span-1 text-center text-gray-700">{r.attempts ?? 0}</div>

                    <div className="col-span-2 flex justify-center">
                      <StatusBadge status={r.status} />
                    </div>

                    <div className="col-span-3 text-center text-gray-700">{fmtTime(r.scheduledAt)}</div>

                    <div className="col-span-2 flex justify-center">
                      <button
                        type="button"
                        disabled={!canRun || loading || actionLoading}
                        onClick={() => runNow(keywordId)}
                        className={cn(
                          "h-9 rounded-lg px-4 text-xs font-semibold border inline-flex items-center gap-2",
                          canRun
                            ? "border-blue-100 bg-blue-50 text-blue-700 hover:bg-blue-100"
                            : "border-gray-100 bg-gray-50 text-gray-400 cursor-not-allowed"
                        )}
                        title={canRun ? "Run search job now for this keyword" : "No keywordId available"}
                      >
                        <Play size={14} className={cn(canRun ? "text-blue-600" : "text-gray-400")} />
                        {actionLoading ? "Running..." : "Run Now"}
                      </button>
                    </div>
                  </div>
                );
              })}

            {!loading && filteredRows.length === 0 ? <div className="px-4 py-10 text-sm text-gray-500">No data</div> : null}
          </div>
        </div>

        {/* Footer */}
        <div className="flex items-center justify-between py-3 text-xs text-gray-500">
          <div>{showingText}</div>
          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={page <= 1 || loading}
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              className="h-8 w-8 rounded-lg border border-gray-100 bg-white grid place-items-center hover:bg-gray-50 disabled:opacity-50"
            >
              <ChevronLeft size={16} className="text-gray-500" />
            </button>
            <button
              type="button"
              disabled={page >= totalPages || loading}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              className="h-8 w-8 rounded-lg border border-gray-100 bg-white grid place-items-center hover:bg-gray-50 disabled:opacity-50"
            >
              <ChevronRight size={16} className="text-gray-500" />
            </button>
          </div>
        </div>
      </div>

      {/* Popover layer */}
      {open ? (
        <div className="absolute inset-0 pointer-events-none">
          <div ref={popoverRef} className="pointer-events-auto">
            {open === "date" ? (
              <div className="absolute z-50" style={{ left: popPos.left, top: popPos.top }}>
                <CalendarPopover
                  value={draftDates}
                  onChange={setDraftDates}
                  onApply={() => {
                    setDates(draftDates);
                    setPage(1);
                    setOpen(null);
                  }}
                />
              </div>
            ) : null}

            {open === "status" ? (
              <div className="absolute z-50" style={{ left: popPos.left, top: popPos.top }}>
                <PopoverCard
                  title="Select Status"
                  note="*Backend currently supports single status filter. Choose 1; selecting multiple will behave like no status filter."
                  onApply={() => {
                    setStatus(draftStatus);
                    setPage(1);
                    setOpen(null);
                  }}
                >
                  <div className="flex flex-wrap gap-3">
                    {(["queued", "running", "failed", "completed"] as const).map((x) => (
                      <Chip
                        key={x}
                        label={statusLabel(x)}
                        selected={draftStatus.includes(x)}
                        onClick={() =>
                          setDraftStatus((prev) => (prev.includes(x) ? prev.filter((t) => t !== x) : [...prev, x]))
                        }
                      />
                    ))}
                  </div>
                </PopoverCard>
              </div>
            ) : null}
          </div>
        </div>
      ) : null}
    </div>
  );
}
