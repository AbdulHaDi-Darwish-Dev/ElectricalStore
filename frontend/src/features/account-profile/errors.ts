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
      return "يوجد حساب بنفس البريد الإلكتروني. إذا سبق أن أنشأت حساباً، يمكنك إعادة إرسال رسالة التأكيد.";
    case "Authentication.InvalidPassword":
    case "Identity.PasswordRequiresDigit":
    case "Identity.PasswordRequiresLower":
    case "Identity.PasswordRequiresUpper":
    case "Identity.PasswordRequiresNonAlphanumeric":
    case "Identity.PasswordTooShort":
      return "كلمة المرور لا تستوفي متطلبات الأمان (٨ أحرف على الأقل مع حرف كبير وصغير ورقم ورمز).";
    case "Authentication.CurrentPasswordInvalid":
      return "كلمة المرور الحالية غير صحيحة.";
    case "Authentication.EmailNotConfirmed":
      return "يجب تأكيد بريدك الإلكتروني قبل تسجيل الدخول.";
    case "Verification.Expired":
      return "انتهت صلاحية رابط التأكيد. اطلب رسالة جديدة.";
    case "Verification.InvalidToken":
    case "Verification.InvalidCode":
    case "Verification.ChallengeNotFound":
    case "Verification.Invalidated":
      return "رابط التأكيد غير صالح.";
    case "Verification.DeliveryFailed":
      return "تعذر إرسال رسالة التأكيد. حاول إعادة الإرسال لاحقاً.";
    default:
      if (status === 429) {
        return "تم تجاوز حد المحاولات. يرجى الانتظار قليلاً ثم إعادة المحاولة.";
      }
      if (status === 401) {
        return "يجب تسجيل الدخول للمتابعة.";
      }
      if (status === 409) {
        return "يوجد حساب بنفس البريد الإلكتروني.";
      }
      return "تعذر إكمال العملية. حاول مرة أخرى.";
  }
}

/** Generic anti-enumeration copy for resend. */
export const EMAIL_VERIFICATION_RESEND_GENERIC =
  "إذا وُجد حساب مؤهل بهذا البريد، فسيتم إرسال رسالة التأكيد.";
