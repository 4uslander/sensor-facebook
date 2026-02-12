import { Link } from "react-router-dom";

export function AuthLayout({
  titleBold,
  titleThin,
  subtitle,
  linkText,
  linkTo,
  linkLabel,
  children,
}: {
  titleBold: string;
  titleThin: string;
  subtitle: string;
  linkText: string;
  linkTo: string;
  linkLabel: string;
  children: React.ReactNode;
}) {
  return (
    <div className="min-h-screen bg-white">
      <div className="px-10 py-6 text-sm font-semibold text-gray-800">Your Logo</div>

      <div className="mx-auto grid max-w-6xl grid-cols-1 gap-10 px-8 py-6 lg:grid-cols-2 lg:py-16">
        <div className="flex items-center">
          <div className="max-w-lg">
            <h1 className="text-5xl font-extrabold tracking-tight text-gray-900">
              {titleBold} <span className="font-extrabold">{titleThin}</span>
            </h1>

            <p className="mt-6 text-sm text-gray-600">
              {subtitle}{" "}
              <Link to={linkTo} className="font-semibold text-indigo-600 hover:underline">
                {linkText}
              </Link>{" "}
              {linkLabel}
            </p>

            {/* Placeholder minh họa (bạn thay bằng ảnh/3D) */}
            <div className="mt-10 h-64 w-64 rounded-2xl bg-gray-50" />
          </div>
        </div>

        <div className="flex items-center justify-center">
          {children}
        </div>
      </div>
    </div>
  );
}
