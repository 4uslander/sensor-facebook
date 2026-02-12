// src/presentation/components/KeywordTable.tsx

import {
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Filter,
  Pencil,
  Trash2,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import type { MutableRefObject, RefObject } from "react";

import AddKeywordModal, { type AddKeywordPayload } from "./AddKeywordModal";
import KeywordEditDrawer from "./KeywordEditDrawer";
import type { KeywordRow } from "./KeywordEditDrawer";

import { useCreateKeywordMutation } from "../hooks/useCreateKeywordMutation";
import { http } from "../../infrastructure/http/httpClient";

/* -------------------- types -------------------- */
type Row = {
  id: string;
  text: string;
  category: string;
  priority: "High" | "Medium" | "Low";
  nextRun: string;
  active: boolean;
};

type OpenKey = null | "date" | "category" | "priority" | "status";

/* -------------------- mapping helpers (UI <-> API) -------------------- */
const CATEGORY_OPTIONS = [
  { id: 1, name: "Speaker" },
  { id: 2, name: "Amplifier" },
  { id: 3, name: "Vintage" },
] as const;

function categoryNameToId(name: string) {
  return CATEGORY_OPTIONS.find((x) => x.name === name)?.id ?? 0;
}
function categoryIdToName(id: number) {
  return CATEGORY_OPTIONS.find((x) => x.id === id)?.name ?? "Unknown";
}

function uiPriorityToNumber(p: Row["priority"]) {
  if (p === "High") return 1;
  if (p === "Medium") return 2;
  return 0;
}
function numberToUiPriority(n: number): Row["priority"] {
  if (n === 1) return "High";
  if (n === 2) return "Medium";
  return "Low";
}

/* -------------------- API types -------------------- */
type KeywordDto = {
  id: number;
  text: string;
  categoryId: number | null;
  priority: number;
  active: boolean;
  createdAt?: string | null;
};

type KeywordsResponse = {
  total: number;
  page: number;
  pageSize: number;
  items: KeywordDto[];
};

function fmtNextRunFromCreatedAt(iso?: string | null) {
  if (!iso) return "—";
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return "—";
  return d.toLocaleDateString("en-GB", { day: "2-digit", month: "short", year: "numeric" });
}

function priorityToUi(n: number): Row["priority"] {
  if (n >= 2) return "High";
  if (n === 1) return "Medium";
  return "Low";
}

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

/* -------------------- UI primitives -------------------- */
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

/* -------------------- Confirm Delete Modal -------------------- */
function ConfirmDeleteModal({
  open,
  title = "Delete keyword",
  description = "Are you sure you want to delete this keyword? This action cannot be undone.",
  confirmText = "Confirm",
  cancelText = "Cancel",
  loading,
  onCancel,
  onConfirm,
}: {
  open: boolean;
  title?: string;
  description?: string;
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
    <div className="fixed inset-0 z-[120]">
      <div className="absolute inset-0 bg-black/30" onClick={onCancel} />
      <div className="absolute inset-0 flex items-center justify-center p-4">
        <div
          className="w-[460px] max-w-full rounded-2xl bg-white border border-gray-100 shadow-[0_25px_80px_-45px_rgba(0,0,0,0.45)] p-6"
          onClick={(e) => e.stopPropagation()}
          role="dialog"
          aria-modal="true"
        >
          <div className="text-sm font-bold text-gray-900">{title}</div>
          <div className="mt-2 text-sm text-gray-600">{description}</div>

          <div className="mt-6 flex justify-end gap-2">
            <button
              type="button"
              onClick={onCancel}
              disabled={loading}
              className={cn(
                "h-9 rounded-lg border px-4 text-xs font-semibold",
                loading
                  ? "border-gray-200 text-gray-400"
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
                loading ? "bg-red-400" : "bg-red-600 hover:bg-red-700"
              )}
            >
              {loading ? "Deleting..." : confirmText}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

/* -------------------- Date picker (mock UI) -------------------- */
function daysInMonth(year: number, month0: number) {
  return new Date(year, month0 + 1, 0).getDate();
}
function firstWeekday(year: number, month0: number) {
  return new Date(year, month0, 1).getDay();
}
function fmtMonthYear(year: number, month0: number) {
  return new Date(year, month0, 1).toLocaleString("en-US", { month: "long", year: "numeric" });
}
function pad2(n: number) {
  return String(n).padStart(2, "0");
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

/* -------------------- main -------------------- */
export default function KeywordTable() {
  const createKw = useCreateKeywordMutation();

  const [rows, setRows] = useState<Row[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);

  // filter state
  const [open, setOpen] = useState<OpenKey>(null);

  const [draftDates, setDraftDates] = useState<string[]>([]);
  const [draftCategories, setDraftCategories] = useState<string[]>([]);
  const [draftPriorities, setDraftPriorities] = useState<Array<Row["priority"]>>([]);
  const [draftStatus, setDraftStatus] = useState<Array<"Active" | "Inactive">>([]);

  const [dates, setDates] = useState<string[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [priorities, setPriorities] = useState<Array<Row["priority"]>>([]);
  const [status, setStatus] = useState<Array<"Active" | "Inactive">>([]);

  // add modal
  const [openAdd, setOpenAdd] = useState(false);

  // edit drawer
  const [editOpen, setEditOpen] = useState(false);
  const [editing, setEditing] = useState<KeywordRow | null>(null);

  // delete confirm
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  // anchors + popover layer
  const containerRef = useRef<HTMLDivElement | null>(null);
  const anchorDate = useRef<HTMLDivElement | null>(null);
  const anchorCategory = useRef<HTMLDivElement | null>(null);
  const anchorPriority = useRef<HTMLDivElement | null>(null);
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

  useOnClickOutside([popoverRef, anchorDate, anchorCategory, anchorPriority, anchorStatus], () => {
    setOpen(null);
  });

  useEffect(() => {
    function onRelayout() {
      if (open === "date") calcPos(anchorDate, 340);
      if (open === "category") calcPos(anchorCategory, 520);
      if (open === "priority") calcPos(anchorPriority, 520);
      if (open === "status") calcPos(anchorStatus, 520);
    }
    window.addEventListener("resize", onRelayout);
    window.addEventListener("scroll", onRelayout, true);
    return () => {
      window.removeEventListener("resize", onRelayout);
      window.removeEventListener("scroll", onRelayout, true);
    };
  }, [open]);

  async function loadKeywords() {
    setLoading(true);
    setLoadError(null);
    try {
      const res = await http.get<KeywordsResponse>("/api/keywords", {
        params: { page: 1, pageSize: 200 },
      });

      const items = res.data?.items ?? [];
      const mapped: Row[] = items.map((k) => ({
        id: String(k.id),
        text: k.text,
        category: categoryIdToName(k.categoryId ?? 0),
        priority: priorityToUi(k.priority ?? 1),
        nextRun: fmtNextRunFromCreatedAt(k.createdAt),
        active: !!k.active,
      }));

      setRows(mapped);
    } catch (e: any) {
      setLoadError(e?.response?.data?.error || e?.message || "Failed to load keywords");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadKeywords();
  }, []);

  function toggleActive(id: string, v: boolean) {
    setRows((prev) => prev.map((r) => (r.id === id ? { ...r, active: v } : r)));
  }

  const filteredRows = useMemo(() => {
    return rows.filter((r) => {
      const okCategory = categories.length ? categories.includes(r.category) : true;
      const okPriority = priorities.length ? priorities.includes(r.priority) : true;
      const okStatus = status.length ? status.includes(r.active ? "Active" : "Inactive") : true;
      const okDate = dates.length ? true : true; // placeholder
      return okCategory && okPriority && okStatus && okDate;
    });
  }, [rows, categories, priorities, status, dates]);

  const showingText = useMemo(() => {
    if (filteredRows.length === 0) return "Showing 0-0 of 0";
    const to = String(Math.min(filteredRows.length, 9)).padStart(2, "0");
    return `Showing 1-${to} of ${filteredRows.length}`;
  }, [filteredRows.length]);

  function resetFilters() {
    setDates([]);
    setCategories([]);
    setPriorities([]);
    setStatus([]);
    setDraftDates([]);
    setDraftCategories([]);
    setDraftPriorities([]);
    setDraftStatus([]);
    setOpen(null);
  }

  function openDate() {
    const next = open === "date" ? null : "date";
    setDraftDates(dates);
    setOpen(next);
    if (next) requestAnimationFrame(() => calcPos(anchorDate, 340));
  }
  function openCategory() {
    const next = open === "category" ? null : "category";
    setDraftCategories(categories);
    setOpen(next);
    if (next) requestAnimationFrame(() => calcPos(anchorCategory, 520));
  }
  function openPriority() {
    const next = open === "priority" ? null : "priority";
    setDraftPriorities(priorities);
    setOpen(next);
    if (next) requestAnimationFrame(() => calcPos(anchorPriority, 520));
  }
  function openStatus() {
    const next = open === "status" ? null : "status";
    setDraftStatus(status);
    setOpen(next);
    if (next) requestAnimationFrame(() => calcPos(anchorStatus, 520));
  }

  function requestDelete(id: string) {
    setDeletingId(id);
    setDeleteOpen(true);
  }

  async function confirmDelete() {
    if (!deletingId) return;
    try {
      setDeleteLoading(true);
      // TODO: call API if needed:
      // await http.delete(`/api/keywords/${Number(deletingId)}`);
      setRows((prev) => prev.filter((x) => x.id !== deletingId));
      setDeleteOpen(false);
      setDeletingId(null);
    } finally {
      setDeleteLoading(false);
    }
  }

  async function onAddKeyword(payload: AddKeywordPayload) {
    await createKw.mutateAsync(payload);
    setOpenAdd(false);
    await loadKeywords();
  }

  return (
    <div ref={containerRef} className="relative">
      <div className="rounded-2xl bg-white border border-gray-100 shadow-[0_20px_70px_-45px_rgba(0,0,0,0.25)]">
        {/* Filters */}
        <div className="px-6 pt-5">
          <div className="flex items-center justify-between gap-4">
            <div className="flex flex-wrap items-center gap-2">
              <button className="inline-flex items-center gap-2 rounded-lg border border-gray-100 bg-white px-3 py-2 text-xs font-semibold text-gray-700 hover:bg-gray-50">
                <Filter size={14} className="text-gray-500" />
                Filter By
              </button>

              <div ref={anchorDate}>
                <SelectPill label="Date" active={open === "date"} onClick={openDate} />
              </div>

              <div ref={anchorCategory}>
                <SelectPill
                  label="Category Type"
                  active={open === "category"}
                  onClick={openCategory}
                />
              </div>

              <div ref={anchorPriority}>
                <SelectPill label="Priority" active={open === "priority"} onClick={openPriority} />
              </div>

              <div ref={anchorStatus}>
                <SelectPill label="Status" active={open === "status"} onClick={openStatus} />
              </div>

              <button
                type="button"
                onClick={resetFilters}
                className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-xs font-semibold text-red-500 hover:bg-red-50"
              >
                Reset Filter
              </button>

              <button
                type="button"
                onClick={loadKeywords}
                className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-xs font-semibold text-gray-600 hover:bg-gray-50 border border-gray-100"
                disabled={loading}
              >
                {loading ? "Refreshing..." : "Refresh"}
              </button>
            </div>

            <button
              type="button"
              onClick={() => setOpenAdd(true)}
              className="rounded-lg bg-blue-600 px-4 py-2 text-xs font-semibold text-white hover:bg-blue-700"
            >
              Add Keyword
            </button>
          </div>

          {loadError ? <div className="mt-3 text-sm text-red-600">{loadError}</div> : null}
        </div>

        {/* Table */}
        <div className="px-6 pb-2 pt-4">
          <div className="overflow-hidden rounded-xl border border-gray-100">
            <div className="grid grid-cols-12 bg-white px-4 py-3 text-[11px] font-semibold text-gray-500 border-b border-gray-100">
              <div className="col-span-3">Text</div>
              <div className="col-span-2">Category</div>
              <div className="col-span-2">Priority</div>
              <div className="col-span-3">Next Run</div>
              <div className="col-span-1">Status</div>
              <div className="col-span-1 text-right">Action</div>
            </div>

            <div className="divide-y divide-gray-100">
              {loading ? (
                <div className="px-4 py-10 text-sm text-gray-500">Loading...</div>
              ) : (
                filteredRows.map((r) => (
                  <div key={r.id} className="grid grid-cols-12 items-center px-4 py-3 text-sm">
                    <div className="col-span-3 text-gray-900">{r.text}</div>
                    <div className="col-span-2 text-gray-600">{r.category}</div>
                    <div className="col-span-2 text-gray-600">{r.priority}</div>
                    <div className="col-span-3 text-gray-600">{r.nextRun}</div>

                    <div className="col-span-1 flex items-center gap-2">
                      <Toggle value={r.active} onChange={(v) => toggleActive(r.id, v)} />
                      <span className="text-xs text-gray-600">{r.active ? "Active" : "Inactive"}</span>
                    </div>

                    <div className="col-span-1 flex justify-end gap-2">
                      <button
                        type="button"
                        onClick={() => {
                          setEditing({
                            id: Number(r.id),
                            text: r.text,
                            categoryId: categoryNameToId(r.category),
                            priority: uiPriorityToNumber(r.priority),
                            active: r.active,

                            locationLat: 0,
                            locationLon: 0,
                            radiusKm: 0,
                            radiusPolicy: "platform",
                            sortBy: "relevance",
                            conditions: [],
                            listedTime: "all",
                            availability: "available",
                          });
                          setEditOpen(true);
                        }}
                        className="h-8 w-8 rounded-lg border border-gray-100 grid place-items-center hover:bg-gray-50"
                        title="Edit"
                      >
                        <Pencil size={14} className="text-gray-500" />
                      </button>

                      <button
                        type="button"
                        onClick={() => requestDelete(r.id)}
                        className="h-8 w-8 rounded-lg border border-gray-100 grid place-items-center hover:bg-red-50"
                        title="Delete"
                      >
                        <Trash2 size={14} className="text-red-500" />
                      </button>
                    </div>
                  </div>
                ))
              )}

              {!loading && filteredRows.length === 0 ? (
                <div className="px-4 py-10 text-sm text-gray-500">No data</div>
              ) : null}
            </div>
          </div>

          {/* Footer */}
          <div className="flex items-center justify-between py-3 text-xs text-gray-500">
            <div>{showingText}</div>
            <div className="flex items-center gap-2">
              <button className="h-8 w-8 rounded-lg border border-gray-100 bg-white grid place-items-center hover:bg-gray-50">
                <ChevronLeft size={16} className="text-gray-500" />
              </button>
              <button className="h-8 w-8 rounded-lg border border-gray-100 bg-white grid place-items-center hover:bg-gray-50">
                <ChevronRight size={16} className="text-gray-500" />
              </button>
            </div>
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
                    setOpen(null);
                  }}
                />
              </div>
            ) : null}

            {open === "category" ? (
              <div className="absolute z-50" style={{ left: popPos.left, top: popPos.top }}>
                <PopoverCard
                  title="Select Category Type"
                  note="*You can choose multiple Category type"
                  onApply={() => {
                    setCategories(draftCategories);
                    setOpen(null);
                  }}
                >
                  <div className="flex flex-wrap gap-3">
                    {CATEGORY_OPTIONS.map((x) => (
                      <Chip
                        key={x.id}
                        label={x.name}
                        selected={draftCategories.includes(x.name)}
                        onClick={() =>
                          setDraftCategories((prev) =>
                            prev.includes(x.name)
                              ? prev.filter((t) => t !== x.name)
                              : [...prev, x.name]
                          )
                        }
                      />
                    ))}
                  </div>
                </PopoverCard>
              </div>
            ) : null}

            {open === "priority" ? (
              <div className="absolute z-50" style={{ left: popPos.left, top: popPos.top }}>
                <PopoverCard
                  title="Select Priority"
                  note="*You can choose multiple Priority"
                  onApply={() => {
                    setPriorities(draftPriorities);
                    setOpen(null);
                  }}
                >
                  <div className="flex flex-wrap gap-3">
                    {(["High", "Medium", "Low"] as const).map((x) => (
                      <Chip
                        key={x}
                        label={x}
                        selected={draftPriorities.includes(x)}
                        onClick={() =>
                          setDraftPriorities((prev) =>
                            prev.includes(x) ? prev.filter((t) => t !== x) : [...prev, x]
                          )
                        }
                      />
                    ))}
                  </div>
                </PopoverCard>
              </div>
            ) : null}

            {open === "status" ? (
              <div className="absolute z-50" style={{ left: popPos.left, top: popPos.top }}>
                <PopoverCard
                  title="Select Status"
                  note="*You can choose multiple Status"
                  onApply={() => {
                    setStatus(draftStatus);
                    setOpen(null);
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

      {/* Add keyword modal */}
      <AddKeywordModal open={openAdd} onClose={() => setOpenAdd(false)} onSubmit={onAddKeyword} />

      {/* Edit drawer */}
      <KeywordEditDrawer
        open={editOpen}
        keyword={editing}
        token={null as any}
        apiBase=""
        onClose={() => {
          setEditOpen(false);
          setEditing(null);
        }}
        onUpdated={(updated) => {
          setRows((prev) =>
            prev.map((x) => {
              if (Number(x.id) !== updated.id) return x;
              return {
                ...x,
                text: updated.text,
                category: categoryIdToName(updated.categoryId),
                priority: numberToUiPriority(updated.priority),
                active: updated.active,
              };
            })
          );
        }}
      />

      {/* Delete confirm modal */}
      <ConfirmDeleteModal
        open={deleteOpen}
        loading={deleteLoading}
        onCancel={() => {
          if (deleteLoading) return;
          setDeleteOpen(false);
          setDeletingId(null);
        }}
        onConfirm={confirmDelete}
      />
    </div>
  );
}
