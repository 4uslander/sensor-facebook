// src/presentation/components/KeywordEditDrawer.tsx
import { useEffect, useMemo, useState } from "react";
import { X, Plus, Trash2 } from "lucide-react";
import { authStorage } from "../../shared/lib/authStorage";

export type KeywordUpdatePayload = {
  text: string;
  categoryId: number;
  priority: number; // 0/1/2...
  active: boolean;

  locationLat: number;
  locationLon: number;
  radiusKm: number;
  radiusPolicy: string;

  sortBy: string;
  conditions: string[];

  listedTime: string;
  availability: string;
};

export type KeywordRow = {
  id: number; // dùng để PUT /api/keywords/{id}
  text: string;
  categoryId: number;
  priority: number;
  active: boolean;

  // optional fields (table list có thể thiếu)
  locationLat?: number | null;
  locationLon?: number | null;
  radiusKm?: number | null;
  radiusPolicy?: string | null;
  sortBy?: string | null;
  conditions?: string[] | null;
  listedTime?: string | null;
  availability?: string | null;
};

type KeywordDetailDto = {
  id: number;
  text: string;
  categoryId: number | null;
  priority: number;
  active: boolean;

  locationLat: number | null;
  locationLon: number | null;

  radiusKm: number;
  radiusPolicy: string;

  sortBy: string;
  conditions: string[] | null;

  listedTime: string;
  availability: string;
  createdAt: string;
};

function cn(...s: Array<string | false | null | undefined>) {
  return s.filter(Boolean).join(" ");
}

function priorityLabel(p: number) {
  if (p === 2) return "High";
  if (p === 1) return "Medium";
  return "Low";
}

function mapDetailToForm(d: KeywordDetailDto): KeywordUpdatePayload {
  return {
    text: d.text ?? "",
    categoryId: d.categoryId ?? 0,
    priority: Number.isFinite(d.priority) ? d.priority : 0,
    active: !!d.active,

    // nếu null -> 0 (giữ đúng kiểu payload hiện tại)
    locationLat: d.locationLat ?? 0,
    locationLon: d.locationLon ?? 0,
    radiusKm: Number.isFinite(d.radiusKm) ? d.radiusKm : 0,
    radiusPolicy: d.radiusPolicy ?? "",

    sortBy: d.sortBy ?? "",
    conditions: d.conditions ?? [],

    listedTime: d.listedTime ?? "",
    availability: d.availability ?? "",
  };
}

export default function KeywordEditDrawer({
  open,
  keyword,
  token,
  apiBase,
  onClose,
  onUpdated,
}: {
  open: boolean;
  keyword: KeywordRow | null;
  token?: string | null; // accessToken
  apiBase?: string; // https://localhost:7141
  onClose: () => void;
  onUpdated: (updated: KeywordRow) => void; // cập nhật list UI
}) {
  const baseUrl = apiBase || import.meta.env.VITE_API_URL || "https://localhost:7141";
  const accessToken = token ?? authStorage.getAccessToken();

  // initial chỉ dùng để “dựng form tạm” trước khi load detail (nếu table thiếu field)
  const initial: KeywordUpdatePayload = useMemo(
    () => ({
      text: keyword?.text ?? "",
      categoryId: keyword?.categoryId ?? 0,
      priority: keyword?.priority ?? 0,
      active: keyword?.active ?? true,

      locationLat: (keyword?.locationLat as number | null | undefined) ?? 0,
      locationLon: (keyword?.locationLon as number | null | undefined) ?? 0,
      radiusKm: (keyword?.radiusKm as number | null | undefined) ?? 0,
      radiusPolicy: (keyword?.radiusPolicy as string | null | undefined) ?? "",

      sortBy: (keyword?.sortBy as string | null | undefined) ?? "",
      conditions: (keyword?.conditions as string[] | null | undefined) ?? [],

      listedTime: (keyword?.listedTime as string | null | undefined) ?? "",
      availability: (keyword?.availability as string | null | undefined) ?? "",
    }),
    [keyword]
  );

  const [form, setForm] = useState<KeywordUpdatePayload>(initial);
  const [saving, setSaving] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  // reset form when open/keyword changes
  useEffect(() => {
    if (!open) return;
    setForm(initial);
    setErr(null);
  }, [open, initial]);

  // ✅ LOAD FULL DETAIL WHEN OPEN (fix hiển thị sai do thiếu field từ table)
  useEffect(() => {
    if (!open) return;
    if (!keyword?.id) return;

    let alive = true;

    (async () => {
      try {
        setLoadingDetail(true);
        setErr(null);

        const res = await fetch(`${baseUrl}/api/keywords/${keyword.id}`, {
          method: "GET",
          headers: {
            accept: "*/*",
            ...(accessToken  ? { Authorization: `Bearer ${accessToken }` } : {}),
          },
        });

        if (!res.ok) {
          const text = await res.text().catch(() => "");
          throw new Error(`GET detail failed (${res.status}). ${text}`);
        }

        const data = (await res.json()) as KeywordDetailDto;
        if (!alive) return;

        setForm(mapDetailToForm(data));
      } catch (e: any) {
        if (!alive) return;
        setErr(e?.message || "Load detail failed");
      } finally {
        if (alive) setLoadingDetail(false);
      }
    })();

    return () => {
      alive = false;
    };
  }, [open, keyword?.id, baseUrl, accessToken ]);

  // ESC to close
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  async function submit() {
    if (!keyword) return;

    // basic validation
    if (!form.text.trim()) {
      setErr("Keyword Name (text) is required.");
      return;
    }
    if (!Number.isFinite(form.categoryId) || form.categoryId <= 0) {
      setErr("categoryId must be > 0.");
      return;
    }

    setSaving(true);
    setErr(null);

    try {
      const res = await fetch(`${baseUrl}/api/keywords/${keyword.id}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
          accept: "*/*",
          ...(accessToken  ? { Authorization: `Bearer ${accessToken }` } : {}),
        },
        body: JSON.stringify(form),
      });

      if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(`PUT failed (${res.status}). ${text}`);
      }

      const updated: KeywordRow = { ...keyword, ...form };
      onUpdated(updated);
      onClose();
    } catch (e: any) {
      setErr(e?.message || "Update failed");
    } finally {
      setSaving(false);
    }
  }

  function set<K extends keyof KeywordUpdatePayload>(key: K, value: KeywordUpdatePayload[K]) {
    setForm((p) => ({ ...p, [key]: value }));
  }

  const [condDraft, setCondDraft] = useState("");
  useEffect(() => {
    if (!open) return;
    setCondDraft("");
  }, [open]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-[100]">
      {/* overlay */}
      <div className="absolute inset-0 bg-black/20" onClick={onClose} />

      {/* drawer */}
      <div
        className={cn(
          "absolute right-0 top-0 h-full w-[460px] bg-white border-l border-gray-100",
          "shadow-[0_30px_120px_-60px_rgba(0,0,0,0.6)]",
          "flex flex-col"
        )}
      >
        {/* header */}
        <div className="px-6 py-5 border-b border-gray-100 flex items-center justify-between">
          <div>
            <div className="text-sm font-semibold text-gray-900">Edit Keyword</div>
            <div className="text-xs text-gray-500">
              ID: {keyword?.id} • Current: <span className="font-medium">{keyword?.text}</span>
            </div>
            {loadingDetail && (
              <div className="mt-1 text-xs text-gray-400">Loading detail...</div>
            )}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="h-9 w-9 rounded-lg border border-gray-100 grid place-items-center hover:bg-gray-50"
          >
            <X size={16} className="text-gray-600" />
          </button>
        </div>

        {/* body */}
        <div className="flex-1 overflow-auto px-6 py-5">
          {err && (
            <div className="mb-4 rounded-xl border border-red-100 bg-red-50 px-4 py-3 text-sm text-red-700">
              {err}
            </div>
          )}

          <div className="space-y-3">
            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Keyword Name</label>
              <input
                className="col-span-8 h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                placeholder="Enter keyword text"
                value={form.text}
                onChange={(e) => set("text", e.target.value)}
              />
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Category</label>
              <input
                type="number"
                className="col-span-8 h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                placeholder="categoryId"
                value={form.categoryId}
                onChange={(e) => set("categoryId", Number(e.target.value))}
              />
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Priority</label>
              <select
                className="col-span-8 h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100 bg-white"
                value={form.priority}
                onChange={(e) => set("priority", Number(e.target.value))}
              >
                <option value={2}>High</option>
                <option value={1}>Medium</option>
                <option value={0}>Low</option>
              </select>
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Status</label>
              <div className="col-span-8 flex items-center gap-3">
                <button
                  type="button"
                  onClick={() => set("active", !form.active)}
                  className={cn(
                    "relative inline-flex h-6 w-11 items-center rounded-full transition",
                    form.active ? "bg-blue-600" : "bg-gray-200"
                  )}
                >
                  <span
                    className={cn(
                      "inline-block h-5 w-5 rounded-full bg-white shadow-sm transition",
                      form.active ? "translate-x-5" : "translate-x-1"
                    )}
                  />
                </button>
                <span className="text-sm text-gray-700">{form.active ? "Active" : "Inactive"}</span>
              </div>
            </div>

            <div className="pt-3">
              <div className="text-xs font-semibold text-gray-500">Advanced (optional)</div>
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Location</label>
              <div className="col-span-8 grid grid-cols-2 gap-2">
                <input
                  type="number"
                  className="h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                  placeholder="Lat"
                  value={form.locationLat}
                  onChange={(e) => set("locationLat", Number(e.target.value))}
                />
                <input
                  type="number"
                  className="h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                  placeholder="Lon"
                  value={form.locationLon}
                  onChange={(e) => set("locationLon", Number(e.target.value))}
                />
              </div>
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Radius</label>
              <div className="col-span-8 grid grid-cols-2 gap-2">
                <input
                  type="number"
                  className="h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                  placeholder="radiusKm"
                  value={form.radiusKm}
                  onChange={(e) => set("radiusKm", Number(e.target.value))}
                />
                <input
                  className="h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                  placeholder="radiusPolicy"
                  value={form.radiusPolicy}
                  onChange={(e) => set("radiusPolicy", e.target.value)}
                />
              </div>
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Sort By</label>
              <input
                className="col-span-8 h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                placeholder="sortBy"
                value={form.sortBy}
                onChange={(e) => set("sortBy", e.target.value)}
              />
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Listed Time</label>
              <input
                className="col-span-8 h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                placeholder="listedTime"
                value={form.listedTime}
                onChange={(e) => set("listedTime", e.target.value)}
              />
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Availability</label>
              <input
                className="col-span-8 h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                placeholder="availability"
                value={form.availability}
                onChange={(e) => set("availability", e.target.value)}
              />
            </div>

            <div className="grid grid-cols-12 items-start gap-3">
              <label className="col-span-4 pt-2 text-sm text-gray-600">Conditions</label>
              <div className="col-span-8">
                <div className="flex gap-2">
                  <input
                    className="flex-1 h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                    placeholder="Add condition..."
                    value={condDraft}
                    onChange={(e) => setCondDraft(e.target.value)}
                  />
                  <button
                    type="button"
                    onClick={() => {
                      const v = condDraft.trim();
                      if (!v) return;
                      set("conditions", [...form.conditions, v]);
                      setCondDraft("");
                    }}
                    className="h-10 px-3 rounded-lg bg-blue-600 text-white text-sm font-semibold hover:bg-blue-700 inline-flex items-center gap-2"
                  >
                    <Plus size={16} />
                    Add
                  </button>
                </div>

                <div className="mt-3 flex flex-wrap gap-2">
                  {form.conditions.map((c, idx) => (
                    <div
                      key={`${c}-${idx}`}
                      className="inline-flex items-center gap-2 rounded-full border border-gray-200 bg-white px-3 py-1.5 text-xs text-gray-700"
                    >
                      <span className="max-w-[240px] truncate">{c}</span>
                      <button
                        type="button"
                        onClick={() => {
                          set("conditions", form.conditions.filter((_, i) => i !== idx));
                        }}
                        className="h-6 w-6 rounded-full grid place-items-center hover:bg-red-50"
                      >
                        <Trash2 size={14} className="text-red-500" />
                      </button>
                    </div>
                  ))}
                  {form.conditions.length === 0 && (
                    <div className="text-xs text-gray-400">No conditions</div>
                  )}
                </div>
              </div>
            </div>

            <div className="pt-2 text-xs text-gray-400">
              Priority now:{" "}
              <span className="font-semibold text-gray-600">{priorityLabel(form.priority)}</span>
            </div>
          </div>
        </div>

        <div className="px-6 py-4 border-t border-gray-100 flex items-center justify-end gap-3">
          <button
            type="button"
            onClick={onClose}
            className="h-10 rounded-lg border border-gray-200 bg-white px-4 text-sm font-semibold text-gray-700 hover:bg-gray-50"
            disabled={saving}
          >
            Cancel
          </button>
          <button
            type="button"
            onClick={submit}
            className={cn(
              "h-10 rounded-lg bg-blue-600 px-5 text-sm font-semibold text-white hover:bg-blue-700",
              (saving || loadingDetail) && "opacity-70 cursor-not-allowed"
            )}
            disabled={saving || loadingDetail}
          >
            {saving ? "Saving..." : "Update"}
          </button>
        </div>
      </div>
    </div>
  );
}
