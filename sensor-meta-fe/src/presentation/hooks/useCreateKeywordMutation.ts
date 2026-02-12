// src/presentation/hooks/useCreateKeywordMutation.ts
import { useMutation, useQueryClient, type UseMutationResult } from "@tanstack/react-query";
import { createKeywordApi, type CreateKeywordRequest } from "../../infrastructure/api/keywords.api";

export function useCreateKeywordMutation(): UseMutationResult<
  { id: number },
  Error,
  CreateKeywordRequest
> {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (payload: CreateKeywordRequest) => createKeywordApi(payload),
    onSuccess: async () => {
      // refresh keyword list
      await qc.invalidateQueries({ queryKey: ["keywords"] });
    },
  });
}
