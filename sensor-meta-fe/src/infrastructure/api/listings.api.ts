import { http } from "../http/httpClient";

export type ListingsQuery = {
  keywordId?: number;
  q?: string;
  active?: boolean;
  from?: string;     // ISO string
  to?: string;       // ISO string
  page?: number;
  pageSize?: number;
};

export type ListingListItemDto = {
  id: string;                 // Guid
  title: string | null;
  price: number | null;       // decimal? -> number
  currency: string | null;
  location: string | null;
  isActive: boolean;
  firstSeen: string;          // DateTimeOffset -> ISO
  lastSeen: string;           // DateTimeOffset -> ISO
  link: string | null;        // ✅ NEW
};

export type ListingsResponse = {
  total: number;
  page: number;
  pageSize: number;
  items: ListingListItemDto[];
};

export async function getListingsApi(params: ListingsQuery): Promise<ListingsResponse> {
  const res = await http.get<ListingsResponse>("/api/listings", { params });
  return res.data;
}
