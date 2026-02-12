// src/presentation/components/AddProxyModal.tsx
import { useEffect, useMemo, useRef, useState } from "react";
import { X } from "lucide-react";
import type { CreateProxyGroupRequest } from "../../infrastructure/api/proxyGroupsApi";

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

function parseMetadataJson(text: string): any | null {
  const t = text.trim();
  if (!t) return null;
  try {
    return JSON.parse(t);
  } catch {
    // cho phép nhập raw json element (string) không hợp lệ -> throw cho UI
    throw new Error("Metadata JSON is invalid JSON.");
  }
}

export default function AddProxyModal({
  open,
  onClose,
  onSubmit,
}: {
  open: boolean;
  onClose: () => void;
  onSubmit: (body: CreateProxyGroupRequest) => Promise<void> | void;
}) {
  const panelRef = useRef<HTMLDivElement | null>(null);

  const initial: FormState = useMemo(
    () => ({
      name: "",
      region: "",
      status: "active",
      protocol: "http",
      host: "",
      port: 0,

      authUsername: "",
      authPasswordPlain: "",

      provider: "",
      isRotating: false,
      maxConcurrency: 3,
      rateLimitRpm: 0,

      metadataJsonText: "",
    }),
    []
  );

  const [form, setForm] = useState<FormState>(initial);
  const [touched, setTouched] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) return;
    setForm(initial);
    setTouched(false);
    setErr(null);
    setSaving(false);

    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose, initial]);

  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      const target = e.target as Node;
      if (panelRef.current && !panelRef.current.contains(target)) onClose();
    };
    document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, [open, onClose]);

  function set<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((p) => ({ ...p, [key]: value }));
  }

  if (!open) return null;

  const validName = form.name.trim().length >= 2;
  const validHost = form.host.trim().length >= 2;
  const validPort = Number.isFinite(form.port) && form.port > 0 && form.port <= 65535;
  const isValid = validName && validHost && validPort;

  async function submit() {
    setTouched(true);
    setErr(null);
    if (!isValid) return;

    let metadata: any | null = null;
    try {
      metadata = parseMetadataJson(form.metadataJsonText);
    } catch (e: any) {
      setErr(e?.message || "Invalid metadata JSON.");
      return;
    }

    const body: CreateProxyGroupRequest = {
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

      metadataJson: metadata,
    };

    try {
      setSaving(true);
      await onSubmit(body);
      onClose();
    } catch (e: any) {
      setErr(e?.message || "Create failed");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="fixed inset-0 z-[140] grid place-items-center">
      <div className="absolute inset-0 bg-black/10" />

      <div
        ref={panelRef}
        className="relative w-[720px] max-w-[calc(100vw-40px)] rounded-2xl bg-white border border-gray-100 shadow-[0_25px_80px_-45px_rgba(0,0,0,0.45)]"
      >
        <div className="p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="text-sm font-bold text-gray-900">New Proxy Group</div>
            <button
              type="button"
              onClick={onClose}
              className="h-9 w-9 rounded-lg border border-gray-100 grid place-items-center hover:bg-gray-50"
              disabled={saving}
            >
              <X size={16} className="text-gray-600" />
            </button>
          </div>

          {err ? (
            <div className="mb-4 rounded-xl border border-red-100 bg-red-50 px-4 py-3 text-sm text-red-700">
              {err}
            </div>
          ) : null}

          <div className="grid grid-cols-12 gap-x-6 gap-y-4">
            <div className="col-span-3 flex items-center">
              <div className="text-xs text-gray-600">Name</div>
            </div>
            <div className="col-span-9">
              <input
                value={form.name}
                onChange={(e) => set("name", e.target.value)}
                onBlur={() => setTouched(true)}
                placeholder="Proxy name"
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
              />
              {touched && !validName ? (
                <div className="mt-1 text-[11px] text-red-500">Name tối thiểu 2 ký tự.</div>
              ) : null}
            </div>

            <div className="col-span-3 flex items-center">
              <div className="text-xs text-gray-600">Region</div>
            </div>
            <div className="col-span-9">
              <input
                value={form.region}
                onChange={(e) => set("region", e.target.value)}
                placeholder="US / SG / ..."
                className="w-full h-9 rounded-md border border-gray-200 bg-white px-3 text-xs outline-none focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
              />
            </div>

            <div className="col-span-3 flex items-center">
              <div className="text-xs text-gray-600">Status</div>
            </div>
            <div className="col-span-9">
              <select
                value={form.status}
                onChange={(e) => set("status", e.target.value as FormState["status"])}
                className="w-full h-9 rounded-md border border-gray-200 bg-white px-3 text-xs outline-none focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
              >
                <option value="active">active</option>
                <option value="inactive">inactive</option>
              </select>
            </div>

            <div className="col-span-3 flex items-center">
              <div className="text-xs text-gray-600">Protocol</div>
            </div>
            <div className="col-span-9">
              <select
                value={form.protocol}
                onChange={(e) => set("protocol", e.target.value as FormState["protocol"])}
                className="w-full h-9 rounded-md border border-gray-200 bg-white px-3 text-xs outline-none focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
              >
                <option value="http">http</option>
                <option value="https">https</option>
                <option value="socks4">socks4</option>
                <option value="socks5">socks5</option>
              </select>
            </div>

            <div className="col-span-3 flex items-center">
              <div className="text-xs text-gray-600">Host</div>
            </div>
            <div className="col-span-5">
              <input
                value={form.host}
                onChange={(e) => set("host", e.target.value)}
                onBlur={() => setTouched(true)}
                placeholder="1.2.3.4"
                className="w-full h-9 rounded-md border border-gray-200 bg-white px-3 text-xs outline-none focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
              />
              {touched && !validHost ? (
                <div className="mt-1 text-[11px] text-red-500">Host tối thiểu 2 ký tự.</div>
              ) : null}
            </div>
            <div className="col-span-1 flex items-center">
              <div className="text-xs text-gray-600">Port</div>
            </div>
            <div className="col-span-3">
              <input
                type="number"
                value={form.port}
                onChange={(e) => set("port", Number(e.target.value))}
                onBlur={() => setTouched(true)}
                placeholder="8080"
                className="w-full h-9 rounded-md border border-gray-200 bg-white px-3 text-xs outline-none focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
              />
              {touched && !validPort ? (
                <div className="mt-1 text-[11px] text-red-500">Port 1–65535.</div>
              ) : null}
            </div>

            <div className="col-span-3 flex items-center">
              <div className="text-xs text-gray-600">Auth Username</div>
            </div>
            <div className="col-span-4">
              <input
                value={form.authUsername}
                onChange={(e) => set("authUsername", e.target.value)}
                placeholder="Optional"
                className="w-full h-9 rounded-md border border-gray-200 bg-white px-3 text-xs outline-none focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
              />
            </div>
            <div className="col-span-2 flex items-center">
              <div className="text-xs text-gray-600">Password</div>
            </div>
            <div className="col-span-3">
              <input
                type="password"
                value={form.authPasswordPlain}
                onChange={(e) => set("authPasswordPlain", e.target.value)}
                placeholder="Optional"
                className="w-full h-9 rounded-md border border-gray-200 bg-white px-3 text-xs outline-none focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
              />
            </div>

            <div className="col-span-3 flex items-center">
              <div className="text-xs text-gray-600">Provider</div>
            </div>
            <div className="col-span-9">
              <input
                value={form.provider}
                onChange={(e) => set("provider", e.target.value)}
                placeholder="Webshare / IPRoyal / ..."
                className="w-full h-9 rounded-md border border-gray-200 bg-white px-3 text-xs outline-none focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
              />
            </div>

            <div className="col-span-3 flex items-center">
              <div className="text-xs text-gray-600">Is Rotating</div>
            </div>
            <div className="col-span-9 flex items-center gap-3">
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
              <span className="text-xs text-gray-700">{form.isRotating ? "Yes" : "No"}</span>
            </div>

            <div className="col-span-3 flex items-center">
              <div className="text-xs text-gray-600">Max Concurrency</div>
            </div>
            <div className="col-span-3">
              <input
                type="number"
                value={form.maxConcurrency}
                onChange={(e) => set("maxConcurrency", Math.max(0, Number(e.target.value)))}
                className="w-full h-9 rounded-md border border-gray-200 bg-white px-3 text-xs outline-none focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
              />
            </div>
            <div className="col-span-3 flex items-center">
              <div className="text-xs text-gray-600">Rate Limit (RPM)</div>
            </div>
            <div className="col-span-3">
              <input
                type="number"
                value={form.rateLimitRpm}
                onChange={(e) => set("rateLimitRpm", Math.max(0, Number(e.target.value)))}
                className="w-full h-9 rounded-md border border-gray-200 bg-white px-3 text-xs outline-none focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
              />
            </div>

            <div className="col-span-3 flex items-start pt-2">
              <div className="text-xs text-gray-600">Metadata JSON</div>
            </div>
            <div className="col-span-9">
              <textarea
                value={form.metadataJsonText}
                onChange={(e) => set("metadataJsonText", e.target.value)}
                placeholder='{"key":"value"}'
                className="w-full min-h-[100px] rounded-md border border-gray-200 bg-white px-3 py-2 text-xs outline-none focus:border-blue-300 focus:ring-2 focus:ring-blue-100 resize-y"
              />
              <div className="mt-2 text-[11px] text-gray-400">JSON object (optional).</div>
            </div>
          </div>

          <div className="mt-6 flex justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              disabled={saving}
              className="h-9 rounded-md border border-gray-200 bg-white px-4 text-xs font-semibold text-gray-700 hover:bg-gray-50 disabled:opacity-60"
            >
              Discard
            </button>

            <button
              type="button"
              onClick={submit}
              disabled={!isValid || saving}
              className={cn(
                "h-9 rounded-md px-4 text-xs font-semibold text-white",
                !isValid || saving ? "bg-blue-300 cursor-not-allowed" : "bg-blue-600 hover:bg-blue-700"
              )}
            >
              {saving ? "Adding..." : "Add Proxy"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
