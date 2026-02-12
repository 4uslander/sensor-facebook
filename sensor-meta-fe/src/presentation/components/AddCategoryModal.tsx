import { useEffect, useRef, useState } from "react";

type Props = {
  open: boolean;
  onClose: () => void;
  onSubmit: (payload: { name: string; description: string }) => void;
};

function cn(...s: Array<string | false | null | undefined>) {
  return s.filter(Boolean).join(" ");
}

export default function AddCategoryModal({ open, onClose, onSubmit }: Props) {
  const panelRef = useRef<HTMLDivElement>(null);

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [touched, setTouched] = useState(false);

  useEffect(() => {
    if (!open) return;
    setName("");
    setDescription("");
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

  const validName = name.trim().length >= 2;
  const validDesc = description.trim().length >= 2;
  const isValid = validName && validDesc;

  function handleSubmit() {
    setTouched(true);
    if (!isValid) return;
    onSubmit({ name: name.trim(), description: description.trim() });
    onClose();
  }

  return (
    <div className="fixed inset-0 z-[120] grid place-items-center">
      <div className="absolute inset-0 bg-black/10" />

      <div
        ref={panelRef}
        className="relative w-[520px] max-w-[calc(100vw-40px)] rounded-2xl bg-white border border-gray-100 shadow-[0_25px_80px_-45px_rgba(0,0,0,0.45)]"
      >
        <div className="p-6">
          <div className="text-sm font-bold text-gray-900 mb-5">New Category</div>

          {/* Label + Input cùng dòng */}
          <div className="grid grid-cols-12 gap-x-6 gap-y-4">
            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Category Name</div>
            </div>
            <div className="col-span-8">
              <input
                value={name}
                onChange={(e) => setName(e.target.value)}
                onBlur={() => setTouched(true)}
                placeholder="Enter category name"
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
              />
              {touched && !validName ? (
                <div className="mt-1 text-[11px] text-red-500">
                  Category name tối thiểu 2 ký tự.
                </div>
              ) : null}
            </div>

            <div className="col-span-4 flex items-center">
              <div className="text-xs text-gray-600">Description</div>
            </div>
            <div className="col-span-8">
              <input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                onBlur={() => setTouched(true)}
                placeholder="Enter description"
                className={cn(
                  "w-full h-9 rounded-md border bg-white px-3 text-xs outline-none",
                  "border-gray-200 focus:border-blue-300 focus:ring-2 focus:ring-blue-100"
                )}
              />
              {touched && !validDesc ? (
                <div className="mt-1 text-[11px] text-red-500">
                  Description tối thiểu 2 ký tự.
                </div>
              ) : null}
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
              Add Category
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
