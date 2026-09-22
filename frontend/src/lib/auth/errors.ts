/**
 * Arabic UX messages for authentication ProblemDetails / status codes.
 */

export function getAuthErrorMessage(
  code: string | undefined,
  status?: number,
): string {
  if (status === 429) {
    return "تم تجاوز حد محاولات تسجيل الدخول. يرجى الانتظار قليلاً ثم إعادة المحاولة.";
  }

  switch (code) {
    case "Auth.InvalidCredentials":
    case "Authentication.InvalidCredentials":
    case "Identity.InvalidCredentials":
    case "InvalidCredentials":
      return "بيانات الدخول غير صحيحة.";
    case "Auth.UserLocked":
    case "Identity.LockedOut":
      return "تم قفل الحساب مؤقتاً. حاول لاحقاً.";
    case "Authentication.InvalidPassword":
    case "Identity.PasswordRequiresDigit":
    case "Identity.PasswordRequiresLower":
    case "Identity.PasswordRequiresUpper":
    case "Identity.PasswordRequiresNonAlphanumeric":
    case "Identity.PasswordTooShort":
      return "كلمة المرور لا تستوفي متطلبات الأمان (٨ أحرف على الأقل مع حرف كبير وصغير ورقم ورمز).";
    case "Authentication.EmailAlreadyExists":
    case "Authentication.UserNameAlreadyExists":
    case "Conflict":
    case "Identity.DuplicateEmail":
    case "Identity.DuplicateUserName":
    case "DuplicateEmail":
    case "DuplicateUserName":
      return "يوجد حساب بنفس البريد أو اسم المستخدم.";
    case "Authentication.EmailNotConfirmed":
      return "يجب تأكيد بريدك الإلكتروني قبل تسجيل الدخول.";
    case "Auth.RefreshFailed":
    case "Auth.NoRefreshSession":
      return "انتهت الجلسة. يرجى تسجيل الدخول مجدداً.";
    case "Validation":
    case "InvalidRequest":
      return "البيانات المدخلة غير صالحة.";
    default:
      if (status === 401) {
        return "بيانات الدخول غير صحيحة.";
      }
      if (status === 409) {
        return "يوجد حساب بنفس البريد أو اسم المستخدم.";
      }
      return "تعذر إكمال العملية. حاول مرة أخرى.";
  }
}

export const MFA_UNAVAILABLE_MESSAGE =
  "يلزم تحقق إضافي (مصادقة متعددة العوامل)، لكن إتمام هذا التحقق غير متاح حالياً عبر واجهة المتجر. تواصل مع الدعم إن كنت بحاجة للمساعدة.";
