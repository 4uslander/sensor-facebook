import { createBrowserRouter } from "react-router-dom";
import LoginPage from "../../presentation/pages/LoginPage/LoginPage";
import RegisterPage from "../../presentation/pages/LoginPage/RegisterPage";
import DashboardPage from "../../presentation/pages/DashboardPage";
import ProductListPage from "../../presentation/pages/ProductListPage";
import KeywordListPage from "../../presentation/pages/KeywordListPage";
import CategoryListPage  from "../../presentation/pages/CategoryListPage";
import ProxyListPage  from "../../presentation/pages/ProxyGroupPage";
import JobListPage  from "../../presentation/pages/JobListPage";
import FacebookListPage  from "../../presentation/pages/AccountListPage";


export const router = createBrowserRouter([
  { path: "/", element: <DashboardPage /> },
  { path: "/login", element: <LoginPage /> },
  { path: "/register", element: <RegisterPage /> },
  { path: "/products", element: <ProductListPage /> },
  { path: "/keywords", element: <KeywordListPage /> },
  { path: "/categories", element: <CategoryListPage  /> },
  { path: "/proxy-groups", element: <ProxyListPage  /> },
  { path: "/job-queue", element: <JobListPage  /> },
  { path: "/account-manager", element: <FacebookListPage  /> },
  
]);
