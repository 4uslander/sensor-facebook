import { Navigate, Outlet } from "react-router-dom";
import { authStorage } from "../lib/authStorage";

export default function RequireAuth() {
  const tokens = authStorage.get();
  if (!tokens) return <Navigate to="/login" replace />;
  return <Outlet />;
}
