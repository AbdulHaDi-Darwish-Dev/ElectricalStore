export default function AdminHomePage() {
  return (
    <section className="space-y-3">
      <h2 className="text-xl font-semibold text-foreground">ملخص مؤقت</h2>
      <p className="max-w-2xl text-sm leading-7 text-muted-foreground">
        هذه هيكلة لوحة الإدارة فقط. لم يتم تفعيل تسجيل الدخول أو الصلاحيات أو استدعاء
        واجهات الخلفية بعد. عناصر القائمة غير المفعّلة تظهر كعناصر مؤجلة.
      </p>
    </section>
  );
}
