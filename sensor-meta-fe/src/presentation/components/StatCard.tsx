//src\presentation\components\StatCard.tsx
import type { ReactNode } from "react";

const toneMap: Record<string, { bg: string; iconBg: string }> = {
  amber: { bg: "bg-white", iconBg: "bg-amber-100 text-amber-600" },
  green: { bg: "bg-white", iconBg: "bg-green-100 text-green-600" },
  indigo: { bg: "bg-white", iconBg: "bg-indigo-100 text-indigo-600" },
  orange: { bg: "bg-white", iconBg: "bg-orange-100 text-orange-600" },
};

export default function StatCard({
  title,
  value,
  icon,
  tone,
}: {
  title: string;
  value: string;
  icon: ReactNode;
  tone: keyof typeof toneMap;
}) {
  const t = toneMap[tone];

  return (
    <div className={`rounded-2xl ${t.bg} border border-gray-100 shadow-[0_20px_70px_-45px_rgba(0,0,0,0.25)] p-5`}>
      <div className="flex items-center justify-between">
        <div>
          <div className="text-xs text-gray-500">{title}</div>
          <div className="mt-2 text-2xl font-extrabold text-gray-900">{value}</div>
        </div>
        <div className={`h-11 w-11 rounded-2xl grid place-items-center ${t.iconBg}`}>
          {icon}
        </div>
      </div>
    </div>
  );
}
