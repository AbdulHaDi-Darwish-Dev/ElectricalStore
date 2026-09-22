export type CustomerProfileDto = {
  userId: string;
  fullName: string;
  email: string;
  emailConfirmed: boolean;
};

export type CustomerRegistrationRequest = {
  fullName: string;
  email: string;
  password: string;
};

export type CustomerRegistrationResponse = {
  userId: string;
  fullName: string;
  email: string;
  emailVerificationRequired: boolean;
  verificationEmailSent: boolean;
};

export type UpdateCustomerProfileRequest = {
  fullName: string;
};

export type ChangePasswordRequest = {
  currentPassword: string;
  newPassword: string;
};

export type ChangePasswordResponse = {
  reauthenticationRequired: boolean;
};

export type ConfirmEmailRequest = {
  challengeId: string;
  token: string;
};

export type ConfirmEmailResponse = {
  confirmed: boolean;
  alreadyConfirmed?: boolean;
};

export type ResendEmailVerificationRequest = {
  email: string;
};

export type ResendEmailVerificationResponse = {
  message: string;
};
