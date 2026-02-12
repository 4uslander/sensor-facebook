// src/presentation/hooks/useCategoryMutations.ts
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  createCategoryApi,
  updateCategoryApi,
  deleteCategoryApi,
  restoreCategoryApi,
  type CreateCategoryRequest,
  type UpdateCategoryRequest,
} from "../../infrastructure/api/categories.api";

export function useCreateCategoryMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateCategoryRequest) => createCategoryApi(payload),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["categories"] });
    },
  });
}

export function useUpdateCategoryMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (args: { id: number; payload: UpdateCategoryRequest }) =>
      updateCategoryApi(args.id, args.payload),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["categories"] });
    },
  });
}

export function useDeleteCategoryMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deleteCategoryApi(id),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["categories"] });
    },
  });
}

export function useRestoreCategoryMutation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => restoreCategoryApi(id),
    onSuccess: async () => {
      await qc.invalidateQueries({ queryKey: ["categories"] });
    },
  });
}
