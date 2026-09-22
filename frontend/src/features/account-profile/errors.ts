export function getCustomerProfileErrorMessage(
  code: string | undefined,
  status?: number,
): string {
  switch (code) {
    case "Customer.FullNameRequired":
      return "الاسم الكامل مطلوب.";
    case "Customer.FullNameTooLong":
      return "الاسم الكامل طويل جداً.";
    case "Customer.EmailRequired":
      return "البريد الإلكتروني مطلوب.";
    case "Customer.PasswordRequired":
      return "كلمة المرور مطلوبة.";
    case "Customer.IdentityNotFound":
      return "تعذر العثور على الحساب.";
    case "Customer.ProfileCreateFailed":
    case "Customer.RegistrationCompensationFailed":
      return "تعذر إنشاء الحساب. حاول مرة أخرى.";
    case "Authentication.EmailAlreadyExists":
    case "Authentication.UserNameAlreadyExists":
    case "Identity.DuplicateEmail":
    case "DuplicateEmail":
      return "يوجد حساب بنفس البريد الإلكتروني.";
    case "Authentication.InvalidPassword":
    case "Identity.PasswordRequiresDigit":
    case "Identity.PasswordRequiresLower":
    case "Identity.PasswordRequiresUpper":
    case "Identity.PasswordRequiresNonAlphanumeric":
    case "Identity.PasswordTooShort":
      return "كلمة المرور لا تستوفي متطلبات الأمان (٨ أحرف على الأقل مع حرف كبير وصغير ورقم ورمز).";
    case "Authentication.CurrentPasswordInvalid":
      return "كلمة المرور الحالية غير صحيحة.";
    default:
      if (status === 401) {
        return "يجب تسجيل الدخول للمتابعة.";
      }
      if (status === 409) {
        return "يوجد حساب بنفس البريد الإلكتروني.";
      }
      return "تعذر إكمال العملية. حاول مرة أخرى.";
  }
}
