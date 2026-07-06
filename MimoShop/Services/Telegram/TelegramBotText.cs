namespace MimoShop.Services.Telegram;

public static class TelegramBotText
{
    public const string WelcomeHeroFormat =
        "📍 أهلاً بك في بوت <b>{0}</b>\n\n" +
        "🔧 تصفّح قطع الغيار المتوفرة حسب العلامة والموديل\n" +
        "📋 تتبّع حالة صيانة جهازك برمز الطلب المطبوع على الوصل\n\n" +
        "اختر من القائمة بالأسفل 👇";

    public const string BrowseParts = "🔧 تصفح قطع الغيار";
    public const string CheckRepairStatus = "📋 تتبع حالة الصيانة";
    public const string Contact = "📞 تواصل معنا";

    public const string ContactFormat =
        "📞 الهاتف: {0}\n" +
        "{1}" +
        "📍 العنوان: {2}";

    public const string ContactWhatsAppLineFormat = "💬 واتساب: {0}\n";

    public const string PriceNote =
        "💡 الأسعار تقديرية، قد يختلف السعر النهائي عند التركيب حسب فحص الجهاز.\n\n" +
        "📞 للطلب أو الاستفسار: {0}";

    public const string PleaseUseButtons =
        "⚠️ الرجاء استخدام الأزرار للتصفح.";

    public const string UseButtonsHint =
        "ℹ️ استخدم الأزرار بالأسفل للتصفح، أو أرسل /start لإعادة فتح القائمة.";

    public const string StatusPrompt =
        "📝 أرسل رمز الطلب الموجود على وصل الاستلام بصيغة REP-XXXX (مثال: REP-0042).";

    public const string StatusFormatError =
        "⚠️ صيغة الرمز غير صحيحة. يجب أن يكون الرمز <b>REP-XXXX</b> (مثال: <code>REP-0042</code>).";

    public const string StatusNotFound =
        "🔍 لم نعثر على طلب بهذا الرمز.\n\nيرجى التأكد من الرمز والمحاولة مجدداً، أو الاتصال بالمحل.";

    public const string StatusSuccessFormat =
        "━━ 📋 <b>حالة الصيانة</b> ━━\n\n📱 <b>الجهاز:</b>  {0}\n🔧 <b>الحالة:</b>    {1}\n👨‍🔧 <b>العامل:</b>    {2}";

    public const string StatusExampleCode = "REP-0042";
    public const string StatusTryAgain = "🔍 تحقق من رمز آخر";
    public const string StatusCancel = "❌ إلغاء";

    public const string BrandsHeader = "<b>اختر العلامة التجارية</b>";
    public const string BrandsTitle = "قطع الغيار";
    public const string ModelsHeader = "<b>اختر الموديل</b>";
    public const string PartTypesHeader = "<b>اختر نوع القطعة</b>";
    public const string VariantsHeader = "<b>القطع المتوفرة لهذا النوع:</b>";

    public const string VariantInlineItemFormat = "✓ {0} — 💰 {1:N0} د.ج";

    public const string EmptyState = "📭 لا توجد عناصر حالياً.";
    public const string NoStockedParts = "📭 لا توجد قطع متاحة لهذه التصفية حالياً.";

    public const string Home = "🏠 الرئيسية";
    public const string BackToBrands = "🔙 العلامات";
    public const string BackToModels = "🔙 الموديلات";
    public const string BackToTypes = "🔙 الأنواع";

    public const string Previous = "◀️ السابق";
    public const string Next = "التالي ▶️";

    public const string CurrencySuffix = " د.ج";

    public const string BotNotConfigured =
        "⚠️ بوت التيليجرام غير مهيأ. يرجى مراجعة مدير النظام.";

    public const string Separator = "━━━━━━━━━━━━━━━━";

    public const string BreadcrumbSeparator = " › ";
}