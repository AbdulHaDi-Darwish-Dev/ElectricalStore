/**
 * Arabic UX for Ordering.* ProblemDetails from Admin order operations.
 */
export function getAdminOrderErrorMessage(
  code: string | undefined,
  status?: number,
): string {
  if (status === 403) {
    return "ليس لديك صلاحية لهذه العملية على الطلبات.";
  }
  if (status === 401) {
    return "انتهت الجلسة. سجّل الدخول ثم أعد المحاولة.";
  }

  switch (code) {
    case "Ordering.NotFound":
      return "الطلب غير موجود.";
    case "Ordering.InvalidTransition":
      return "هذه العملية غير مسموحة لحالة الطلب الحالية.";
    case "Ordering.AlreadyPaid":
      return "الطلب مُعلَّم كمدفوع مسبقاً.";
    case "Ordering.ConfirmationStockConflict":
      return "المخزون غير كافٍ لتأكيد الطلب. لم يُحجز أي مخزون. راجع المخزون ثم أعد المحاولة.";
    case "Ordering.ConcurrencyConflict":
      return "تغيرت حالة الطلب بواسطة عملية أخرى. تم تحديث بيانات الطلب.";
    case "Ordering.CancellationReasonRequired":
      return "سبب الإلغاء مطلوب.";
    case "Ordering.CannotModify":
      return "يمكن تعديل الطلب فقط وهو بانتظار التأكيد.";
    case "Ordering.ActorRequired":
      return "يجب تسجيل الدخول لإتمام هذه العملية.";
    case "Ordering.Forbidden":
      return "ليس لديك صلاحية الوصول إلى هذا الطلب.";
    case "Ordering.InsufficientStock":
      return "الكمية المطلوبة تتجاوز المخزون المتاح.";
    case "Inventory.ConcurrencyConflict":
      return "تم تعديل المخزون من عملية أخرى. حدّث البيانات ثم أعد المحاولة.";
    default:
      if (status === 404) return "الطلب غير موجود.";
      if (status === 409) {
        return "تعارض في حالة الطلب أو المخزون. حدّث الصفحة ثم أعد المحاولة.";
      }
      if (status && status >= 500) {
        return "حدث خطأ غير متوقع. أعد المحاولة لاحقاً.";
      }
      return "تعذر إكمال العملية. يمكنك المحاولة مرة أخرى.";
  }
}
