/**
 * Arabic UX messages for Category.* and Media.* ProblemDetails codes.
 * Do not surface raw English backend detail as primary copy.
 */
export function getCategoryErrorMessage(
  code: string | undefined,
  status?: number,
): string {
  if (status === 403) {
    return "ليس لديك صلاحية إدارة التصنيفات.";
  }
  if (status === 401) {
    return "انتهت الجلسة. سجّل الدخول ثم أعد المحاولة.";
  }

  switch (code) {
    case "Category.NotFound":
      return "التصنيف غير موجود.";
    case "Category.NameRequired":
      return "اسم التصنيف مطلوب.";
    case "Category.NameTooLong":
      return "اسم التصنيف أطول من الحد المسموح.";
    case "Category.DescriptionTooLong":
      return "وصف التصنيف أطول من الحد المسموح.";
    case "Category.NameAlreadyExists":
      return "يوجد تصنيف بنفس الاسم. اختر اسماً آخر.";
    case "Category.ImageNotFound":
      return "لا توجد صورة لهذا التصنيف.";
    case "Media.EmptyFile":
      return "اختر ملف صورة صالحاً.";
    case "Media.FileTooLarge":
      return "حجم الصورة أكبر من الحد المسموح (5 ميغابايت).";
    case "Media.InvalidContentType":
      return "نوع الملف غير مسموح. استخدم JPEG أو PNG أو WebP.";
    case "Media.InvalidImageContent":
      return "محتوى الملف ليس صورة صالحة.";
    case "Media.UploadFailed":
      return "تعذر حفظ الصورة. أعد المحاولة لاحقاً.";
    case "Media.NotConfigured":
      return "تخزين الصور غير مهيأ حالياً. تواصل مع المسؤول.";
    default:
      if (status === 404) return "التصنيف غير موجود.";
      if (status === 409) return "تعارض مع بيانات موجودة. راجع الاسم ثم أعد المحاولة.";
      if (status && status >= 500) {
        return "حدث خطأ غير متوقع. أعد المحاولة لاحقاً.";
      }
      return "تعذر إكمال العملية. يمكنك المحاولة مرة أخرى.";
  }
}
