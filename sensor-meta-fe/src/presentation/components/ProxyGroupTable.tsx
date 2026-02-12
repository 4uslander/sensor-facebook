// src/presentation/components/ProxyGroupTable.tsx
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

import AddProxyModal from "./AddProxyModal";
import ProxyEditDrawer from "./ProxyEditDrawer";
import type { CreateProxyGroupRequest, ProxyGroupDto } from "../../infrastructure/api/proxyGroupsApi";
import { proxyGroupsApi } from "../../infrastructure/api/proxyGroupsApi";

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
  }, [handler, refs]);
}

function getAccessTokenFromLocalStorage(): string | null {
  // theo file bạn đưa trước đó: KEY = "sf_auth_tokens"
  const raw = localStorage.getItem("sf_auth_tokens");
  if (!raw) return null;
  try {
    const parsed = JSON.parse(raw) as { accessToken?: string };
    return parsed?.accessToken ?? null;
  } catch {
    return null;
  }
}

function toStatusLabel(s: string) {
  return String(s || "").toLowerCase() === "inactive" ? "inactive" : "active";
}

function toEndpoint(dto: ProxyGroupDto) {
  return dto.endpoint || `${dto.protocol}://${dto.host}:${dto.port}`;
}

/* -------------------- small UI -------------------- */
type OpenKey = null | "status";

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

/* -------------------- confirm delete modal -------------------- */
function ConfirmDeleteModal({
  open,
  title = "Delete Proxy Group",
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
              disabled={loading}
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

/* -------------------- main -------------------- */
export default function ProxyGroupTable() {
  const baseUrl = import.meta.env.VITE_API_URL || "https://localhost:7141";
  const token = getAccessTokenFromLocalStorage();

  // server data
  const [items, setItems] = useState<ProxyGroupDto[]>([]);
  const [total, setTotal] = useState(0);

  // query state
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);

  // Search + filters
  const [q, setQ] = useState("");
  const [open, setOpen] = useState<OpenKey>(null);

  const [draftStatus, setDraftStatus] = useState<Array<"active" | "inactive">>([]);
  const [status, setStatus] = useState<Array<"active" | "inactive">>([]);

  // add/edit/delete
  const [addOpen, setAddOpen] = useState(false);

  const [editOpen, setEditOpen] = useState(false);
  const [editing, setEditing] = useState<ProxyGroupDto | null>(null);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleting, setDeleting] = useState<ProxyGroupDto | null>(null);
  const [deleteLoading, setDeleteLoading] = useState(false);

  // loading/error
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  // Anchors + popover positioning
  const containerRef = useRef<HTMLDivElement | null>(null);
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

  useOnClickOutside([popoverRef, anchorStatus], () => setOpen(null));

  useEffect(() => {
    function onRelayout() {
      if (open === "status") calcPos(anchorStatus, 520);
    }
    window.addEventListener("resize", onRelayout);
    window.addEventListener("scroll", onRelayout, true);
    return () => {
      window.removeEventListener("resize", onRelayout);
      window.removeEventListener("scroll", onRelayout, true);
    };
  }, [open]);

  async function fetchList() {
    setLoading(true);
    setErr(null);
    try {
      const api = proxyGroupsApi(baseUrl, token || undefined);
      // status filter: BE nhận string? 1 cái. nếu chọn 2 cái => bỏ filter (hoặc bạn đổi BE hỗ trợ multi)
      const statusParam =
        status.length === 1 ? status[0] : status.length === 0 ? null : null;

      const res = await api.list({
        page,
        pageSize,
        q: q.trim() ? q.trim() : null,
        status: statusParam,
        region: null,
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
  }, [page, pageSize, status.join(","), q]);

  function resetFilters() {
    setQ("");
    setStatus([]);
    setDraftStatus([]);
    setOpen(null);
    setPage(1);
  }

  function openStatus() {
    const next = open === "status" ? null : "status";
    setDraftStatus(status);
    setOpen(next);
    if (next) requestAnimationFrame(() => calcPos(anchorStatus, 520));
  }

  function beginEdit(r: ProxyGroupDto) {
    setEditing(r);
    setEditOpen(true);
  }

  function requestDelete(r: ProxyGroupDto) {
    setDeleting(r);
    setDeleteOpen(true);
  }

  async function confirmDelete() {
    if (!deleting) return;
    try {
      setDeleteLoading(true);
      const api = proxyGroupsApi(baseUrl, token || undefined);
      await api.del(deleting.id);
      setDeleteOpen(false);
      setDeleting(null);
      // refresh current page
      await fetchList();
    } catch (e: any) {
      // giữ modal mở để user thấy lỗi
      setErr(e?.message || "Delete failed");
    } finally {
      setDeleteLoading(false);
    }
  }

  async function toggleStatus(row: ProxyGroupDto) {
    // toggle active/inactive -> gọi PUT
    const next = toStatusLabel(row.status) === "active" ? "inactive" : "active";
    try {
      const api = proxyGroupsApi(baseUrl, token || undefined);
      await api.update(row.id, { status: next });
      await fetchList();
    } catch (e: any) {
      setErr(e?.message || "Update status failed");
    }
  }

  async function createProxy(body: CreateProxyGroupRequest) {
    const api = proxyGroupsApi(baseUrl, token || undefined);
    await api.create(body);
    setPage(1);
    await fetchList();
  }

  const showingText = useMemo(() => {
    const start = total === 0 ? 0 : (page - 1) * pageSize + 1;
    const end = Math.min(page * pageSize, total);
    return `Showing ${start}-${end} of ${total}`;
  }, [page, pageSize, total]);

  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  return (
    <>
      <div
        ref={containerRef}
        className="relative rounded-2xl bg-white border border-gray-100 shadow-[0_18px_55px_-38px_rgba(0,0,0,0.25)]"
      >
        <div className="px-6 pt-5">
          <div className="flex items-center justify-between gap-4">
            <div className="flex flex-wrap items-center gap-3">
              <div className="relative">
                <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                <input
                  value={q}
                  onChange={(e) => {
                    setQ(e.target.value);
                    setPage(1);
                  }}
                  placeholder="Search (name/region/host)"
                  className="h-9 w-64 rounded-lg border border-gray-100 bg-white pl-9 pr-3 text-xs outline-none focus:ring-2 focus:ring-blue-100"
                />
              </div>

              <div className="inline-flex items-center gap-2 text-xs font-semibold text-gray-600">
                <Filter size={14} className="text-gray-500" />
                Filter By
              </div>

              <div ref={anchorStatus}>
                <SelectPill label="Status" active={open === "status"} onClick={openStatus} />
              </div>

              <button
                type="button"
                onClick={resetFilters}
                className="inline-flex items-center gap-2 rounded-lg px-2 py-2 text-xs font-semibold text-red-500 hover:bg-red-50"
              >
                Reset Filter
              </button>
            </div>

            <button
              type="button"
              onClick={() => setAddOpen(true)}
              className="rounded-lg bg-blue-600 px-4 py-2 text-xs font-semibold text-white hover:bg-blue-700"
            >
              Add Proxy
            </button>
          </div>

          {err ? (
            <div className="mt-4 rounded-xl border border-red-100 bg-red-50 px-4 py-3 text-sm text-red-700">
              {err}
            </div>
          ) : null}
        </div>

        <div className="px-6 pb-2 pt-4">
          <div className="overflow-hidden rounded-xl border border-gray-100">
            <div
              className={cn(
                "grid items-center bg-gray-50 px-4 py-3 text-[11px] font-semibold text-gray-500",
                "grid-cols-[90px_1.3fr_2fr_1fr_1.2fr_120px]"
              )}
            >
              <div>ID</div>
              <div>Name</div>
              <div>Endpoint</div>
              <div>Region</div>
              <div>Status</div>
              <div className="text-center">Action</div>
            </div>

            <div className="divide-y divide-gray-100 bg-white">
              {loading ? (
                <div className="px-4 py-10 text-sm text-gray-500">Loading...</div>
              ) : null}

              {!loading &&
                items.map((r) => {
                  const lockActions = editOpen || deleteLoading;

                  return (
                    <div
                      key={r.id}
                      className={cn(
                        "grid items-center px-4 py-3 text-sm",
                        "grid-cols-[90px_1.3fr_2fr_1fr_1.2fr_120px]",
                        "hover:bg-gray-50/60"
                      )}
                    >
                      <div className="text-gray-700">{r.id}</div>

                      <div className="text-gray-900 font-medium truncate">{r.name}</div>

                      <div className="text-gray-600 truncate">{toEndpoint(r)}</div>

                      <div className="text-gray-600 truncate">{r.region ?? "-"}</div>

                      <div className="flex items-center gap-2">
                        <button
                          type="button"
                          onClick={() => toggleStatus(r)}
                          disabled={lockActions}
                          className={cn(
                            "h-6 w-11 rounded-full relative transition",
                            toStatusLabel(r.status) === "active" ? "bg-blue-600" : "bg-gray-200",
                            lockActions && "opacity-60 cursor-not-allowed"
                          )}
                          aria-label="toggle status"
                        >
                          <span
                            className={cn(
                              "absolute top-0.5 h-5 w-5 rounded-full bg-white shadow-sm transition",
                              toStatusLabel(r.status) === "active" ? "left-5" : "left-1"
                            )}
                          />
                        </button>
                        <span className="text-xs text-gray-700">{toStatusLabel(r.status)}</span>
                      </div>

                      <div className="flex items-center justify-center gap-2">
                        <button
                          type="button"
                          disabled={lockActions}
                          onClick={() => beginEdit(r)}
                          className={cn(
                            "h-8 w-8 rounded-lg border border-gray-100 grid place-items-center bg-white",
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
                            "h-8 w-8 rounded-lg border border-gray-100 grid place-items-center bg-white",
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

              {!loading && items.length === 0 ? (
                <div className="px-4 py-10 text-sm text-gray-500">No data</div>
              ) : null}
            </div>
          </div>

          <div className="flex items-center justify-between py-3 text-xs text-gray-500">
            <div>{showingText}</div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="h-8 w-8 rounded-lg border border-gray-100 bg-white grid place-items-center hover:bg-gray-50 disabled:opacity-50"
              >
                <ChevronLeft size={16} className="text-gray-500" />
              </button>
              <button
                type="button"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                className="h-8 w-8 rounded-lg border border-gray-100 bg-white grid place-items-center hover:bg-gray-50 disabled:opacity-50"
              >
                <ChevronRight size={16} className="text-gray-500" />
              </button>
            </div>
          </div>
        </div>

        {open ? (
          <div className="absolute inset-0 pointer-events-none">
            <div ref={popoverRef} className="pointer-events-auto">
              {open === "status" ? (
                <div className="absolute z-50" style={{ left: popPos.left, top: popPos.top }}>
                  <PopoverCard
                    title="Select Status"
                    note="*BE currently supports single status filter. Selecting both = no filter."
                    onApply={() => {
                      setStatus(draftStatus);
                      setPage(1);
                      setOpen(null);
                    }}
                  >
                    <div className="flex flex-wrap gap-3">
                      {(["active", "inactive"] as const).map((x) => (
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

      {/* Add modal */}
      <AddProxyModal
        open={addOpen}
        onClose={() => setAddOpen(false)}
        onSubmit={createProxy}
      />

      {/* Confirm delete */}
      <ConfirmDeleteModal
        open={deleteOpen}
        loading={deleteLoading}
        message={
          deleting
            ? `Are you sure you want to delete "${deleting.name}" (ID ${deleting.id})? This action cannot be undone.`
            : "Are you sure you want to delete this proxy group?"
        }
        onCancel={() => {
          if (deleteLoading) return;
          setDeleteOpen(false);
          setDeleting(null);
        }}
        onConfirm={confirmDelete}
      />

      {/* Edit drawer */}
      <ProxyEditDrawer
        open={editOpen}
        proxy={editing}
        token={token}
        apiBase={baseUrl}
        onClose={() => {
          setEditOpen(false);
          setEditing(null);
        }}
        onUpdated={async () => {
          await fetchList();
        }}
      />
    </>
  );
}
