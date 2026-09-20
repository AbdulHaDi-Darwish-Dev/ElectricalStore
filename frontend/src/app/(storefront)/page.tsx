import { brand } from "@/config/brand";

export default function StorefrontHomePage() {
  return (
    <section className="space-y-4">
      <h1 className="text-2xl font-semibold tracking-tight text-foreground sm:text-3xl">
        مرحباً بك في {brand.name}
      </h1>
      <p className="max-w-2xl text-base leading-7 text-muted-foreground">
        هذه واجهة أساسية للمتجر (المرحلة F1). لم يتم ربط الكتالوج أو السلة أو الدفع
        بعد. سيتم بناء الميزات على هذه البنية لاحقاً.
      </p>
      <p className="text-sm text-muted-foreground">
        الاتجاه من اليمين إلى اليسار (RTL) واللغة العربية مفعّلان من الجذر.
      </p>
    </section>
  );
}
