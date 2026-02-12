//src\presentation\hooks\useCategoriesQuery.ts
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { getCategoriesApi, type CategoriesQuery, type CategoriesResponse } from "../../infrastructure/api/categories.api";

export function useCategoriesQuery(params: CategoriesQuery) {
  return useQuery<CategoriesResponse>({
    queryKey: ["categories", params],
    queryFn: () => getCategoriesApi(params),
    staleTime: 30_000,
    placeholderData: keepPreviousData,
  });
}
