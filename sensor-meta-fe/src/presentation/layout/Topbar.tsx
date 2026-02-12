// src/presentation/components/Topbar.tsx
import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { ChevronDown, LogOut } from "lucide-react";

import { authStorage } from "../../shared/lib/authStorage";
import { logout } from "../../infrastructure/api/logout";

type MeDto = {
  id: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
};

function pickBaseUrl() {
  return import.meta.env.VITE_API_URL || "https://localhost:7141";
}

async function getMe(baseUrl: string, token: string): Promise<MeDto> {
  const res = await fetch(`${baseUrl}/api/users/me`, {
    method: "GET",
    headers: {
      accept: "*/*",
      Authorization: `Bearer ${token}`,
    },
  });

  if (!res.ok) {
    const txt = await res.text().catch(() => "");
    throw new Error(`GET /api/users/me failed (${res.status}). ${txt}`);
  }

  const raw = await res.json();
  return {
    id: raw.id ?? raw.Id,
    email: raw.email ?? raw.Email,
    role: raw.role ?? raw.Role ?? "user",
    isActive: raw.isActive ?? raw.IsActive ?? true,
    createdAt: raw.createdAt ?? raw.CreatedAt,
  };
}

export function Topbar() {
  const baseUrl = pickBaseUrl();
  const nav = useNavigate();

  const [me, setMe] = useState<MeDto | null>(null);
  const [loadingMe, setLoadingMe] = useState(false);

  const [openMenu, setOpenMenu] = useState(false);
  const menuRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const token = authStorage.getAccessToken();
    if (!token) {
      setMe(null);
      return;
    }

    let alive = true;
    setLoadingMe(true);

    (async () => {
      try {
        const data = await getMe(baseUrl, token);
        if (!alive) return;
        setMe(data);
      } catch {
        if (!alive) return;
        setMe(null);
      } finally {
        if (alive) setLoadingMe(false);
      }
    })();

    return () => {
      alive = false;
    };
  }, [baseUrl]);

  // click outside => close dropdown
  useEffect(() => {
    if (!openMenu) return;
    const onDown = (e: MouseEvent) => {
      const el = menuRef.current;
      if (!el) return;
      if (!el.contains(e.target as Node)) setOpenMenu(false);
    };
    window.addEventListener("mousedown", onDown);
    return () => window.removeEventListener("mousedown", onDown);
  }, [openMenu]);

  function onLogout() {
    setOpenMenu(false);
    logout();
    nav("/login"); // đổi nếu route login khác
  }

  const displayName = me?.email ?? (loadingMe ? "Loading..." : "Unknown user");
  const displayRole = me?.role ?? "";

  return (
    <header className="px-8 pt-6">
      <div className="flex items-center justify-between">
        <div className="relative w-[380px]">{/* search area */}</div>

        <div className="flex items-center gap-5">
          {/* user info + dropdown */}
          <div className="relative" ref={menuRef}>
            <button
              type="button"
              onClick={() => setOpenMenu((v) => !v)}
              className="flex items-center gap-3 rounded-xl px-2 py-1.5 hover:bg-gray-50"
            >
              <div className="h-10 w-10 rounded-full bg-gray-200" />
              <div className="leading-tight text-left">
                <div className="text-sm font-semibold text-gray-900">{displayName}</div>
                <div className="text-xs text-gray-500">{displayRole || "user"}</div>
              </div>
              <ChevronDown size={16} className="text-gray-500" />
            </button>

            {openMenu && (
              <div className="absolute right-0 mt-2 w-52 rounded-xl border border-gray-100 bg-white shadow-lg overflow-hidden">
                <button
                  type="button"
                  onClick={onLogout}
                  className="w-full flex items-center gap-2 px-4 py-3 text-sm text-gray-700 hover:bg-gray-50"
                >
                  <LogOut size={16} className="text-gray-500" />
                  Logout
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </header>
  );
}
