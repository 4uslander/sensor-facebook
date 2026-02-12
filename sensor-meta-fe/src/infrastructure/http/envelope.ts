import { z } from "zod";

export const ApiErrorSchema = z.object({
  code: z.string().optional(),
  message: z.string(),
  details: z.unknown().optional(),
});

export const ApiMetaSchema = z.record(z.string(), z.unknown()).optional();

export const EnvelopeSchema = <T extends z.ZodTypeAny>(dataSchema: T) =>
  z.object({
    data: dataSchema.nullable(),
    error: ApiErrorSchema.nullable(),
    meta: ApiMetaSchema.nullable().optional(),
  });
