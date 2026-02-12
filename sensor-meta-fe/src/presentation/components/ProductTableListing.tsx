import { ChevronLeft, ChevronRight } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useListingsQuery } from "../hooks/useListingsQuery";
import type { ListingListItemDto } from "../../infrastructure/api/listings.api";

type Props = {
  keywordId: number | null;
};

function fmtDateTime(iso: string) {
  const d = new Date(iso);
  const dd = String(d.getDate()).padStart(2, "0");
  const mm = String(d.getMonth() + 1).padStart(2, "0");
  const yyyy = d.getFullYear();
  let hh = d.getHours();
  const min = String(d.getMinutes()).padStart(2, "0");
  const ampm = hh >= 12 ? "PM" : "AM";
  hh = hh % 12;
  if (hh === 0) hh = 12;
  return `${dd}.${mm}.${yyyy} - ${hh}.${min} ${ampm}`;
}

function fmtPrice(price: number | null, currency: string | null) {
  if (price == null) return "-";
  const cur = (currency || "USD").toUpperCase();
  try {
    return new Intl.NumberFormat("en-US", { style: "currency", currency: cur }).format(price);
  } catch {
    return `${price} ${cur}`;
  }
}

export default function ProductTableListing({ keywordId }: Props) {
  const [page, setPage] = useState(1);
  const pageSize = 12;

  useEffect(() => {
    setPage(1);
  }, [keywordId]);

  const { data, isLoading, isError, error, isFetching } = useListingsQuery({
    keywordId: keywordId ?? undefined,
    page,
    pageSize,
  });

  const items: ListingListItemDto[] = useMemo(() => data?.items ?? [], [data]);

  const total = data?.total ?? 0;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  const fromIdx = total === 0 ? 0 : (page - 1) * pageSize + 1;
  const toIdx = Math.min(page * pageSize, total);

  return (
    <div className="rounded-2xl bg-white border border-gray-100 shadow-[0_20px_70px_-45px_rgba(0,0,0,0.25)] overflow-hidden">
      <div className="px-6 py-4 border-b border-gray-100">
        <div className="flex items-center justify-between">
          <div className="grid grid-cols-12 text-xs font-semibold text-gray-500 w-full">
            {/* <div className="col-span-2">Image</div> */}
            <div className="col-span-5">Product Name</div>
            <div className="col-span-2">Location</div>
            <div className="col-span-2">Price</div>
            <div className="col-span-1">Link</div>
            <div className="col-span-2">Date Time</div>
          </div>
          {isFetching ? <div className="ml-3 text-xs text-gray-400">Loading</div> : null}
        </div>
      </div>

      {keywordId == null ? (
        <div className="px-6 py-10 text-sm text-gray-500">Select a keyword to view products.</div>
      ) : isLoading ? (
        <div className="px-6 py-10 text-sm text-gray-500">Loading...</div>
      ) : isError ? (
        <div className="px-6 py-10 text-sm text-red-600">
          {(error as any)?.response?.data?.error || (error as any)?.message || "Failed to load listings"}
        </div>
      ) : items.length === 0 ? (
        <div className="px-6 py-10 text-sm text-gray-500">No products for this keyword.</div>
      ) : (
        <div className="divide-y divide-gray-100">
          {items.map((it) => (
            <div key={it.id} className="px-6 py-5">
              <div className="grid grid-cols-12 items-center">
                {/* <div className="col-span-2">
                  <div className="h-12 w-12 rounded-xl overflow-hidden bg-gray-100">
                    <div className="h-full w-full bg-gray-200" />
                  </div>
                </div> */}

                <div className="col-span-5 text-sm font-medium text-gray-900 line-clamp-1">
                  {it.title || "(No title)"}
                </div>

                <div className="col-span-2 text-sm text-gray-600 line-clamp-1">{it.location || "-"}</div>

                <div className="col-span-2 text-sm text-gray-700">{fmtPrice(it.price, it.currency)}</div>

                <div className="col-span-1 text-sm">
                  {it.link ? (
                    <a
                      href={it.link}
                      target="_blank"
                      rel="noreferrer"
                      className="text-blue-600 hover:underline"
                    >
                      Open
                    </a>
                  ) : (
                    <span className="text-gray-400">-</span>
                  )}
                </div>

                <div className="col-span-2 text-sm text-gray-600">
                  {fmtDateTime(it.lastSeen || it.firstSeen)}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <div className="px-6 py-4 border-t border-gray-100 flex items-center justify-between text-xs text-gray-500">
        <div>
          {total === 0 ? "Showing 0-0 of 0" : `Showing ${fromIdx}-${toIdx} of ${total.toLocaleString()}`}
        </div>

        <div className="flex items-center gap-2">
          <button
            className="h-8 w-8 rounded-lg border border-gray-100 bg-white grid place-items-center hover:bg-gray-50 disabled:opacity-50"
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            <ChevronLeft size={16} className="text-gray-500" />
          </button>
          <button
            className="h-8 w-8 rounded-lg border border-gray-100 bg-white grid place-items-center hover:bg-gray-50 disabled:opacity-50"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
          >
            <ChevronRight size={16} className="text-gray-500" />
          </button>
        </div>
      </div>
    </div>
  );
}
