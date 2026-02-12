import type { ReactNode } from "react";
import { Sidebar } from "./Sidebar";
import { Topbar } from "./Topbar";

export default function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen bg-[#F5F7FB]">
      <div className="flex">
        <Sidebar />
        <div className="flex-1">
          <Topbar />
          <main className="px-8 py-6">{children}</main>
        </div>
      </div>
    </div>
  );
}
