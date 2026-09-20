/**
 * Arabic UX for Authorization.* / IAM ProblemDetails codes.
 */
export function getIamErrorMessage(
  code: string | undefined,
  status?: number,
): string {
  if (status === 403) {
    return "ليس لديك صلاحية لهذه العملية، أو تتعارض مع قواعد التسلسل الإداري.";
  }
  if (status === 401) {
    return "انتهت الجلسة. سجّل الدخول ثم أعد المحاولة.";
  }

  switch (code) {
    case "Authorization.UserNotFound":
      return "المستخدم غير موجود أو خارج نطاق إدارتك.";
    case "Authorization.RoleNotFound":
      return "الدور غير موجود أو خارج نطاق إدارتك.";
    case "Authorization.PermissionNotFound":
      return "الصلاحية غير موجودة.";
    case "Authorization.HierarchyViolation":
      return "لا يمكن إدارة دور أو مستخدم في مستوى مساوٍ أو أعلى من مستواك.";
    case "Authorization.CannotManageSelf":
      return "لا يمكن تعديل إعدادات الهوية الخاصة بحسابك بهذه الطريقة.";
    case "Authorization.OwnerProtected":
      return "دور المالك محمي ولا يمكن تعديله أو حذفه.";
    case "Authorization.RoleHasUsers":
      return "لا يمكن حذف الدور لأنه مرتبط بمستخدمين.";
    case "Authorization.RoleHasPermissions":
      return "لا يمكن حذف الدور لأنه يملك صلاحيات مرتبطة. أزل الصلاحيات أولاً.";
    case "Authorization.RoleAlreadyExists":
      return "يوجد دور بنفس الاسم.";
    case "Authorization.InvalidRoleName":
      return "اسم الدور غير صالح.";
    case "Authorization.InvalidRolePlacement":
      return "موضع الدور غير صالح. اختر دوراً مرجعياً وموضعاً (فوق/تحت/نفس المستوى).";
    case "Authorization.InvalidRoleUpdate":
      return "قدّم اسماً و/أو موضع الدور للتحديث.";
    case "Authorization.InvalidOverrideEffect":
      return "تأثير الاستثناء يجب أن يكون Allow أو Deny.";
    case "Authorization.OverrideNotFound":
      return "استثناء الصلاحية غير موجود.";
    case "Authorization.OverrideUnchanged":
      return "لم يتغيّر استثناء الصلاحية.";
    case "Authorization.HierarchyLevelSpaceExhausted":
      return "لا تتوفر مساحة كافية في مستويات الأدوار لهذا الموضع.";
    case "Authorization.InvalidPaging":
      return "معاملات الصفحات غير صالحة.";
    case "Authorization.MissingManagePermission":
      return "تنقصك صلاحية الإدارة المطلوبة لهذه العملية.";
    default:
      if (status === 404) return "المورد غير موجود.";
      if (status === 409) {
        return "تعارض في حالة الهوية. حدّث الصفحة ثم أعد المحاولة.";
      }
      if (status && status >= 500) {
        return "حدث خطأ غير متوقع. أعد المحاولة لاحقاً.";
      }
      return "تعذر إكمال العملية. يمكنك المحاولة مرة أخرى.";
  }
}
