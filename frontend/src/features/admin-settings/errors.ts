/**
 * Arabic UX for settings-related ProblemDetails (Ordering settings).
 */
export function getSettingsErrorMessage(
  code: string | undefined,
  status?: number,
): string {
  if (status === 403) {
    return "ليس لديك صلاحية إدارة الإعدادات.";
  }
  if (status === 401) {
    return "انتهت الجلسة. سجّل الدخول ثم أعد المحاولة.";
  }

  switch (code) {
    case "Ordering.InvalidMinimumOrderAmount":
      return "الحد الأدنى لقيمة المنتجات لا يمكن أن يكون سالباً.";
    default:
      if (status === 400) {
        return "قيمة الإعداد غير صالحة. راجع الحقول ثم أعد المحاولة.";
      }
      if (status && status >= 500) {
        return "حدث خطأ غير متوقع. أعد المحاولة لاحقاً.";
      }
      return "تعذر حفظ الإعدادات. يمكنك المحاولة مرة أخرى.";
  }
}
