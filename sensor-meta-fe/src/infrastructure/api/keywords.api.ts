//src\infrastructure\api\keywords.api.ts
import { http } from "../http/httpClient";

export type KeywordDto = {
  id: number;
  text: string;
  categoryId: number | null;
  priority: number;
  active: boolean;
  locationLat: number | null;
  locationLon: number | null;
  radiusKm: number;
  radiusPolicy: string;
  sortBy: string;
  conditions: string[] | null;
  listedTime: string;
  availability: string;
  createdAt: string;
};

export type KeywordsQuery = {
  page?: number;
  pageSize?: number;
  q?: string;
  categoryId?: number;
  active?: boolean;
  sortBy?: string;
  conditions?: string[]; // query param name: conditions=...
  listedTime?: string;
  availability?: string;
};

export type KeywordsResponse = {
  total: number;
  page: number;
  pageSize: number;
  items: KeywordDto[];
};

export async function getKeywordsApi(params: KeywordsQuery): Promise<KeywordsResponse> {
  const res = await http.get<KeywordsResponse>("/api/keywords", { params });
  return res.data;
}

export type CreateKeywordRequest = {
  text: string;
  categoryId?: number | null;

  priority?: number | null; // server default 1
  active?: boolean | null;  // server default true

  locationLat?: number | null;
  locationLon?: number | null;

  radiusKm: number;
  radiusPolicy?: "auto" | "fixed" | "platform" | string;

  sortBy?: "relevance" | "distance_asc" | "date_desc" | "price_asc" | "price_desc" | string;

  conditions?: Array<"new" | "like_new" | "good" | "fair" | string> | null;

  listedTime?: "all" | "24h" | "7d" | "30d" | string;
  availability?: "available" | "sold" | string;
};

export async function createKeywordApi(payload: CreateKeywordRequest): Promise<{ id: number }> {
  const res = await http.post<{ id: number }>("/api/keywords", payload);
  return res.data;
}