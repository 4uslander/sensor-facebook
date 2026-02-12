import { useMutation } from "@tanstack/react-query";
import { loginApi, registerApi, type LoginRequest, type RegisterRequest } from "../../infrastructure/api/auth.api";
import { authStorage, type TokenPayload } from "../../shared/lib/authStorage";

export function useLoginMutation() {
  return useMutation<TokenPayload, any, LoginRequest>({
    mutationFn: (payload) => loginApi(payload),
    onSuccess: (data) => authStorage.set(data),
  });
}

export function useRegisterMutation() {
  return useMutation<{ ok: boolean; error?: string }, any, RegisterRequest>({
    mutationFn: (payload) => registerApi(payload),
  });
}
