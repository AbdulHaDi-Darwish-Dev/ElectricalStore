/**
 * Arabic UX for Shipping.* ProblemDetails codes.
 * Do not surface raw English backend detail as primary copy.
 */
export function getShippingErrorMessage(
  code: string | undefined,
  status?: number,
): string {
  if (status === 403) {
    return "ليس لديك صلاحية إدارة مناطق الشحن.";
  }
  if (status === 401) {
    return "انتهت الجلسة. سجّل الدخول ثم أعد المحاولة.";
  }

  switch (code) {
    case "Shipping.NotFound":
      return "منطقة الشحن غير موجودة.";
    case "Shipping.NameRequired":
      return "اسم منطقة الشحن مطلوب.";
    case "Shipping.NameTooLong":
      return "اسم منطقة الشحن أطول من الحد المسموح.";
    case "Shipping.NegativeFee":
      return "رسوم الشحن لا يمكن أن تكون سالبة.";
    case "Shipping.NameAlreadyExists":
      return "توجد منطقة بنفس الاسم. اختر اسماً آخر.";
    default:
      if (status === 404) return "منطقة الشحن غير موجودة.";
      if (status === 409) {
        return "تعارض مع بيانات موجودة. راجع الاسم ثم أعد المحاولة.";
      }
      if (status && status >= 500) {
        return "حدث خطأ غير متوقع. أعد المحاولة لاحقاً.";
      }
      return "تعذر إكمال العملية. يمكنك المحاولة مرة أخرى.";
  }
}
