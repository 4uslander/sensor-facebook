import { NavLink, useNavigate } from "react-router-dom";
import {
  LayoutDashboard,
  ShoppingBag,
  KeyRound,
  Tags,
  Network,
  // Monitor,
  ListChecks,
  Users,
  Settings,
  LogOut,
} from "lucide-react";
import { logout } from "../../infrastructure/api/logout";

const navItems = [
  { to: "/", label: "Dashboard", icon: LayoutDashboard },
  { to: "/products", label: "Products", icon: ShoppingBag },
  { to: "/keywords", label: "Keywords", icon: KeyRound },
  { to: "/categories", label: "Categories", icon: Tags },
  { to: "/proxy-groups", label: "Proxy groups", icon: Network },
  // { to: "/browser-pool", label: "Browser pool", icon: Monitor },
  { to: "/job-queue", label: "Job queue", icon: ListChecks },
  { to: "/account-manager", label: "Account manager", icon: Users },
];

export function Sidebar() {
  const nav = useNavigate();
  return (
    <aside className="w-64 min-h-screen bg-white border-r border-gray-100 flex flex-col">
      <div className="px-6 py-6">
        <div className="text-lg font-extrabold text-gray-900">
          Sensor <span className="text-blue-600">Facebook</span>
        </div>
      </div>

      <nav className="px-3 space-y-1">
        {navItems.map((it) => {
          const Icon = it.icon;
          return (
            <NavLink
              key={it.to}
              to={it.to}
              end={it.to === "/"}
              className={({ isActive }) =>
                [
                  "flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium",
                  isActive
                    ? "bg-blue-600 text-white shadow-sm"
                    : "text-gray-600 hover:bg-gray-50",
                ].join(" ")
              }
            >
              <Icon size={18} />
              {it.label}
            </NavLink>
          );
        })}
      </nav>

      <div className="mt-auto px-3 py-6 space-y-1">
        <button className="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium text-gray-600 hover:bg-gray-50">
          <Settings size={18} />
          Settings
        </button>
        <button
          type="button"
          onClick={() => {
            logout();
            nav("/login", { replace: true });
          }}
          className="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium text-gray-600 hover:bg-gray-50"
        >
          <LogOut size={18} />
          Logout
        </button>
      </div>
    </aside>
  );
}
