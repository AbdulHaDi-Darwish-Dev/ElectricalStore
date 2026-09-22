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
