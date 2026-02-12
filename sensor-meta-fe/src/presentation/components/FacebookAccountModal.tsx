// src/presentation/components/FacebookAccountModal.tsx
import { useEffect, useMemo, useRef, useState } from "react";
import { X } from "lucide-react";
import { accountsApi, type CreateOrUpdateAccountRequest } from "../../infrastructure/api/accountsApi";

function cn(...s: Array<string | false | null | undefined>) {
  return s.filter(Boolean).join(" ");
}

function toInt(v: string) {
  const n = Number(v);
  return Number.isFinite(n) ? Math.trunc(n) : 0;
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

export type FbAccountPayload = {
  id?: string;
  email: string;
  displayName: string;
  proxyGroupId: number;
  profileDir: string;
  cookiePlain: string;
  status: string; // UI label: "Active" | "Inactive" | ...
};

type Props = {
  open: boolean;
  mode: "add" | "edit";
  initial?: FbAccountPayload;
  onClose: () => void;

  // gọi để Table refetch (invalidate query hoặc fetch lại)
  onSaved: (id: string) => void;

  // optional: override apiBase/token nếu bạn muốn
  apiBase?: string;
  token?: string | null;
};

function uiStatusToApi(status: string): string {
  const s = status.trim().toLowerCase();
  // UI hiện đang chỉ có Active/Inactive
  if (s === "active") return "active";
  if (s === "inactive") return "inactive";

  // hỗ trợ trường hợp UI gửi "Active"/"Inactive"
  if (s === "active".toLowerCase()) return "active";
  if (s === "inactive".toLowerCase()) return "inactive";

  // fallback: gửi raw (nhưng BE sẽ validate)
  return s;
}

export default function FacebookAccountModal({
  open,
  mode,
  initial,
  onClose,
  onSaved,
  apiBase,
  token,
}: Props) {
  const panelRef = useRef<HTMLDivElement>(null);

  const baseUrl = apiBase ?? import.meta.env.VITE_API_URL ?? "https://localhost:7141";
  const accessToken = token ?? getAccessTokenFromLocalStorage();

  const [id, setId] = useState(initial?.id ?? "");
  const [email, setEmail] = useState(initial?.email ?? "");
  const [displayName, setDisplayName] = useState(initial?.displayName ?? "");
  const [proxyGroupId, setProxyGroupId] = useState<string>(
    initial?.proxyGroupId != null ? String(initial.proxyGroupId) : ""
  );
  const [profileDir, setProfileDir] = useState(initial?.profileDir ?? "");
  const [cookiePlain, setCookiePlain] = useState(initial?.cookiePlain ?? "");
  const [status, setStatus] = useState(initial?.status ?? "Active");

  const [touched, setTouched] = useState(false);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;

    setId(initial?.id ?? "");
    setEmail(initial?.email ?? "");
    setDisplayName(initial?.displayName ?? "");
    setProxyGroupId(initial?.proxyGroupId != null ? String(initial.proxyGroupId) : "");
    setProfileDir(initial?.profileDir ?? "");
    setCookiePlain(initial?.cookiePlain ?? "");
    setStatus(initial?.status ?? "Active");
    setTouched(false);
    setSaving(false);
    setErr(null);

    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose, initial]);

  // click outside
  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      const target = e.target as Node;
      if (panelRef.current && !panelRef.current.contains(target)) onClose();
    };
    document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, [open, onClose]);

  const proxyGroupIdNum = useMemo(() => toInt(proxyGroupId), [proxyGroupId]);

  // validations
  const validId = mode === "add" ? true : id.trim().length >= 8;
  const validEmail = email.trim().length >= 6 && email.includes("@");
  const validDisplayName = displayName.trim().length >= 1;
  const validProxyGroupId = proxyGroupId.trim() !== "" && proxyGroupIdNum >= 0;
  const validStatus = status.trim().length > 0;

  const isValid = validId && validEmail && validDisplayName && validProxyGroupId && validStatus;

  async function submit() {
    setTouched(true);
    setErr(null);
    if (!isValid || saving) return;

    if (!accessToken) {
      setErr("Missing access token. Please login again.");
      return;
    }

    const body: CreateOrUpdateAccountRequest = {
      ...(mode === "edit" ? { id: id.trim() } : {}),
      email: email.trim(),
      displayName: displayName.trim(),
      proxyGroupId: proxyGroupIdNum,
      profileDir: profileDir.trim() || null,
      cookiePlain: cookiePlain?.trim() ? cookiePlain : null,
      status: uiStatusToApi(status),
    };

    try {
      setSaving(true);
      const api = accountsApi(baseUrl, accessToken);
      const res = await api.upsert(body); // POST /api/accounts
      onSaved(res.id);
      onClose();
    } catch (e: any) {
      setErr(e?.message || "Save failed");
    } finally {
      setSaving(false);
    }
  }

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-[140] grid place-items-center px-4">
      <div className="absolute inset-0 bg-black/10" />

      <div
        ref={panelRef}
        className="relative w-[760px] max-w-[calc(100vw-24px)] rounded-2xl bg-white border border-gray-100 shadow-[0_25px_90px_-45px_rgba(0,0,0,0.55)]"
      >
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <div className="text-sm font-bold text-gray-900">
            {mode === "add" ? "Add Account" : "Edit Account"}
          </div>
          <button
            type="button"
            onClick={onClose}
            className="h-9 w-9 rounded-lg border border-gray-100 grid place-items-center hover:bg-gray-50"
            aria-label="Close"
            disabled={saving}
          >
            <X size={16} className="text-gray-600" />
          </button>
        </div>

        <div className="p-6">
          {err ? (
            <div className="mb-4 rounded-xl border border-red-100 bg-red-50 px-4 py-3 text-sm text-red-700">
              {err}
            </div>
          ) : null}

          <div className="grid grid-cols-12 gap-x-6 gap-y-4">
            {/* ID (edit only) */}
            {mode === "edit" ? (
              <>
                <div className="col-span-4 flex items-center">
                  <div className="text-xs text-gray-600">Id *</div>
                </div>
                <div className="col-span-8">
                  <input
                    value={id}
                    onChange={(e) => setId(e.target.value)}
                    onBlur={() => setTouched(true)}
                    placeholder="3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    className={cn(
                      "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                      "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                    )}
                    disabled={saving}
                  />
                  {touched && !validId ? (
                    <div className="mt-1 text-[11px] text-red-500">Id là bắt buộc khi edit.</div>
                  ) : null}
                </div>
              </>
            ) : null}

            {/* Email */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Email *</div>
            </div>
            <div className="col-span-8">
              <input
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                onBlur={() => setTouched(true)}
                placeholder="example@gmail.com"
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
                disabled={saving}
              />
              {touched && !validEmail ? (
                <div className="mt-1 text-[11px] text-red-500">Email không hợp lệ.</div>
              ) : null}
            </div>

            {/* Display Name */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Display Name *</div>
            </div>
            <div className="col-span-8">
              <input
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                onBlur={() => setTouched(true)}
                placeholder="Moni Roy"
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
                disabled={saving}
              />
              {touched && !validDisplayName ? (
                <div className="mt-1 text-[11px] text-red-500">Display Name là bắt buộc.</div>
              ) : null}
            </div>

            {/* Proxy Group Id */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Proxy Group Id *</div>
            </div>
            <div className="col-span-8">
              <input
                value={proxyGroupId}
                onChange={(e) => setProxyGroupId(e.target.value.replace(/[^\d]/g, ""))}
                onBlur={() => setTouched(true)}
                inputMode="numeric"
                placeholder="0"
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
                disabled={saving}
              />
              {touched && !validProxyGroupId ? (
                <div className="mt-1 text-[11px] text-red-500">ProxyGroupId phải là số.</div>
              ) : null}
            </div>

            {/* Profile Dir */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Profile Dir</div>
            </div>
            <div className="col-span-8">
              <input
                value={profileDir}
                onChange={(e) => setProfileDir(e.target.value)}
                placeholder="profiles/acc01"
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
                disabled={saving}
              />
            </div>

            {/* Cookie Plain */}
            <div className="col-span-4 flex items-start pt-2">
              <div className="text-xs text-gray-600">Cookie Plain</div>
            </div>
            <div className="col-span-8">
              <textarea
                value={cookiePlain}
                onChange={(e) => setCookiePlain(e.target.value)}
                placeholder="datr=...; c_user=...; xs=..."
                rows={5}
                className={cn(
                  "w-full rounded-md border bg-white px-3 py-2 text-xs outline-none resize-y",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
                disabled={saving}
              />
            </div>

            {/* Status */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Status *</div>
            </div>
            <div className="col-span-8">
              <select
                value={status}
                onChange={(e) => setStatus(e.target.value)}
                onBlur={() => setTouched(true)}
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
                disabled={saving}
              >
                <option value="Active">Active</option>
                <option value="Inactive">Inactive</option>
                {/* nếu sau này BE hỗ trợ: */}
                {/* <option value="Checkpoint">Checkpoint</option>
                <option value="Disabled">Disabled</option> */}
              </select>
              {touched && !validStatus ? (
                <div className="mt-1 text-[11px] text-red-500">Status là bắt buộc.</div>
              ) : null}
            </div>
          </div>

          <div className="mt-6 flex justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="h-9 rounded-md border border-gray-200 bg-white px-4 text-xs font-semibold text-gray-700 hover:bg-gray-50"
              disabled={saving}
            >
              Discard
            </button>

            <button
              type="button"
              onClick={submit}
              className={cn(
                "h-9 rounded-md px-4 text-xs font-semibold text-white",
                isValid && !saving ? "bg-blue-600 hover:bg-blue-700" : "bg-blue-300 cursor-not-allowed"
              )}
              disabled={!isValid || saving}
            >
              {saving ? "Saving..." : mode === "add" ? "Add Account" : "Update"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
