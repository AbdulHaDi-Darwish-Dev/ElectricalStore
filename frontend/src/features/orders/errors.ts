/**
 * Arabic UX messages for Ordering.* ProblemDetails codes.
 * Do not surface raw English backend detail as primary copy.
 */
export function getOrderingErrorMessage(code: string | undefined): string {
  switch (code) {
    case "Ordering.EmptyItems":
      return "السلة فارغة. أضف منتجات قبل إتمام الطلب.";
    case "Ordering.QuantityMustBePositive":
      return "يجب أن تكون الكميات أكبر من صفر.";
    case "Ordering.DuplicateVariant":
      return "يوجد تكرار في أصناف الطلب. راجع السلة ثم أعد المحاولة.";
    case "Ordering.InvalidQuantityIncrement":
      return "كمية أحد الأصناف لا تتوافق مع وحدة البيع.";
    case "Ordering.CustomerNameRequired":
      return "الاسم مطلوب.";
    case "Ordering.PhoneRequired":
      return "رقم الهاتف مطلوب.";
    case "Ordering.AddressRequired":
      return "العنوان مطلوب.";
    case "Ordering.IdempotencyKeyRequired":
    case "Ordering.InvalidIdempotencyKey":
      return "تعذر إرسال الطلب. أعد المحاولة.";
    case "Ordering.VariantNotFound":
      return "أحد المنتجات غير متاح حالياً. راجع السلة.";
    case "Ordering.DeliveryZoneNotFound":
      return "منطقة التوصيل المحددة غير متاحة.";
    case "Ordering.NotPurchasable":
      return "أحد المنتجات لم يعد قابلاً للشراء. راجع السلة وحدّث الطلب.";
    case "Ordering.InsufficientStock":
      return "الكمية المتاحة غير كافية لأحد الأصناف. راجع السلة.";
    case "Ordering.DeliveryZoneInactive":
      return "منطقة التوصيل غير نشطة حالياً. اختر منطقة أخرى.";
    case "Ordering.BelowMinimumOrder":
      return "مجموع البضاعة أقل من الحد الأدنى للطلب.";
    case "Ordering.ConcurrencyConflict":
      return "حدث تعارض أثناء حفظ الطلب. حدّث الملخص ثم أعد المحاولة.";
    case "Ordering.IdempotencyReplayUnavailable":
      return "تعذر استرجاع نتيجة المحاولة السابقة. يمكنك إرسال طلب جديد.";
    case "Ordering.InvalidGuestToken":
    case "Ordering.NotFound":
      return "تعذر العثور على الطلب أو انتهت صلاحية الوصول.";
    default:
      return "تعذر إكمال الطلب. يمكنك المحاولة مرة أخرى.";
  }
}
