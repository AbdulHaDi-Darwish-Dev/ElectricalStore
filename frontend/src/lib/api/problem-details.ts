/**
 * Backend ProblemDetails shape (ASP.NET / Permixa).
 *
 * Reliable discriminators for the frontend:
 * - HTTP status
 * - `code` (extension; present on essentially all error paths)
 *
 * Unreliable / optional:
 * - `title` (sometimes the error code, sometimes a generic phrase)
 * - `traceId` (present on some writers, absent on business Result errors)
 * - validation `errors` dictionary (not used by this API)
 */
export type ProblemDetails = {
  status?: number;
  title?: string;
  detail?: string;
  type?: string;
  /** Primary machine-readable error discriminator when present. */
  code?: string;
  /** Optional correlation id — may be absent. */
  traceId?: string;
  [key: string]: unknown;
};

/**
 * Normalized client error for UI and logging.
 * Prefer `status` + `code` over `title`.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly detail?: string;
  readonly title?: string;
  readonly traceId?: string;
  readonly problem?: ProblemDetails;

  constructor(status: number, problem?: ProblemDetails, fallbackMessage?: string) {
    const message =
      problem?.detail ??
      problem?.code ??
      fallbackMessage ??
      `طلب فشل بحالة ${status}`;

    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = typeof problem?.code === "string" ? problem.code : undefined;
    this.detail = typeof problem?.detail === "string" ? problem.detail : undefined;
    this.title = typeof problem?.title === "string" ? problem.title : undefined;
    this.traceId =
      typeof problem?.traceId === "string" ? problem.traceId : undefined;
    this.problem = problem;
  }
}

export function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === "object" && value !== null;
}
