import { StarOff, X } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useKeywordsQuery } from "../hooks/useKeywordsQuery";
import type { KeywordDto } from "../../infrastructure/api/keywords.api";

type Props = {
  selectedId: number | null;
  onSelect: (id: number) => void;
};

export default function KeywordPanel({ selectedId, onSelect }: Props) {
  const [q, setQ] = useState("");

  // ✅ luôn load danh sách ngay từ đầu (q undefined => backend không filter)
  const queryParams = useMemo(
    () => ({
      page: 1,
      pageSize: 200,
      q: q.trim() ? q.trim() : undefined,
      // active: true, // nếu muốn chỉ lấy keyword active
    }),
    [q]
  );

  const { data, isLoading, isError, error, isFetching } = useKeywordsQuery(queryParams);

  const list: KeywordDto[] = useMemo(() => data?.items ?? [], [data]);

  // ✅ auto select keyword đầu tiên khi list có data và chưa chọn gì
  useEffect(() => {
    if (selectedId != null) return;
    if (list.length === 0) return;
    onSelect(list[0].id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedId, list.length]);

  // mock actions (chưa có API pinned/remove)
  function togglePin(_id: number) {}
  function remove(_id: number) {}

  return (
    <div className="rounded-2xl bg-white border border-gray-100 shadow-[0_20px_70px_-45px_rgba(0,0,0,0.25)] p-5">
      <div className="text-sm font-bold text-gray-900 mb-4">Keywords</div>

      <input
        className="w-full mb-4 rounded-lg bg-indigo-50/60 px-4 py-2.5 text-sm text-gray-900 outline-none ring-1 ring-indigo-100 focus:ring-2 focus:ring-indigo-200"
        value={q}
        onChange={(e) => setQ(e.target.value)}
        placeholder="Search keyword..."
      />

      {isLoading ? (
        <div className="text-sm text-gray-500 py-6">Loading...</div>
      ) : isError ? (
        <div className="text-sm text-red-600 py-6">
          {(error as any)?.response?.data?.error || (error as any)?.message || "Failed to load keywords"}
        </div>
      ) : list.length === 0 ? (
        <div className="text-sm text-gray-500 py-6">No keywords</div>
      ) : (
        <div className="relative">
          <div className="absolute left-0 top-0 bottom-0 w-3 flex items-center justify-center">
            <div className="h-40 w-1 rounded-full bg-gray-200" />
          </div>

          <div className="pl-4 max-h-[520px] overflow-auto pr-2 space-y-3">
            {list.map((k) => {
              const active = selectedId === k.id;

              return (
                <button
                  key={k.id}
                  onClick={() => onSelect(k.id)}
                  className={[
                    "w-full flex items-center justify-between rounded-xl border px-3 py-3 text-sm",
                    active
                      ? "bg-blue-600 text-white border-blue-600 shadow-sm"
                      : "bg-white text-gray-700 border-gray-100 hover:bg-gray-50",
                  ].join(" ")}
                >
                  <div className="flex items-center gap-2">
                    <span
                      className={[
                        "h-4 w-4 rounded-full border grid place-items-center",
                        active ? "border-white/60" : "border-gray-200",
                      ].join(" ")}
                    >
                      <span className={["h-2 w-2 rounded-full", active ? "bg-white" : "bg-transparent"].join(" ")} />
                    </span>

                    <span className="text-left">{k.text}</span>
                  </div>

                  <div className="flex items-center gap-2">
                    <span
                      onClick={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        togglePin(k.id);
                      }}
                      className={[
                        "h-8 w-8 rounded-lg grid place-items-center",
                        active ? "hover:bg-white/10" : "hover:bg-gray-100",
                      ].join(" ")}
                      role="button"
                    >
                      <StarOff size={16} className={active ? "text-white/80" : "text-gray-300"} />
                    </span>

                    <span
                      onClick={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                        remove(k.id);
                      }}
                      className={[
                        "h-8 w-8 rounded-lg grid place-items-center",
                        active ? "hover:bg-white/10" : "hover:bg-gray-100",
                      ].join(" ")}
                      role="button"
                    >
                      <X size={16} className={active ? "text-white/80" : "text-gray-300"} />
                    </span>
                  </div>
                </button>
              );
            })}
          </div>

          {isFetching ? <div className="mt-3 text-xs text-gray-400">Refreshing...</div> : null}
        </div>
      )}
    </div>
  );
}
