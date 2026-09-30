using MimoShop.Models;

namespace MimoShop.Data;

// Static seed data for the 58 Algerian wilayas (administrative provinces).
//
// Codes 01–58 are the official numeric codes. Each wilaya row is seeded with
// ShippingFee = 0 and IsActive = true. The owner adjusts shipping fees via the
// Delivery Zones admin (SHOP.md §7). Commune seed starts empty — the owner
// supplies a commune dataset later, surfaced via the same admin.
internal static class WilayaCommuneSeed
{
    public static IReadOnlyList<Wilaya> Wilayas() => new[]
    {
        W(1, "01", "أدرار", "Adrar"),
        W(2, "02", "الشلف", "Chlef"),
        W(3, "03", "الأغواط", "Laghouat"),
        W(4, "04", "أم البواقي", "Oum El Bouaghi"),
        W(5, "05", "باتنة", "Batna"),
        W(6, "06", "بجاية", "Béjaïa"),
        W(7, "07", "بسكرة", "Biskra"),
        W(8, "08", "بشار", "Béchar"),
        W(9, "09", "البليدة", "Blida"),
        W(10, "10", "البويرة", "Bouira"),
        W(11, "11", "تمنراست", "Tamanrasset"),
        W(12, "12", "تبسة", "Tébessa"),
        W(13, "13", "تلمسان", "Tlemcen"),
        W(14, "14", "تيارت", "Tiaret"),
        W(15, "15", "تيزي وزو", "Tizi Ouzou"),
        W(16, "16", "الجزائر", "Alger"),
        W(17, "17", "الجلفة", "Djelfa"),
        W(18, "18", "جيجل", "Jijel"),
        W(19, "19", "سطيف", "Sétif"),
        W(20, "20", "سعيدة", "Saïda"),
        W(21, "21", "سكيكدا", "Skikda"),
        W(22, "22", "سيدي بلعباس", "Sidi Bel Abbès"),
        W(23, "23", "عنابة", "Annaba"),
        W(24, "24", "قالمة", "Guelma"),
        W(25, "25", "قسنطينة", "Constantine"),
        W(26, "26", "المدية", "Médéa"),
        W(27, "27", "مستغانم", "Mostaganem"),
        W(28, "28", "المسيلة", "M'Sila"),
        W(29, "29", "معسكر", "Mascara"),
        W(30, "30", "ورقلة", "Ouargla"),
        W(31, "31", "وهران", "Oran"),
        W(32, "32", "البيض", "El Bayadh"),
        W(33, "33", "إليزي", "Illizi"),
        W(34, "34", "برج بوعريريج", "Bordj Bou Arréridj"),
        W(35, "35", "بومرداس", "Boumerdès"),
        W(36, "36", "الطريف", "El Tarf"),
        W(37, "37", "تندوف", "Tindouf"),
        W(38, "38", "تيسمسيلت", "Tissemsilt"),
        W(39, "39", "الوادي", "El Oued"),
        W(40, "40", "خنشلة", "Khenchela"),
        W(41, "41", "سوق أهراس", "Souk Ahras"),
        W(42, "42", "تيبازة", "Tipaza"),
        W(43, "43", "ميلة", "Mila"),
        W(44, "44", "عين الدفلى", "Aïn Defla"),
        W(45, "45", "النعامة", "Naâma"),
        W(46, "46", "عين تموشنت", "Aïn Témouchent"),
        W(47, "47", "غرداية", "Ghardaïa"),
        W(48, "48", "غليزان", "Relizane"),
        W(49, "49", "تيميمون", "Timimoun"),
        W(50, "50", "برج باجي مختار", "Bordj Badji Mokhtar"),
        W(51, "51", "أولاد جلال", "Ouled Djellal"),
        W(52, "52", "بني عباس", "Béni Abbès"),
        W(53, "53", "عين صالح", "In Salah"),
        W(54, "54", "عين قزام", "In Guezzam"),
        W(55, "55", "تقرت", "Touggourt"),
        W(56, "56", "جانت", "Djanet"),
        W(57, "57", "المغير", "El M'Ghair"),
        W(58, "58", "المنية", "El Meniaa")
    };

    private static Wilaya W(int id, string code, string nameAr, string nameFr) => new()
    {
        Id = id,
        Code = code,
        NameAr = nameAr,
        NameFr = nameFr,
        ShippingFee = 0m,
        IsActive = true
    };
}