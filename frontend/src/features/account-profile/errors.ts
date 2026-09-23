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
    case "Customer.PasswordPolicyFailed":
    case "Identity.PasswordRequiresDigit":
    case "Identity.PasswordRequiresLower":
    case "Identity.PasswordRequiresUpper":
    case "Identity.PasswordRequiresNonAlphanumeric":
    case "Identity.PasswordTooShort":
      return "كلمة المرور لا تستوفي متطلبات الأمان (٨ أحرف على الأقل مع حرف كبير وصغير ورقم ورمز).";
    case "Authentication.CurrentPasswordInvalid":
    case "Customer.CurrentPasswordInvalid":
      return "كلمة المرور الحالية غير صحيحة.";
    case "Customer.EmailAlreadyInUse":
      return "هذا البريد الإلكتروني مستخدم بالفعل.";
    case "Customer.EmailUnchanged":
      return "البريد الجديد مطابق للبريد الحالي.";
    case "Customer.EmailChangeDeliveryFailed":
    case "Verification.DeliveryFailed":
      return "تعذر إرسال رسالة التأكيد. حاول لاحقاً.";
    case "Customer.EmailChangeCooldownActive":
      return "يرجى الانتظار قليلاً قبل طلب تغيير آخر.";
    case "Customer.EmailChangeUnavailable":
      return "تعذر تغيير البريد لهذا الحساب.";
    case "Customer.EmailChangeLinkExpired":
      return "انتهت صلاحية رابط تأكيد تغيير البريد. اطلب رابطاً جديداً.";
    case "Customer.EmailChangeLinkUsed":
      return "هذا الرابط لم يعد صالحًا. اطلب رابطًا جديدًا.";
    case "Customer.EmailChangeLinkInvalid":
    case "Customer.EmailChangeConfirmFailed":
    case "Customer.EmailChangeRequestFailed":
      return "تعذر تأكيد تغيير البريد الإلكتروني.";
    case "Customer.EmailChangeSessionRevocationFailed":
    case "Customer.PasswordResetSessionRevocationFailed":
      return "تعذر إكمال تأمين الجلسات. حاول تسجيل الدخول مجددًا أو تواصل مع الدعم.";
    case "Authentication.EmailNotConfirmed":
      return "يجب تأكيد بريدك الإلكتروني قبل تسجيل الدخول.";
    case "Customer.PasswordResetLinkExpired":
    case "Verification.Expired":
      return "انتهت صلاحية الرابط. اطلب رابطاً جديداً.";
    case "Customer.PasswordResetLinkUsed":
    case "Verification.AlreadyConsumed":
    case "Verification.Invalidated":
      return "هذا الرابط لم يعد صالحًا. اطلب رابطًا جديدًا.";
    case "Customer.PasswordResetLinkInvalid":
    case "Customer.PasswordResetFailed":
    case "Verification.InvalidToken":
    case "Verification.InvalidCode":
    case "Verification.ChallengeNotFound":
      return "رابط إعادة التعيين غير صالح.";
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

/** Generic anti-enumeration copy for forgot-password. */
export const PASSWORD_RESET_REQUEST_GENERIC =
  "إذا كان هناك حساب مؤهل مرتبط بهذا البريد، فقد أرسلنا رابط إعادة تعيين كلمة المرور.";
