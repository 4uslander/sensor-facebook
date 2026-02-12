export type ApiError = {
  code?: string;
  message: string;
  details?: unknown;
};

export type ApiMeta = {
  page?: number;
  pageSize?: number;
  total?: number;
  [k: string]: unknown;
};

export type Envelope<T> = {
  data: T | null;
  error: ApiError | null;
  meta?: ApiMeta | null;
};
