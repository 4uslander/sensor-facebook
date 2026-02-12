import { useEffect, useState } from "react";
import AppShell from "../layout/AppShell";
import KeywordPanel from "../components/KeywordPanel";
import ProductTableListing from "../components/ProductTableListing";
import { http } from "../../infrastructure/http/httpClient";
import { authStorage } from "../../shared/lib/authStorage";

export default function ProductListPage() {
  const [selectedKeywordId, setSelectedKeywordId] = useState<number | null>(null);

  // ✅ Force attach token to axios defaults (workaround chắc chắn)
  useEffect(() => {
    const token = authStorage.getAccessToken();
    if (token) {
      // axios v1: set default header để mọi request đều có Bearer
      (http.defaults.headers as any).common = (http.defaults.headers as any).common ?? {};
      (http.defaults.headers as any).common["Authorization"] = `Bearer ${token}`;
    } else {
      // nếu không có token thì xoá default auth
      (http.defaults.headers as any).common = (http.defaults.headers as any).common ?? {};
      delete (http.defaults.headers as any).common["Authorization"];
    }
  }, []);

  return (
    <AppShell>
      <h1 className="text-2xl font-extrabold text-gray-900 mb-4">Product Listing</h1>

      <div className="grid grid-cols-12 gap-6">
        {/* Left keyword panel */}
        <div className="col-span-12 lg:col-span-3">
          <KeywordPanel
            selectedId={selectedKeywordId}
            onSelect={(id) => setSelectedKeywordId(id)}
          />
        </div>

        {/* Right table */}
        <div className="col-span-12 lg:col-span-9">
          <ProductTableListing keywordId={selectedKeywordId} />
        </div>
      </div>
    </AppShell>
  );
}
