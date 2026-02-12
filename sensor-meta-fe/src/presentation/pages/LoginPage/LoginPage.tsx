import { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useLoginMutation } from "../../../presentation/hooks/useLoginMutation";
import { Eye, EyeOff } from "lucide-react";

export default function LoginPage() {
  const nav = useNavigate();
  const m = useLoginMutation();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [show, setShow] = useState(false);

  const inputCls =
    "w-full rounded-lg bg-indigo-50/60 px-4 py-3 text-sm text-gray-900 outline-none ring-1 ring-indigo-100 focus:ring-2 focus:ring-indigo-200";
  const btnCls =
    "w-full rounded-lg bg-indigo-600 py-3 text-sm font-semibold text-white shadow-[0_12px_30px_-15px_rgba(79,70,229,0.6)] hover:bg-indigo-700 disabled:opacity-60";

  async function onSubmit() {
    try {
      await m.mutateAsync({ email, password });
      nav("/"); // hoặc /proxy-groups
    } catch (e) {
      // handled by m.error UI
    }
  }

  return (
    <div className="min-h-screen bg-white">
      <div className="px-10 py-6 text-sm font-semibold text-gray-900">Sensor Facebook</div>

      <div className="mx-auto grid max-w-6xl grid-cols-1 items-center gap-10 px-8 pb-12 lg:grid-cols-2 lg:gap-16">
        {/* LEFT */}
        <div>
          <h1 className="text-5xl font-extrabold tracking-tight text-gray-900">
            Sign in <span className="font-extrabold">to</span>
          </h1>

          <p className="mt-6 max-w-sm text-sm leading-6 text-gray-600">
            If you don't have an account register. You can{" "}
            <Link to="/register" className="font-semibold text-indigo-600 hover:underline">
              Register here!
            </Link>
          </p>

          {/* placeholder illustration */}
          <div className="mt-10 flex items-center gap-4">
            {/* <div className="flex h-10 w-10 items-center justify-center rounded-full bg-gray-50 shadow-sm">
              😎
            </div> */}
            {/* <div className="h-72 w-72 rounded-3xl bg-white shadow-[0_25px_60px_-30px_rgba(0,0,0,0.25)] ring-1 ring-gray-100 flex items-center justify-center">
              <div className="text-xs text-gray-400">3D Illustration</div>
            </div> */}
          </div>
        </div>

        {/* RIGHT */}
        <div className="flex justify-center lg:justify-end">
          <div className="w-full max-w-md">
            <h2 className="mb-6 text-2xl font-semibold text-gray-900">Sign in</h2>

            <div className="rounded-2xl bg-white p-6 shadow-[0_20px_70px_-35px_rgba(0,0,0,0.25)] ring-1 ring-gray-100">
              <div className="space-y-4">
                <input
                  className={inputCls}
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="Enter email or user name"
                />

                <div className="relative">
                  <input
                    className={inputCls}
                    type={show ? "text" : "password"}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="Password"
                  />
                  <button
                    type="button"
                    onClick={() => setShow((s) => !s)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-700"
                  >
                    {show ? <EyeOff size={18} /> : <Eye size={18} />}
                  </button>
                </div>

                <div className="text-right text-xs text-gray-400">Forgot password ?</div>

                <button className={btnCls} onClick={onSubmit} disabled={m.isPending}>
                  {m.isPending ? "Logging in..." : "Login"}
                </button>

                {m.isError ? (
                  <div className="text-sm text-red-600">
                    {(m.error as any)?.response?.data?.error ||
                      (m.error as any)?.message ||
                      "Login failed"}
                  </div>
                ) : null}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
