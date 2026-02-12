// src/presentation/components/CategoryTable.tsx
import {
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  Filter,
  Pencil,
  Search,
  Trash2,
  X,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import type { MutableRefObject, RefObject } from "react";

import AddCategoryModal from "./AddCategoryModal";
import { useCategoriesQuery } from "../hooks/useCategoriesQuery";
import {
  useCreateCategoryMutation,
  useDeleteCategoryMutation,
  useRestoreCategoryMutation,
  useUpdateCategoryMutation,
} from "../hooks/useCategoryMutations";

import type { CategoryListItemDto } from "../../infrastructure/api/categories.api";

/* -------------------- types -------------------- */
type Row = {
  id: number;
  name: string;
  description: string;
  createdAt: string; // ISO -> show raw (giữ UI hiện tại)
  active: boolean;
};

type OpenKey = null | "date" | "status";

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
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [handler]);
}

/* -------------------- small UI -------------------- */
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

function Toggle({
  value,
  disabled,
  onChange,
}: {
  value: boolean;
  disabled?: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <button
      type="button"
      disabled={disabled}
      onClick={() => onChange(!value)}
      className={cn(
        "relative inline-flex h-5 w-9 items-center rounded-full transition",
        value ? "bg-blue-600" : "bg-gray-200",
        disabled ? "opacity-50 cursor-not-allowed" : ""
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

/* -------------------- confirm delete modal -------------------- */
function ConfirmDeleteModal({
  open,
  title = "Delete Category",
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
    <div className="fixed inset-0 z-[100]">
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
              disabled={loading}
              className={cn(
                "h-8 w-8 rounded-lg border border-gray-100 grid place-items-center",
                loading ? "opacity-50 cursor-not-allowed" : "hover:bg-gray-50"
              )}
              aria-label="Close modal"
            >
              <X size={16} className="text-gray-600" />
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

/* -------------------- Date picker (demo UI, not wired) -------------------- */
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
export default function CategoryTable() {
  // Filters state
  const [q, setQ] = useState("");
  const [open, setOpen] = useState<OpenKey>(null);

  const [draftDates, setDraftDates] = useState<string[]>([]);
  const [draftStatus, setDraftStatus] = useState<Array<"Active" | "Inactive">>([]);

  const [dates, setDates] = useState<string[]>([]);
  const [status, setStatus] = useState<Array<"Active" | "Inactive">>([]);

  // Pagination
  const [page, setPage] = useState(1);
  const pageSize = 20;

  // Add modal
  const [addOpen, setAddOpen] = useState(false);

  // Inline edit state
  const [editingId, setEditingId] = useState<number | null>(null);
  const [draftName, setDraftName] = useState("");
  const [draftDesc, setDraftDesc] = useState("");

  // Delete confirm modal
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleting, setDeleting] = useState<Row | null>(null);

  // Map status -> active param cho API
  const activeParam = useMemo(() => {
    if (status.length === 1) return status[0] === "Active";
    return undefined;
  }, [status]);

  const { data, isLoading, isError, error, isFetching } = useCategoriesQuery({
    q: q.trim() ? q.trim() : undefined,
    active: activeParam,
    page,
    pageSize,
  });

  const mCreate = useCreateCategoryMutation();
  const mUpdate = useUpdateCategoryMutation();
  const mDelete = useDeleteCategoryMutation();
  const mRestore = useRestoreCategoryMutation();

  const rows: Row[] = useMemo(() => {
    const items: CategoryListItemDto[] = data?.items ?? [];
    return items.map((x) => ({
      id: x.id,
      name: x.name,
      description: x.description ?? "",
      createdAt: x.createdAt,
      active: x.active,
    }));
  }, [data]);

  const total = data?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const fromIdx = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const toIdx = Math.min(page * pageSize, total);

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

  function beginInlineEdit(r: Row) {
    setEditingId(r.id);
    setDraftName(r.name);
    setDraftDesc(r.description);
  }

  function cancelInlineEdit() {
    setEditingId(null);
    setDraftName("");
    setDraftDesc("");
  }

  async function saveInlineEdit(id: number) {
    const name = draftName.trim();
    const description = draftDesc.trim();
    if (!name) return;

    await mUpdate.mutateAsync({
      id,
      payload: { name, description: description.length ? description : null },
    });

    cancelInlineEdit();
  }

  async function toggleActive(id: number, v: boolean) {
    if (editingId !== null) return;
    await mUpdate.mutateAsync({ id, payload: { active: v } });
  }

  function requestDelete(r: Row) {
    setDeleting(r);
    setDeleteOpen(true);
  }

  async function confirmDelete() {
    if (!deleting) return;
    await mDelete.mutateAsync(deleting.id);
    setDeleteOpen(false);
    setDeleting(null);
  }

  const showingText = useMemo(() => {
    if (total === 0) return "Showing 0-0 of 0";
    return `Showing ${fromIdx}-${toIdx} of ${total.toLocaleString()}`;
  }, [fromIdx, toIdx, total]);

  const busy = mCreate.isPending || mUpdate.isPending || mDelete.isPending || mRestore.isPending;

  return (
    <>
      <div
        ref={containerRef}
        className="relative rounded-2xl bg-white border border-gray-100 shadow-[0_20px_70px_-45px_rgba(0,0,0,0.25)]"
      >
        {/* Filters bar */}
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

              {/* Search */}
              <div className="relative">
                <Search
                  size={14}
                  className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400"
                />
                <input
                  value={q}
                  onChange={(e) => {
                    setQ(e.target.value);
                    setPage(1);
                  }}
                  placeholder="Search"
                  className="h-9 w-44 rounded-lg border border-gray-100 bg-white pl-9 pr-3 text-xs outline-none focus:ring-2 focus:ring-blue-100"
                />
              </div>

              {/* Date (demo UI) */}
              <div ref={anchorDate}>
                <SelectPill label="Date" active={open === "date"} onClick={openDate} />
              </div>

              {/* Status */}
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
            </div>

            <button
              type="button"
              onClick={() => setAddOpen(true)}
              disabled={busy}
              className={cn(
                "rounded-lg px-4 py-2 text-xs font-semibold text-white",
                busy ? "bg-blue-300 cursor-not-allowed" : "bg-blue-600 hover:bg-blue-700"
              )}
            >
              Add Category
            </button>
          </div>
        </div>

        {/* Table */}
        <div className="px-6 pb-2 pt-4">
          <div className="overflow-hidden rounded-xl border border-gray-100">
            <div className="grid grid-cols-12 bg-white px-4 py-3 text-[11px] font-semibold text-gray-500 border-b border-gray-100">
              <div className="col-span-1">ID</div>
              <div className="col-span-2">Name</div>
              <div className="col-span-5">Description</div>
              <div className="col-span-2">Create At</div>
              <div className="col-span-1">Status</div>
              <div className="col-span-1 text-right">Action</div>
              {isFetching ? (
                <div className="col-span-12 mt-2 text-[11px] text-gray-400">Loading...</div>
              ) : null}
            </div>

            {isLoading ? (
              <div className="px-4 py-10 text-sm text-gray-500">Loading...</div>
            ) : isError ? (
              <div className="px-4 py-10 text-sm text-red-600">
                {(error as any)?.response?.data?.error ||
                  (error as any)?.message ||
                  "Failed to load categories"}
              </div>
            ) : rows.length === 0 ? (
              <div className="px-4 py-10 text-sm text-gray-500">No data</div>
            ) : (
              <div className="divide-y divide-gray-100">
                {rows.map((r) => {
                  const isEditing = editingId === r.id;
                  const lockActions = (editingId !== null && !isEditing) || busy;

                  return (
                    <div key={r.id} className="grid grid-cols-12 items-center px-4 py-3 text-sm">
                      <div className="col-span-1 text-gray-700">{r.id}</div>

                      <div className="col-span-2">
                        {isEditing ? (
                          <input
                            value={draftName}
                            onChange={(e) => setDraftName(e.target.value)}
                            className="h-9 w-full rounded-lg border border-gray-200 bg-white px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                          />
                        ) : (
                          <div className="text-gray-900">{r.name}</div>
                        )}
                      </div>

                      <div className="col-span-5">
                        {isEditing ? (
                          <input
                            value={draftDesc}
                            onChange={(e) => setDraftDesc(e.target.value)}
                            className="h-9 w-full rounded-lg border border-gray-200 bg-white px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                          />
                        ) : (
                          <div className="text-gray-600">{r.description}</div>
                        )}
                      </div>

                      <div className="col-span-2 text-gray-600">{r.createdAt}</div>

                      <div className="col-span-1 flex items-center gap-2">
                        <Toggle
                          value={r.active}
                          disabled={lockActions}
                          onChange={(v) => toggleActive(r.id, v)}
                        />
                        <span className="text-xs text-gray-600">
                          {r.active ? "Active" : "Inactive"}
                        </span>
                      </div>

                      <div className="col-span-1 flex justify-end gap-2">
                        {isEditing ? (
                          <>
                            <button
                              type="button"
                              onClick={() => saveInlineEdit(r.id)}
                              disabled={busy}
                              className={cn(
                                "h-9 rounded-lg px-3 text-xs font-semibold text-white",
                                busy
                                  ? "bg-blue-300 cursor-not-allowed"
                                  : "bg-blue-600 hover:bg-blue-700"
                              )}
                            >
                              {mUpdate.isPending ? "Saving..." : "Save"}
                            </button>
                            <button
                              type="button"
                              onClick={cancelInlineEdit}
                              disabled={busy}
                              className={cn(
                                "h-9 rounded-lg border border-gray-200 px-3 text-xs font-semibold",
                                busy
                                  ? "text-gray-400 cursor-not-allowed"
                                  : "text-gray-700 hover:bg-gray-50"
                              )}
                            >
                              Cancel
                            </button>
                          </>
                        ) : (
                          <>
                            <button
                              type="button"
                              disabled={lockActions}
                              onClick={() => beginInlineEdit(r)}
                              className={cn(
                                "h-8 w-8 rounded-lg border border-gray-100 grid place-items-center",
                                lockActions ? "opacity-50 cursor-not-allowed" : "hover:bg-gray-50"
                              )}
                              title="Edit"
                            >
                              <Pencil size={14} className="text-gray-500" />
                            </button>

                            {r.active ? (
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
                            ) : (
                              <button
                                type="button"
                                disabled={lockActions}
                                onClick={() => mRestore.mutateAsync(r.id)}
                                className={cn(
                                  "h-8 rounded-lg border border-gray-100 px-3 text-xs font-semibold",
                                  lockActions
                                    ? "text-gray-400 cursor-not-allowed"
                                    : "text-blue-600 hover:bg-blue-50"
                                )}
                                title="Restore"
                              >
                                {mRestore.isPending ? "Restoring..." : "Restore"}
                              </button>
                            )}
                          </>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Footer (pagination real) */}
          <div className="flex items-center justify-between py-3 text-xs text-gray-500">
            <div>{showingText}</div>
            <div className="flex items-center gap-2">
              <button
                className="h-8 w-8 rounded-lg border border-gray-100 bg-white grid place-items-center hover:bg-gray-50 disabled:opacity-50"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                <ChevronLeft size={16} className="text-gray-500" />
              </button>
              <button
                className="h-8 w-8 rounded-lg border border-gray-100 bg-white grid place-items-center hover:bg-gray-50 disabled:opacity-50"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
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
                      setOpen(null);
                      setPage(1);
                    }}
                  />
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

      {/* Add Category Modal */}
      <AddCategoryModal
        open={addOpen}
        onClose={() => {
          if (mCreate.isPending) return;
          setAddOpen(false);
        }}
        onSubmit={async (payload) => {
          await mCreate.mutateAsync({
            name: payload.name,
            description: payload.description,
          });
          setAddOpen(false);
          setPage(1);
        }}
      />

      {/* Confirm delete modal */}
      <ConfirmDeleteModal
        open={deleteOpen}
        loading={mDelete.isPending}
        message={
          deleting
            ? `Are you sure you want to delete "${deleting.name}"? This action cannot be undone.`
            : "Are you sure you want to delete this category?"
        }
        onCancel={() => {
          if (mDelete.isPending) return;
          setDeleteOpen(false);
          setDeleting(null);
        }}
        onConfirm={confirmDelete}
      />
    </>
  );
}
