/**
 * Arabic UX for Product.* and Media.* ProblemDetails codes.
 * Do not surface raw English backend detail as primary copy.
 */
export function getProductErrorMessage(
  code: string | undefined,
  status?: number,
): string {
  if (status === 403) {
    return "ليس لديك صلاحية إدارة المنتجات.";
  }
  if (status === 401) {
    return "انتهت الجلسة. سجّل الدخول ثم أعد المحاولة.";
  }

  switch (code) {
    case "Product.NotFound":
      return "المنتج غير موجود.";
    case "Product.VariantNotFound":
      return "خيار المنتج غير موجود.";
    case "Product.CategoryNotFound":
      return "التصنيف المحدد غير موجود.";
    case "Product.NameRequired":
      return "اسم المنتج مطلوب.";
    case "Product.NameTooLong":
      return "اسم المنتج أطول من الحد المسموح.";
    case "Product.DescriptionTooLong":
      return "وصف المنتج أطول من الحد المسموح.";
    case "Product.CategoryRequired":
      return "يجب اختيار تصنيف.";
    case "Product.VariantsRequired":
      return "يجب إضافة خيار واحد على الأقل عند إنشاء المنتج.";
    case "Product.DuplicateVariantName":
      return "أسماء الخيارات يجب أن تكون فريدة داخل المنتج.";
    case "Product.DuplicateSkuInRequest":
      return "لا يمكن تكرار رمز SKU في نفس الطلب.";
    case "Product.SkuAlreadyExists":
      return "رمز SKU مستخدم مسبقاً. اختر رمزاً آخر.";
    case "Product.VariantNameRequired":
      return "اسم الخيار مطلوب.";
    case "Product.VariantNameTooLong":
      return "اسم الخيار أطول من الحد المسموح.";
    case "Product.SkuRequired":
      return "رمز SKU مطلوب.";
    case "Product.SkuTooLong":
      return "رمز SKU أطول من الحد المسموح.";
    case "Product.InvalidPrice":
      return "السعر يجب أن يكون أكبر من صفر.";
    case "Product.InvalidQuantityIncrement":
      return "خطوة الكمية غير صالحة لوحدة البيع المحددة (للقطعة يجب أن تكون 1).";
    case "Product.InvalidSellingUnit":
      return "وحدة البيع غير صالحة. استخدم قطعة أو متر.";
    case "Product.TooManyImages":
      return "الحد الأقصى لصور المنتج هو 4 صور.";
    case "Product.ImageNotFound":
      return "صورة المنتج غير موجودة.";
    case "Product.InvalidImageReorder":
      return "ترتيب الصور غير صالح. يجب تضمين كل الصور مرة واحدة.";
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
      if (status === 404) return "المنتج غير موجود.";
      if (status === 409) {
        return "تعارض مع بيانات موجودة. راجع SKU أو الأسماء ثم أعد المحاولة.";
      }
      if (status && status >= 500) {
        return "حدث خطأ غير متوقع. أعد المحاولة لاحقاً.";
      }
      return "تعذر إكمال العملية. يمكنك المحاولة مرة أخرى.";
  }
}
