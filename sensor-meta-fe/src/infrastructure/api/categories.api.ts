// src/infrastructure/api/categories.api.ts
import { http } from "../http/httpClient";

export type CategoriesQuery = {
  q?: string;
  active?: boolean;
  page?: number;
  pageSize?: number;
};

export type CategoryListItemDto = {
  id: number;
  name: string;
  description: string | null;
  ownerId: string | null;
  active: boolean;
  createdAt: string; // ISO
};

export type CategoriesResponse = {
  total: number;
  page: number;
  pageSize: number;
  items: CategoryListItemDto[];
};

export async function getCategoriesApi(params: CategoriesQuery): Promise<CategoriesResponse> {
  const res = await http.get<CategoriesResponse>("/api/categories", { params });
  return res.data;
}

/* -------------------- NEW: create / update / delete / restore -------------------- */
export type CreateCategoryRequest = {
  name: string;
  description?: string | null;
};

export type UpdateCategoryRequest = {
  name?: string | null;
  description?: string | null;
  active?: boolean | null;
};

export type OkResponse = { ok: boolean };
export type CreateCategoryResponse = { id: number };

export async function createCategoryApi(payload: CreateCategoryRequest): Promise<CreateCategoryResponse> {
  const res = await http.post<CreateCategoryResponse>("/api/categories", payload);
  return res.data;
}

export async function updateCategoryApi(id: number, payload: UpdateCategoryRequest): Promise<OkResponse> {
  const res = await http.put<OkResponse>(`/api/categories/${id}`, payload);
  return res.data;
}

// soft delete
export async function deleteCategoryApi(id: number): Promise<OkResponse> {
  const res = await http.delete<OkResponse>(`/api/categories/${id}`);
  return res.data;
}

export async function restoreCategoryApi(id: number): Promise<OkResponse> {
  const res = await http.post<OkResponse>(`/api/categories/${id}/restore`, {});
  return res.data;
}
