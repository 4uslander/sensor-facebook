//src\presentation\hooks\useKeywordsQuery.ts
import { useQuery, type UseQueryResult } from "@tanstack/react-query";
import { getKeywordsApi, type KeywordsQuery, type KeywordsResponse } from "../../infrastructure/api/keywords.api";

export function useKeywordsQuery(params: KeywordsQuery): UseQueryResult<KeywordsResponse, Error> {
  return useQuery<KeywordsResponse>({
    queryKey: ["keywords", params],
    queryFn: () => getKeywordsApi(params),
    staleTime: 10_000,
    // placeholderData: keepPreviousData,
  });
}
