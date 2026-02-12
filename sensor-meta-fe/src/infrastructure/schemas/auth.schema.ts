import { z } from "zod";

export const RegisterResSchema = z.object({
  ok: z.boolean(),
  error: z.string().optional(),
});

export const TokenResponseSchema = z.object({
  accessToken: z.string(),
  accessExpires: z.string(),   // server trả DateTimeOffset => JSON string
  refreshToken: z.string(),
  refreshExpires: z.string(),
});
