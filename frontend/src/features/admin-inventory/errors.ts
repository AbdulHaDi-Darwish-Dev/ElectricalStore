/**
 * Arabic UX for Inventory.* ProblemDetails codes.
 * Do not surface raw English backend detail as primary copy.
 */
export function getInventoryErrorMessage(
  code: string | undefined,
  status?: number,
): string {
  if (status === 403) {
    return "ليس لديك صلاحية لهذه العملية على المخزون.";
  }
  if (status === 401) {
    return "انتهت الجلسة. سجّل الدخول ثم أعد المحاولة.";
  }

  switch (code) {
    case "Inventory.VariantNotFound":
      return "خيار المنتج غير موجود.";
    case "Inventory.ZeroAdjustment":
      return "قيمة التعديل لا يمكن أن تكون صفراً.";
    case "Inventory.ReasonRequired":
      return "سبب التعديل مطلوب.";
    case "Inventory.ReasonTooLong":
      return "سبب التعديل أطول من الحد المسموح (500 حرفاً).";
    case "Inventory.InvalidQuantity":
      return "الكمية غير صالحة.";
    case "Inventory.OnHandWouldBeNegative":
      return "التعديل يجعل المخزون الفعلي سالباً. راجع الكمية ثم أعد المحاولة.";
    case "Inventory.OnHandBelowReserved":
      return "لا يمكن خفض المخزون الفعلي عن الكمية المحجوزة للطلبات.";
    case "Inventory.ConcurrencyConflict":
      return "تم تعديل المخزون من عملية أخرى. تم تحديث البيانات، حاول مرة أخرى.";
    case "Inventory.InsufficientAvailable":
      return "الكمية المتاحة غير كافية.";
    case "Inventory.InsufficientReserved":
      return "الكمية المحجوزة غير كافية.";
    case "Inventory.ActorRequired":
      return "يجب تسجيل الدخول لتعديل المخزون.";
    default:
      if (status === 404) return "خيار المنتج غير موجود.";
      if (status === 409) {
        return "تعارض في المخزون. حدّث البيانات ثم أعد المحاولة.";
      }
      if (status && status >= 500) {
        return "حدث خطأ غير متوقع. أعد المحاولة لاحقاً.";
      }
      return "تعذر إكمال العملية. يمكنك المحاولة مرة أخرى.";
  }
}
