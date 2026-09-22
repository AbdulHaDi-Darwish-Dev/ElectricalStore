import { authenticatedFetch } from "@/lib/auth";
import { apiFetch } from "@/lib/api";
import type {
  ChangePasswordRequest,
  ChangePasswordResponse,
  CustomerProfileDto,
  CustomerRegistrationRequest,
  CustomerRegistrationResponse,
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
    cache: "no-store",
  });
}

export function changeCustomerPassword(
  body: ChangePasswordRequest,
): Promise<ChangePasswordResponse> {
  return authenticatedFetch<ChangePasswordResponse>("/account/change-password", {
    method: "POST",
    body,
    cache: "no-store",
  });
}
