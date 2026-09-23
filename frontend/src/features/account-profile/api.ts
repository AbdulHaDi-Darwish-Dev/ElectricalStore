import { authenticatedFetch } from "@/lib/auth";
import { apiFetch } from "@/lib/api";
import type {
  ChangePasswordRequest,
  ChangePasswordResponse,
  ConfirmEmailRequest,
  ConfirmEmailResponse,
  CustomerProfileDto,
  CustomerRegistrationRequest,
  CustomerRegistrationResponse,
  ForgotPasswordRequest,
  ForgotPasswordResponse,
  ResendEmailVerificationRequest,
  ResendEmailVerificationResponse,
  ResetPasswordRequest,
  ResetPasswordResponse,
  RequestEmailChangeRequest,
  RequestEmailChangeResponse,
  ConfirmEmailChangeRequest,
  ConfirmEmailChangeResponse,
  UpdateCustomerProfileRequest,
} from "./types";

export function registerCustomer(
  body: CustomerRegistrationRequest,
): Promise<CustomerRegistrationResponse> {
  return apiFetch<CustomerRegistrationResponse>("/account/register", {
    method: "POST",
    body,
    cache: "no-store",
  });
}

export function confirmCustomerEmail(
  body: ConfirmEmailRequest,
): Promise<ConfirmEmailResponse> {
  return apiFetch<ConfirmEmailResponse>("/account/email-verification/confirm", {
    method: "POST",
    body,
    cache: "no-store",
  });
}

export function resendCustomerEmailVerification(
  body: ResendEmailVerificationRequest,
): Promise<ResendEmailVerificationResponse> {
  return apiFetch<ResendEmailVerificationResponse>(
    "/account/email-verification/resend",
    {
      method: "POST",
      body,
      cache: "no-store",
    },
  );
}

export function getCustomerProfile(): Promise<CustomerProfileDto> {
  return authenticatedFetch<CustomerProfileDto>("/account/profile", {
    method: "GET",
    cache: "no-store",
  });
}

export function updateCustomerProfile(
  body: UpdateCustomerProfileRequest,
): Promise<CustomerProfileDto> {
  return authenticatedFetch<CustomerProfileDto>("/account/profile", {
    method: "PUT",
    body,
  });
}

export function changeCustomerPassword(
  body: ChangePasswordRequest,
): Promise<ChangePasswordResponse> {
  return authenticatedFetch<ChangePasswordResponse>("/account/change-password", {
    method: "POST",
    body,
  });
}

export function requestCustomerPasswordReset(
  body: ForgotPasswordRequest,
): Promise<ForgotPasswordResponse> {
  return apiFetch<ForgotPasswordResponse>("/account/password/forgot", {
    method: "POST",
    body,
    cache: "no-store",
  });
}

export function resetCustomerPassword(
  body: ResetPasswordRequest,
): Promise<ResetPasswordResponse> {
  return apiFetch<ResetPasswordResponse>("/account/password/reset", {
    method: "POST",
    body,
    cache: "no-store",
  });
}

export function requestCustomerEmailChange(
  body: RequestEmailChangeRequest,
): Promise<RequestEmailChangeResponse> {
  return authenticatedFetch<RequestEmailChangeResponse>(
    "/account/email-change/request",
    {
      method: "POST",
      body,
    },
  );
}

export function confirmCustomerEmailChange(
  body: ConfirmEmailChangeRequest,
): Promise<ConfirmEmailChangeResponse> {
  return apiFetch<ConfirmEmailChangeResponse>("/account/email-change/confirm", {
    method: "POST",
    body,
    cache: "no-store",
  });
}
