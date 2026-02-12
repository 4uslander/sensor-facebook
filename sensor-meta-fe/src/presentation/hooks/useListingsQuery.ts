import { useQuery } from "@tanstack/react-query";
import { getListingsApi, type ListingsQuery } from "../../infrastructure/api/listings.api";

export function useListingsQuery(params: ListingsQuery) {
  return useQuery({
    queryKey: ["listings", params],
    queryFn: () => getListingsApi(params),
    staleTime: 10_000,
  });
}
