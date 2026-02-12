// src/presentation/components/FacebookAccountTable.tsx
import {
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Filter,
  Pencil,
  Search,
  Trash2,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import type { MutableRefObject, RefObject } from "react";
import FacebookAccountModal from "./FacebookAccountModal";
import type { FbAccountPayload } from "./FacebookAccountModal";
import { authStorage } from "../../shared//lib/authStorage";
import { accountsApi, type FbAccountDto } from "../../infrastructure/api/accountsApi";

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

function fmtDate(d?: string | null) {
  if (!d) return "—";
  const t = Date.parse(d);
  if (!Number.isFinite(t)) return "—";
  return new Date(t).toLocaleDateString("en-GB", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

function toUiStatus(s: unknown): "Active" | "Inactive" {
  // Backend có thể trả enum number (0/1/2/3) nếu chưa bật JsonStringEnumConverter
  if (typeof s === "number") {
    // Giả định enum: Active=0, Suspended/Inactive=1, Checkpointed=2, Disabled=3
    return s === 0 ? "Active" : "Inactive";
  }

  const v = String(s ?? "").trim().toLowerCase();

  // trường hợp enum number nhưng JSON bị stringify thành "0"
  if (v === "0") return "Active";
  if (v === "1" || v === "2" || v === "3") return "Inactive";

  // trường hợp enum string
  if (v === "active") return "Active";
  if (v === "inactive" || v === "suspended" || v === "checkpoint" || v === "checkpointed" || v === "disabled")
    return "Inactive";

  // fallback
  return "Inactive";
}


function toServerStatus(ui: "Active" | "Inactive") {
  return ui === "Active" ? "active" : "inactive";
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
        active
          ? "border-blue-100 bg-blue-50 text-blue-700"
          : "border-gray-100 bg-white text-gray-600 hover:bg-gray-50"
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
        "min-w-[86px] rounded-full border px-4 py-1.5 text-xs font-semibold transition",
        selected
          ? "bg-blue-600 border-blue-600 text-white"
          : "bg-white border-gray-200 text-gray-700 hover:bg-gray-50"
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

function Toggle({ value, onChange }: { value: boolean; onChange: (v: boolean) => void }) {
  return (
    <button
      type="button"
      onClick={() => onChange(!value)}
      className={cn(
        "relative inline-flex h-5 w-9 items-center rounded-full transition",
        value ? "bg-blue-600" : "bg-gray-200"
      )}
      aria-label="toggle"
    >
      <span
        className={cn(
          "inline-block h-4 w-4 rounded-full bg-white shadow-sm transition",
          value ? "translate-x-4" : "translate-x-1"
        )}
      />
    </button>
  );
}

/* -------------------- confirm delete modal (inline) -------------------- */
function ConfirmDeleteModal({
  open,
  title = "Delete Account",
  message,
  confirmText = "Delete",
  cancelText = "Cancel",
  loading,
  onCancel,
  onConfirm,
}: {
  open: boolean;
  title?: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  loading?: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onCancel();
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [open, onCancel]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-[150]">
      <button
        type="button"
        aria-label="Close"
        onClick={onCancel}
        className="absolute inset-0 bg-black/30"
      />
      <div className="absolute inset-0 grid place-items-center px-4">
        <div className="w-[420px] max-w-full rounded-2xl bg-white border border-gray-100 shadow-[0_30px_90px_-45px_rgba(0,0,0,0.55)]">
          <div className="flex items-center justify-between px-5 py-4 border-b border-gray-100">
            <div className="text-sm font-bold text-gray-900">{title}</div>
            <button
              type="button"
              onClick={onCancel}
              className="h-8 w-8 rounded-lg border border-gray-100 grid place-items-center hover:bg-gray-50"
              aria-label="Close modal"
            >
              <span className="text-gray-600 text-sm">×</span>
            </button>
          </div>

          <div className="px-5 py-4">
            <div className="text-sm text-gray-700">{message}</div>

            <div className="mt-5 flex justify-end gap-2">
              <button
                type="button"
                onClick={onCancel}
                disabled={loading}
                className={cn(
                  "h-9 rounded-lg border px-4 text-xs font-semibold",
                  loading
                    ? "border-gray-200 text-gray-400 cursor-not-allowed"
                    : "border-gray-200 text-gray-700 hover:bg-gray-50"
                )}
              >
                {cancelText}
              </button>

              <button
                type="button"
                onClick={onConfirm}
                disabled={loading}
                className={cn(
                  "h-9 rounded-lg px-4 text-xs font-semibold text-white",
                  loading ? "bg-red-300 cursor-not-allowed" : "bg-red-500 hover:bg-red-600"
                )}
              >
                {loading ? "Deleting..." : confirmText}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

/* -------------------- Date picker (demo UI - same pattern) -------------------- */
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

/* -------------------- types -------------------- */
type AccountRow = {
  id: string;
  email: string;
  displayName: string;
  proxyGroupId: number;
  proxyGroupName: string;
  region: string;
  profileDir: string;
  cookiePlain: string; // not returned from server
  status: "Active" | "Inactive";
  lastCheckpoint: string;
  active: boolean;
};

function mapDtoToRow(dto: FbAccountDto): AccountRow {
  const uiStatus = toUiStatus(dto.status);
  return {
    id: dto.id,
    email: dto.email,
    displayName: dto.displayName ?? "",
    proxyGroupId: dto.proxyGroupId ?? 0,
    proxyGroupName: dto.proxyGroupName ?? "",
    region: dto.region ?? "",
    profileDir: dto.profileDir ?? "",
    cookiePlain: "",
    status: uiStatus,
    lastCheckpoint: fmtDate(dto.lastCheckpoint),
    active: uiStatus === "Active",
  };
}

/* -------------------- main -------------------- */
export default function FacebookAccountTable() {
  const baseUrl = import.meta.env.VITE_API_URL || "https://localhost:7141";
  const token = authStorage.getAccessToken() || undefined;
  const api = useMemo(() => accountsApi(baseUrl, token), [baseUrl, token]);

  const [rows, setRows] = useState<AccountRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const [page, setPage] = useState(1);
  const pageSize = 20;
  const [total, setTotal] = useState(0);

  const [q, setQ] = useState("");
  const [open, setOpen] = useState<OpenKey>(null);

  const [draftDates, setDraftDates] = useState<string[]>([]);
  const [draftStatus, setDraftStatus] = useState<Array<"Active" | "Inactive">>([]);

  const [dates, setDates] = useState<string[]>([]);
  const [status, setStatus] = useState<Array<"Active" | "Inactive">>([]);

  // modals
  const [addOpen, setAddOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const [editing, setEditing] = useState<AccountRow | null>(null);

  // delete confirm
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleting, setDeleting] = useState<AccountRow | null>(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  // anchors + popover positioning
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

  // ---- load from server ----
  const load = async () => {
    setLoading(true);
    setErr(null);
    try {
      const serverStatus =
        status.length === 1 ? toServerStatus(status[0]) : undefined;

      const res = await api.list({
        page,
        pageSize,
        q: q.trim() ? q.trim() : undefined,
        status: serverStatus,
      });

      setTotal(res.total);
      setRows(res.items.map(mapDtoToRow));
    } catch (e: any) {
      setErr(e?.message || "Load failed");
    } finally {
      setLoading(false);
    }
  };

  // debounce q + status
  useEffect(() => {
    const t = setTimeout(() => {
      setPage(1);
      load();
    }, 350);
    return () => clearTimeout(t);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [q, status]);

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page]);

  // modal saved => reload
  async function handleSaved(_id: string) {
    setPage(1);
    await load();
  }

  function beginEdit(r: AccountRow) {
    setEditing(r);
    setEditOpen(true);
  }

  // delete: server has no DELETE. Use Disabled status as "soft delete"
  function requestDelete(r: AccountRow) {
    setDeleting(r);
    setDeleteOpen(true);
  }

  async function confirmDelete() {
    if (!deleting) return;
    try {
      setDeleteLoading(true);
      await api.updateStatus(deleting.id, "disabled");
      setDeleteOpen(false);
      setDeleting(null);
      await load();
    } catch (e: any) {
      setErr(e?.message || "Delete failed");
    } finally {
      setDeleteLoading(false);
    }
  }

  async function toggleActive(id: string, v: boolean) {
    setRows((prev) =>
      prev.map((r) =>
        r.id === id
          ? { ...r, active: v, status: v ? "Active" : "Inactive" }
          : r
      )
    );

    try {
      if (v) await api.unlock(id);
      else await api.lock(id);
    } catch (e: any) {
      setErr(e?.message || "Update status failed");
      await load();
    }
  }

  const showingText = useMemo(() => {
    const start = (page - 1) * pageSize + 1;
    const end = Math.min(page * pageSize, total);
    if (total === 0) return "Showing 0 of 0";
    return `Showing ${String(start).padStart(2, "0")}-${String(end).padStart(
      2,
      "0"
    )} of ${total}`;
  }, [page, pageSize, total]);

  const lockActions = addOpen || editOpen || deleteOpen || deleteLoading;

  return (
    <>
      <div
        ref={containerRef}
        className="relative rounded-2xl bg-white border border-gray-100 shadow-[0_20px_70px_-45px_rgba(0,0,0,0.25)]"
      >
        {err ? (
          <div className="px-6 pt-5">
            <div className="rounded-xl border border-red-100 bg-red-50 px-4 py-3 text-sm text-red-700">
              {err}
            </div>
          </div>
        ) : null}

        <div className="px-6 pt-5">
          <div className="flex items-center justify-between gap-4">
            <div className="flex flex-wrap items-center gap-2">
              <button
                type="button"
                className="inline-flex items-center gap-2 rounded-lg border border-gray-100 bg-white px-3 py-2 text-xs font-semibold text-gray-700 hover:bg-gray-50"
              >
                <Filter size={14} className="text-gray-500" />
                Filter By
              </button>

              <div className="relative">
                <Search
                  size={14}
                  className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
                />
                <input
                  value={q}
                  onChange={(e) => setQ(e.target.value)}
                  placeholder="Search"
                  className="h-9 w-56 rounded-lg border border-gray-100 bg-white pl-9 pr-3 text-xs outline-none focus:ring-2 focus:ring-blue-100"
                />
              </div>

              <div ref={anchorDate}>
                <SelectPill label="Date" active={open === "date"} onClick={openDate} />
              </div>

              <div ref={anchorStatus}>
                <SelectPill
                  label="Status"
                  active={open === "status"}
                  onClick={openStatus}
                />
              </div>

              <button
                type="button"
                onClick={resetFilters}
                className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-xs font-semibold text-red-500 hover:bg-red-50"
              >
                Reset Filter
              </button>

              {loading ? <div className="text-xs text-gray-400 ml-2">Loading…</div> : null}
            </div>

            <button
              type="button"
              onClick={() => setAddOpen(true)}
              className="rounded-lg bg-blue-600 px-4 py-2 text-xs font-semibold text-white hover:bg-blue-700"
              disabled={lockActions}
            >
              Add Account
            </button>
          </div>
        </div>

        <div className="px-6 pb-2 pt-4">
          <div className="overflow-hidden rounded-xl border border-gray-100">
            <div className="grid grid-cols-12 bg-white px-4 py-3 text-[11px] font-semibold text-gray-500 border-b border-gray-100">
              <div className="col-span-1">No.</div>
              <div className="col-span-3">Email</div>
              <div className="col-span-3">Proxy Group</div>
              <div className="col-span-2">Last Checkpoint</div>
              <div className="col-span-2">Status</div>
              <div className="col-span-1 text-right">Action</div>
            </div>

            <div className="divide-y divide-gray-100">
              {rows.map((r, idx) => {
                const no = String((page - 1) * pageSize + idx + 1).padStart(2, "0");

                return (
                  <div key={r.id} className="grid grid-cols-12 items-center px-4 py-3 text-sm">
                    <div className="col-span-1 text-gray-700">{no}</div>

                    <div className="col-span-3">
                      <div className="text-gray-900 font-medium">{r.email}</div>
                      <div className="text-[11px] text-gray-400 truncate">
                        {r.displayName || "—"}
                      </div>
                    </div>

                    <div className="col-span-3 text-gray-700">
                      {r.proxyGroupName ? (
                        <div>
                          <div className="text-gray-900">{r.proxyGroupName}</div>
                          <div className="text-[11px] text-gray-400">{r.region || "—"}</div>
                        </div>
                      ) : (
                        `Proxy ${r.proxyGroupId}`
                      )}
                    </div>

                    <div className="col-span-2 text-gray-600">{r.lastCheckpoint}</div>

                    <div className="col-span-2 flex items-center gap-2">
                      <Toggle value={r.active} onChange={(v) => toggleActive(r.id, v)} />
                      <span className="text-xs text-gray-600">
                        {r.active ? "Active" : "Inactive"}
                      </span>
                    </div>

                    <div className="col-span-1 flex justify-end gap-2">
                      <button
                        type="button"
                        disabled={lockActions}
                        onClick={() => beginEdit(r)}
                        className={cn(
                          "h-8 w-8 rounded-lg border border-gray-100 grid place-items-center",
                          lockActions ? "opacity-50 cursor-not-allowed" : "hover:bg-gray-50"
                        )}
                        title="Edit"
                      >
                        <Pencil size={14} className="text-gray-500" />
                      </button>

                      <button
                        type="button"
                        disabled={lockActions}
                        onClick={() => requestDelete(r)}
                        className={cn(
                          "h-8 w-8 rounded-lg border border-gray-100 grid place-items-center",
                          lockActions ? "opacity-50 cursor-not-allowed" : "hover:bg-red-50"
                        )}
                        title="Delete"
                      >
                        <Trash2 size={14} className="text-red-500" />
                      </button>
                    </div>
                  </div>
                );
              })}

              {!loading && rows.length === 0 ? (
                <div className="px-4 py-10 text-sm text-gray-500">No data</div>
              ) : null}
            </div>
          </div>

          <div className="flex items-center justify-between py-3 text-xs text-gray-500">
            <div>{showingText}</div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1 || loading}
                className={cn(
                  "h-8 w-8 rounded-lg border border-gray-100 bg-white grid place-items-center",
                  page <= 1 || loading ? "opacity-50 cursor-not-allowed" : "hover:bg-gray-50"
                )}
              >
                <ChevronLeft size={16} className="text-gray-500" />
              </button>

              <button
                type="button"
                onClick={() => {
                  const maxPage = Math.max(1, Math.ceil(total / pageSize));
                  setPage((p) => Math.min(maxPage, p + 1));
                }}
                disabled={loading || page >= Math.ceil(Math.max(total, 1) / pageSize)}
                className={cn(
                  "h-8 w-8 rounded-lg border border-gray-100 bg-white grid place-items-center",
                  loading || page >= Math.ceil(Math.max(total, 1) / pageSize)
                    ? "opacity-50 cursor-not-allowed"
                    : "hover:bg-gray-50"
                )}
              >
                <ChevronRight size={16} className="text-gray-500" />
              </button>
            </div>
          </div>
        </div>

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
                      setOpen(null);
                    }}
                  />
                </div>
              ) : null}

              {open === "status" ? (
                <div className="absolute z-50" style={{ left: popPos.left, top: popPos.top }}>
                  <PopoverCard
                    title="Select Status"
                    note="*API supports single status. If you choose 2, it will not filter server-side."
                    onApply={() => {
                      setStatus(draftStatus);
                      setOpen(null);
                      setPage(1);
                    }}
                  >
                    <div className="flex flex-wrap gap-3">
                      {(["Active", "Inactive"] as const).map((x) => (
                        <Chip
                          key={x}
                          label={x}
                          selected={draftStatus.includes(x)}
                          onClick={() =>
                            setDraftStatus((prev) =>
                              prev.includes(x) ? prev.filter((t) => t !== x) : [...prev, x]
                            )
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

      {/* Add modal: modal tự POST /api/accounts */}
      <FacebookAccountModal
        open={addOpen}
        mode="add"
        onClose={() => setAddOpen(false)}
        onSaved={handleSaved}
        apiBase={baseUrl}
        token={token ?? null}
      />

      {/* Edit modal */}
      <FacebookAccountModal
        open={editOpen}
        mode="edit"
        initial={
          editing
            ? ({
                id: editing.id,
                email: editing.email,
                displayName: editing.displayName,
                proxyGroupId: editing.proxyGroupId,
                profileDir: editing.profileDir,
                cookiePlain: "",
                status: editing.status,
              } as FbAccountPayload)
            : undefined
        }
        onClose={() => {
          setEditOpen(false);
          setEditing(null);
        }}
        onSaved={handleSaved}
        apiBase={baseUrl}
        token={token ?? null}
      />

      <ConfirmDeleteModal
        open={deleteOpen}
        loading={deleteLoading}
        title="Disable Account"
        confirmText="Disable"
        message={
          deleting
            ? `Disable "${deleting.email}"? (Server has no DELETE; this will set status=disabled.)`
            : "Disable this account?"
        }
        onCancel={() => {
          if (deleteLoading) return;
          setDeleteOpen(false);
          setDeleting(null);
        }}
        onConfirm={confirmDelete}
      />
    </>
  );
}
