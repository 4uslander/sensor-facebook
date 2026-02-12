// src/presentation/components/ProxyEditDrawer.tsx
import { useEffect, useMemo, useState } from "react";
import { X } from "lucide-react";
import type { ProxyGroupDto, UpdateProxyGroupRequest } from "../../infrastructure/api/proxyGroupsApi";
import { proxyGroupsApi } from "../../infrastructure/api/proxyGroupsApi";
import { authStorage } from "../../shared/lib/authStorage"; // ✅ chỉnh path nếu khác

function cn(...s: Array<string | false | null | undefined>) {
  return s.filter(Boolean).join(" ");
}

type FormState = {
  name: string;
  region: string;
  status: "active" | "inactive";
  protocol: "http" | "https" | "socks4" | "socks5";
  host: string;
  port: number;
  authUsername: string;
  authPasswordPlain: string;
  provider: string;
  isRotating: boolean;
  maxConcurrency: number;
  rateLimitRpm: number;
  metadataJsonText: string;
};

function toEndpoint(dto: ProxyGroupDto) {
  return dto.endpoint || `${dto.protocol}://${dto.host}:${dto.port}`;
}

function safeJsonStringify(v: unknown) {
  try {
    return JSON.stringify(v ?? null, null, 2);
  } catch {
    return "";
  }
}

function parseMetadataJson(text: string): any | null {
  const t = text.trim();
  if (!t) return null;
  return JSON.parse(t);
}

function normalizeStatus(s: string | null | undefined): FormState["status"] {
  return s?.toLowerCase() === "inactive" ? "inactive" : "active";
}

function normalizeProtocol(p: string | null | undefined): FormState["protocol"] {
  const v = (p || "http").toLowerCase();
  if (v === "https") return "https";
  if (v === "socks4") return "socks4";
  if (v === "socks5") return "socks5";
  return "http";
}

export default function ProxyEditDrawer({
  open,
  proxy,
  token,
  apiBase,
  onClose,
  onUpdated,
}: {
  open: boolean;
  proxy: ProxyGroupDto | null; // từ list
  token?: string | null;
  apiBase?: string;
  onClose: () => void;
  onUpdated: () => void; // refresh list
}) {
  const baseUrl = apiBase || import.meta.env.VITE_API_URL || "https://localhost:7141";
  const accessToken = token ?? authStorage.getAccessToken(); // ✅ tránh 401

  // initial tạm để render ngay, sẽ bị overwrite sau khi load detail
  const initial: FormState = useMemo(
    () => ({
      name: proxy?.name ?? "",
      region: proxy?.region ?? "",
      status: normalizeStatus(proxy?.status),
      protocol: normalizeProtocol(proxy?.protocol),
      host: proxy?.host ?? "",
      port: proxy?.port ?? 0,
      authUsername: "",
      authPasswordPlain: "",
      provider: proxy?.provider ?? "",
      isRotating: proxy?.isRotating ?? false,
      maxConcurrency: proxy?.maxConcurrency ?? 3,
      rateLimitRpm: proxy?.rateLimitRpm ?? 0,
      metadataJsonText: safeJsonStringify((proxy as any)?.metadataJson ?? null),
    }),
    [proxy]
  );

  const [form, setForm] = useState<FormState>(initial);
  const [saving, setSaving] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  // reset when open/proxy changes
  useEffect(() => {
    if (!open) return;
    setForm(initial);
    setErr(null);
    setSaving(false);
  }, [open, initial]);

  // ✅ LOAD FULL DETAIL ON OPEN
  useEffect(() => {
    if (!open) return;
    if (!proxy?.id) return;

    let alive = true;

    (async () => {
      try {
        setLoadingDetail(true);
        setErr(null);

        const api = proxyGroupsApi(baseUrl, accessToken || undefined);
        const d = await api.get(proxy.id);

        if (!alive) return;

        setForm({
          name: d.name ?? "",
          region: d.region ?? "",
          status: normalizeStatus(d.status),
          protocol: normalizeProtocol(d.protocol),
          host: d.host ?? "",
          port: d.port ?? 0,

          // nếu BE có trả authUsername thì show, không có thì để trống
          authUsername: (d as any).authUsername ?? "",
          authPasswordPlain: "",

          provider: d.provider ?? "",
          isRotating: !!d.isRotating,
          maxConcurrency: d.maxConcurrency ?? 3,
          rateLimitRpm: d.rateLimitRpm ?? 0,

          metadataJsonText: safeJsonStringify((d as any).metadataJson ?? null),
        });
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
  }, [open, proxy?.id, baseUrl, accessToken]);

  // ESC to close
  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((p) => ({ ...p, [key]: value }));
  }

  async function submit() {
    if (!proxy) return;

    if (!form.name.trim()) return setErr("Name is required.");
    if (!form.host.trim()) return setErr("Host is required.");
    if (!Number.isFinite(form.port) || form.port <= 0 || form.port > 65535)
      return setErr("Port must be in range 1–65535.");

    setSaving(true);
    setErr(null);

    let metadata: any | null | undefined = undefined;
    if (form.metadataJsonText.trim()) {
      try {
        metadata = parseMetadataJson(form.metadataJsonText);
      } catch {
        setSaving(false);
        setErr("Metadata JSON is invalid JSON.");
        return;
      }
    }

    const body: UpdateProxyGroupRequest = {
      name: form.name.trim(),
      region: form.region.trim() ? form.region.trim() : null,
      status: form.status,
      protocol: form.protocol,
      host: form.host.trim(),
      port: form.port,

      authUsername: form.authUsername.trim() ? form.authUsername.trim() : null,
      authPasswordPlain: form.authPasswordPlain.trim() ? form.authPasswordPlain.trim() : null,

      provider: form.provider.trim() ? form.provider.trim() : null,
      isRotating: form.isRotating,
      maxConcurrency: form.maxConcurrency,
      rateLimitRpm: form.rateLimitRpm || null,
      ...(metadata !== undefined ? { metadataJson: metadata } : {}),
    };

    try {
      const api = proxyGroupsApi(baseUrl, accessToken || undefined);
      await api.update(proxy.id, body);
      onUpdated();
      onClose();
    } catch (e: any) {
      setErr(e?.message || "Update failed");
    } finally {
      setSaving(false);
    }
  }

  if (!open || !proxy) return null;

  return (
    <div className="fixed inset-0 z-[140]">
      <div className="absolute inset-0 bg-black/20" onClick={onClose} />

      <div
        className={cn(
          "absolute right-0 top-0 h-full w-[520px] max-w-[92vw] bg-white border-l border-gray-100",
          "shadow-[0_30px_120px_-60px_rgba(0,0,0,0.6)]",
          "flex flex-col"
        )}
      >
        <div className="px-6 py-5 border-b border-gray-100 flex items-center justify-between">
          <div>
            <div className="text-sm font-semibold text-gray-900">Edit Proxy Group</div>
            <div className="text-xs text-gray-500">
              ID: {proxy.id} • Endpoint: <span className="font-medium">{toEndpoint(proxy)}</span>
            </div>
            {loadingDetail && <div className="mt-1 text-xs text-gray-400">Loading detail...</div>}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="h-9 w-9 rounded-lg border border-gray-100 grid place-items-center hover:bg-gray-50"
          >
            <X size={16} className="text-gray-600" />
          </button>
        </div>

        <div className="flex-1 overflow-auto px-6 py-5">
          {err && (
            <div className="mb-4 rounded-xl border border-red-100 bg-red-50 px-4 py-3 text-sm text-red-700">
              {err}
            </div>
          )}

          <div className="space-y-3">
            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Name</label>
              <input
                className="col-span-8 h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                value={form.name}
                onChange={(e) => set("name", e.target.value)}
              />
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Region</label>
              <input
                className="col-span-8 h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                value={form.region}
                onChange={(e) => set("region", e.target.value)}
              />
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Status</label>
              <select
                className="col-span-8 h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100 bg-white"
                value={form.status}
                onChange={(e) => set("status", e.target.value as FormState["status"])}
              >
                <option value="active">active</option>
                <option value="inactive">inactive</option>
              </select>
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Protocol</label>
              <select
                className="col-span-8 h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100 bg-white"
                value={form.protocol}
                onChange={(e) => set("protocol", e.target.value as FormState["protocol"])}
              >
                <option value="http">http</option>
                <option value="https">https</option>
                <option value="socks4">socks4</option>
                <option value="socks5">socks5</option>
              </select>
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Host / Port</label>
              <div className="col-span-8 grid grid-cols-2 gap-2">
                <input
                  className="h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                  value={form.host}
                  onChange={(e) => set("host", e.target.value)}
                />
                <input
                  type="number"
                  className="h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                  value={form.port}
                  onChange={(e) => set("port", Number(e.target.value))}
                />
              </div>
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Auth</label>
              <div className="col-span-8 grid grid-cols-2 gap-2">
                <input
                  className="h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                  placeholder={proxy.hasAuth ? "Username (hasAuth)" : "Username"}
                  value={form.authUsername}
                  onChange={(e) => set("authUsername", e.target.value)}
                />
                <input
                  type="password"
                  className="h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                  placeholder="Password (leave blank to keep)"
                  value={form.authPasswordPlain}
                  onChange={(e) => set("authPasswordPlain", e.target.value)}
                />
              </div>
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Provider</label>
              <input
                className="col-span-8 h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                value={form.provider}
                onChange={(e) => set("provider", e.target.value)}
              />
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Is Rotating</label>
              <div className="col-span-8 flex items-center gap-3">
                <button
                  type="button"
                  onClick={() => set("isRotating", !form.isRotating)}
                  className={cn(
                    "relative inline-flex h-6 w-11 items-center rounded-full transition",
                    form.isRotating ? "bg-blue-600" : "bg-gray-200"
                  )}
                >
                  <span
                    className={cn(
                      "inline-block h-5 w-5 rounded-full bg-white shadow-sm transition",
                      form.isRotating ? "translate-x-5" : "translate-x-1"
                    )}
                  />
                </button>
                <span className="text-sm text-gray-700">{form.isRotating ? "Yes" : "No"}</span>
              </div>
            </div>

            <div className="grid grid-cols-12 items-center gap-3">
              <label className="col-span-4 text-sm text-gray-600">Limits</label>
              <div className="col-span-8 grid grid-cols-2 gap-2">
                <input
                  type="number"
                  className="h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                  placeholder="maxConcurrency"
                  value={form.maxConcurrency}
                  onChange={(e) => set("maxConcurrency", Math.max(0, Number(e.target.value)))}
                />
                <input
                  type="number"
                  className="h-10 rounded-lg border border-gray-200 px-3 text-sm outline-none focus:ring-2 focus:ring-blue-100"
                  placeholder="rateLimitRpm"
                  value={form.rateLimitRpm}
                  onChange={(e) => set("rateLimitRpm", Math.max(0, Number(e.target.value)))}
                />
              </div>
            </div>

            <div className="grid grid-cols-12 items-start gap-3">
              <label className="col-span-4 pt-2 text-sm text-gray-600">Metadata JSON</label>
              <textarea
                className="col-span-8 min-h-[120px] rounded-lg border border-gray-200 px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-blue-100 resize-y"
                value={form.metadataJsonText}
                onChange={(e) => set("metadataJsonText", e.target.value)}
                placeholder='{"key":"value"} (optional)'
              />
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
