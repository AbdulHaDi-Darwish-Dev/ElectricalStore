export {
  registerCustomer,
  confirmCustomerEmail,
  resendCustomerEmailVerification,
  getCustomerProfile,
  updateCustomerProfile,
  changeCustomerPassword,
} from "./api";
export { accountProfileKeys } from "./query-keys";
export {
  customerRegisterFormSchema,
  updateProfileFormSchema,
  changePasswordFormSchema,
  PASSWORD_POLICY_HINT,
  type CustomerRegisterFormValues,
  type UpdateProfileFormValues,
  type ChangePasswordFormValues,
} from "./schemas";
export {
  getCustomerProfileErrorMessage,
  EMAIL_VERIFICATION_RESEND_GENERIC,
} from "./errors";
export type {
  CustomerProfileDto,
  CustomerRegistrationRequest,
  CustomerRegistrationResponse,
  ConfirmEmailRequest,
  ConfirmEmailResponse,
  ResendEmailVerificationRequest,
  ResendEmailVerificationResponse,
  UpdateCustomerProfileRequest,
  ChangePasswordRequest,
  ChangePasswordResponse,
} from "./types";
