// src/presentation/components/AddKeywordModal.tsx
import { useEffect, useMemo, useRef, useState } from "react";

function cn(...s: Array<string | false | null | undefined>) {
  return s.filter(Boolean).join(" ");
}

function toNumOrNull(v: string): number | null {
  const t = v.trim();
  if (!t) return null;
  const n = Number(t);
  return Number.isFinite(n) ? n : null;
}

export type AddKeywordPayload = {
  text: string;
  categoryId: number | null;
  priority: number | null;          // backend: int? default 1
  active: boolean;

  locationLat: number | null;
  locationLon: number | null;

  radiusKm: number;
  radiusPolicy: "platform" | "auto" | "fixed";

  sortBy: "relevance" | "distance_asc" | "date_desc" | "price_asc" | "price_desc";

  conditions: Array<"new" | "like_new" | "good" | "fair">;

  listedTime: "all" | "24h" | "7d" | "30d";
  availability: "available" | "sold";
};


type CategoryOption = { id: number; name: string };

type Props = {
  open: boolean;
  onClose: () => void;
  onSubmit: (payload: AddKeywordPayload) => void;

  // optional: nếu có categories thật thì truyền vào, không thì modal vẫn chạy được
  categories?: CategoryOption[];
};

const RADIUS_OPTIONS = [1, 2, 5, 10, 20, 40, 60, 65, 80, 100, 250, 500];

const CONDITION_OPTIONS: Array<{ key: "new" | "like_new" | "good" | "fair"; label: string }> = [
  { key: "new", label: "New" },
  { key: "like_new", label: "Used – like new" },
  { key: "good", label: "Used – good" },
  { key: "fair", label: "Used – fair" },
];

const LISTED_TIME_OPTIONS: Array<{ key: "all" | "24h" | "7d" | "30d"; label: string }> = [
  { key: "all", label: "All" },
  { key: "24h", label: "Last 24 hours" },
  { key: "7d", label: "Last 7 days" },
  { key: "30d", label: "Last 30 days" },
];

const AVAILABILITY_OPTIONS: Array<{ key: "available" | "sold"; label: string }> = [
  { key: "available", label: "Available" },
  { key: "sold", label: "Sold" },
];

const SORTBY_OPTIONS: Array<{ key: string; label: string }> = [
  { key: "relevance", label: "Relevance" },
  { key: "distance_asc", label: "Distance (Nearest)" },
  { key: "date_desc", label: "Date listed (Newest)" },
  { key: "price_asc", label: "Price (Low → High)" },
  { key: "price_desc", label: "Price (High → Low)" },
];

const POLICY_OPTIONS: Array<{ key: "platform" | "auto" | "fixed"; label: string }> = [
  { key: "platform", label: "Platform" },
  { key: "auto", label: "Auto" },
  { key: "fixed", label: "Fixed" },
];

export default function AddKeywordModal({ open, onClose, onSubmit, categories }: Props) {
  const panelRef = useRef<HTMLDivElement>(null);

  const [text, setText] = useState("");

  const [categoryId, setCategoryId] = useState<string>(""); // empty => null
  const [priority, setPriority] = useState<string>("1"); // server default 1
  const [active, setActive] = useState<string>("true"); // select: true/false

  const [locationLat, setLocationLat] = useState<string>("");
  const [locationLon, setLocationLon] = useState<string>("");

  const [radiusKm, setRadiusKm] = useState<string>("65");
  const [radiusPolicy, setRadiusPolicy] = useState<string>("platform");
  const [sortBy, setSortBy] = useState<string>("relevance");

  const [conditions, setConditions] = useState<Array<"new" | "like_new" | "good" | "fair">>([]);
  const [listedTime, setListedTime] = useState<"all" | "24h" | "7d" | "30d">("all");
  const [availability, setAvailability] = useState<"available" | "sold">("available");

  const [touched, setTouched] = useState(false);

  const categoryOptions = useMemo<CategoryOption[]>(() => {
    if (categories?.length) return categories;
    // fallback (không phá UI nếu chưa có API category)
    return [
      { id: 1, name: "Speaker" },
      { id: 2, name: "Amplifier" },
      { id: 3, name: "Vintage" },
    ];
  }, [categories]);

  useEffect(() => {
    if (!open) return;

    // reset mỗi lần mở
    setText("");
    setCategoryId("");
    setPriority("1");
    setActive("true");
    setLocationLat("");
    setLocationLon("");
    setRadiusKm("65");
    setRadiusPolicy("platform");
    setSortBy("relevance");
    setConditions([]);
    setListedTime("all");
    setAvailability("available");
    setTouched(false);

    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [open, onClose]);

  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      const target = e.target as Node;
      if (panelRef.current && !panelRef.current.contains(target)) onClose();
    };
    document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, [open, onClose]);

  if (!open) return null;

  const latNum = toNumOrNull(locationLat);
  const lonNum = toNumOrNull(locationLon);

  const hasLat = latNum !== null;
  const hasLon = lonNum !== null;

  const validText = text.trim().length >= 2;
  const validRadius = Number(radiusKm) > 0;

  // server rule: lat/lon phải đi cùng nhau (hoặc cả 2 null)
  const validLatLonPair = (hasLat && hasLon) || (!hasLat && !hasLon);

  // range check theo server
  const validLat = !hasLat || (latNum! >= -90 && latNum! <= 90);
  const validLon = !hasLon || (lonNum! >= -180 && lonNum! <= 180);

  const isValid = validText && validRadius && validLatLonPair && validLat && validLon;

  function toggleCondition(key: "new" | "like_new" | "good" | "fair") {
    setConditions((prev) => (prev.includes(key) ? prev.filter((x) => x !== key) : [...prev, key]));
  }

  function handleSubmit() {
    setTouched(true);
    if (!isValid) return;

    const payload: AddKeywordPayload = {
      text: text.trim(),

      categoryId: categoryId === "" ? null : Number(categoryId),

      priority: priority === "" ? null : Number(priority),
      active: active === "true",

      locationLat: hasLat ? latNum : null,
      locationLon: hasLon ? lonNum : null,

      radiusKm: Number(radiusKm),
      radiusPolicy: radiusPolicy as "platform" | "auto" | "fixed",
      sortBy: sortBy as "relevance" | "distance_asc" | "date_desc" | "price_asc" | "price_desc",

      conditions: conditions.length ? conditions : [],
      listedTime,
      availability,
    };

    onSubmit(payload);
    onClose();
  }

  return (
    <div className="fixed inset-0 z-[120] grid place-items-center px-4">
      <div className="absolute inset-0 bg-black/10" />

      <div
        ref={panelRef}
        className="relative w-[860px] max-w-[calc(100vw-24px)] rounded-2xl bg-white border border-gray-100 shadow-[0_25px_90px_-45px_rgba(0,0,0,0.55)]"
      >
        <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
          <div className="text-sm font-bold text-gray-900">Add Keyword</div>
          <button
            type="button"
            onClick={onClose}
            className="h-9 w-9 rounded-lg border border-gray-100 grid place-items-center hover:bg-gray-50"
            aria-label="Close"
          >
            <span className="text-gray-600 text-sm">×</span>
          </button>
        </div>

        <div className="p-6">
          <div className="grid grid-cols-12 gap-x-6 gap-y-4">
            {/* text */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Text *</div>
            </div>
            <div className="col-span-8">
              <input
                value={text}
                onChange={(e) => setText(e.target.value)}
                onBlur={() => setTouched(true)}
                placeholder="Enter keyword"
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
              />
              {touched && !validText ? (
                <div className="mt-1 text-[11px] text-red-500">Text tối thiểu 2 ký tự.</div>
              ) : null}
            </div>

            {/* categoryId */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Category</div>
            </div>
            <div className="col-span-8">
              <select
                value={categoryId}
                onChange={(e) => setCategoryId(e.target.value)}
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
              >
                <option value="">None</option>
                {categoryOptions.map((c) => (
                  <option key={c.id} value={String(c.id)}>
                    {c.name} (#{c.id})
                  </option>
                ))}
              </select>
            </div>

            {/* priority */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Priority</div>
            </div>
            <div className="col-span-8">
              <select
                value={priority}
                onChange={(e) => setPriority(e.target.value)}
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
              >
                <option value="1">1 (High)</option>
                <option value="2">2 (Medium)</option>
                <option value="3">3 (Low)</option>
              </select>
            </div>

            {/* active */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Active</div>
            </div>
            <div className="col-span-8">
              <select
                value={active}
                onChange={(e) => setActive(e.target.value)}
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
              >
                <option value="true">true</option>
                <option value="false">false</option>
              </select>
            </div>

            {/* locationLat */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Location Lat</div>
            </div>
            <div className="col-span-8">
              <input
                value={locationLat}
                onChange={(e) => setLocationLat(e.target.value)}
                onBlur={() => setTouched(true)}
                placeholder="e.g. 10.8231"
                inputMode="decimal"
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
              />
              {touched && !validLatLonPair ? (
                <div className="mt-1 text-[11px] text-red-500">Lat/Lon phải đi cùng nhau (cả 2 hoặc none).</div>
              ) : null}
              {touched && !validLat ? (
                <div className="mt-1 text-[11px] text-red-500">Lat phải nằm trong [-90..90].</div>
              ) : null}
            </div>

            {/* locationLon */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Location Lon</div>
            </div>
            <div className="col-span-8">
              <input
                value={locationLon}
                onChange={(e) => setLocationLon(e.target.value)}
                onBlur={() => setTouched(true)}
                placeholder="e.g. 106.6297"
                inputMode="decimal"
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
              />
              {touched && !validLon ? (
                <div className="mt-1 text-[11px] text-red-500">Lon phải nằm trong [-180..180].</div>
              ) : null}
            </div>

            {/* radiusKm (select theo hình) */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Radius (km) *</div>
            </div>
            <div className="col-span-8">
              <select
                value={radiusKm}
                onChange={(e) => setRadiusKm(e.target.value)}
                onBlur={() => setTouched(true)}
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
              >
                {RADIUS_OPTIONS.map((n) => (
                  <option key={n} value={String(n)}>
                    {n} kilometres
                  </option>
                ))}
              </select>
              {touched && !validRadius ? (
                <div className="mt-1 text-[11px] text-red-500">RadiusKm là bắt buộc.</div>
              ) : null}
            </div>

            {/* radiusPolicy */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Radius Policy</div>
            </div>
            <div className="col-span-8">
              <select
                value={radiusPolicy}
                onChange={(e) => setRadiusPolicy(e.target.value)}
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
              >
                {POLICY_OPTIONS.map((p) => (
                  <option key={p.key} value={p.key}>
                    {p.label}
                  </option>
                ))}
              </select>
            </div>

            {/* sortBy */}
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Sort By</div>
            </div>
            <div className="col-span-8">
              <select
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value)}
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
              >
                {SORTBY_OPTIONS.map((s) => (
                  <option key={s.key} value={s.key}>
                    {s.label}
                  </option>
                ))}
              </select>
            </div>

            {/* Condition (checkbox multi theo hình) */}
            <div className="col-span-4 flex items-start pt-2">
              <div className="text-xs text-gray-600">Condition</div>
            </div>
            <div className="col-span-8">
              <div className="space-y-2">
                {CONDITION_OPTIONS.map((c) => (
                  <label key={c.key} className="flex items-center justify-between gap-3 text-sm">
                    <span className="text-xs text-gray-800">{c.label}</span>
                    <input
                      type="checkbox"
                      checked={conditions.includes(c.key)}
                      onChange={() => toggleCondition(c.key)}
                      className="h-4 w-4 accent-blue-600"
                    />
                  </label>
                ))}
              </div>
            </div>

            {/* Date listed (radio theo hình) */}
            <div className="col-span-4 flex items-start pt-2">
              <div className="text-xs text-gray-600">Date listed</div>
            </div>
            <div className="col-span-8">
              <div className="space-y-2">
                {LISTED_TIME_OPTIONS.map((x) => (
                  <label key={x.key} className="flex items-center justify-between gap-3">
                    <span className="text-xs text-gray-800">{x.label}</span>
                    <input
                      type="radio"
                      name="listedTime"
                      checked={listedTime === x.key}
                      onChange={() => setListedTime(x.key)}
                      className="h-4 w-4 accent-blue-600"
                    />
                  </label>
                ))}
              </div>
            </div>

            {/* Availability (radio theo hình) */}
            <div className="col-span-4 flex items-start pt-2">
              <div className="text-xs text-gray-600">Availability</div>
            </div>
            <div className="col-span-8">
              <div className="space-y-2">
                {AVAILABILITY_OPTIONS.map((x) => (
                  <label key={x.key} className="flex items-center justify-between gap-3">
                    <span className="text-xs text-gray-800">{x.label}</span>
                    <input
                      type="radio"
                      name="availability"
                      checked={availability === x.key}
                      onChange={() => setAvailability(x.key)}
                      className="h-4 w-4 accent-blue-600"
                    />
                  </label>
                ))}
              </div>
            </div>
          </div>

          <div className="mt-6 flex justify-end gap-3">
            <button
              type="button"
              onClick={onClose}
              className="h-9 rounded-md border border-gray-200 bg-white px-4 text-xs font-semibold text-gray-700 hover:bg-gray-50"
            >
              Discard
            </button>

            <button
              type="button"
              onClick={handleSubmit}
              className={cn(
                "h-9 rounded-md px-4 text-xs font-semibold text-white",
                isValid ? "bg-blue-600 hover:bg-blue-700" : "bg-blue-300 cursor-not-allowed"
              )}
            >
              Add Keyword
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
