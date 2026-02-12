import { Eye, EyeOff } from "lucide-react";
import { useState } from "react";

export function PasswordField({
  value,
  onChange,
  placeholder,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder: string;
}) {
  const [show, setShow] = useState(false);

  return (
    <div className="relative">
      <input
        className="w-full rounded-lg bg-indigo-50/60 px-4 py-3 text-sm outline-none ring-1 ring-indigo-50 focus:ring-2 focus:ring-indigo-200"
        type={show ? "text" : "password"}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
      />
      <button
        type="button"
        onClick={() => setShow((s) => !s)}
        className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-700"
        aria-label="toggle password"
      >
        {show ? <EyeOff size={18} /> : <Eye size={18} />}
      </button>
    </div>
  );
}
