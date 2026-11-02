using FixCampus.Web.Contracts;
using FixCampus.Web.Models.Entities;

namespace FixCampus.Web.Data;

public static class DefaultCategories
{
    public static readonly IReadOnlyList<Kategori> Items = new List<Kategori>
    {
        // Sosyal
        new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), Ad = "Ders & Sınav Çalışma", Tur = "Sosyal" },
        new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000002"), Ad = "Spor & Turnuva", Tur = "Sosyal" },
        new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000003"), Ad = "Kahve & Kampüs Sohbeti", Tur = "Sosyal" },
        new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000004"), Ad = "Masa Oyunları & Gaming", Tur = "Sosyal" },
        new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000005"), Ad = "Sanat, Tiyatro & Konser", Tur = "Sosyal" },
        new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000006"), Ad = "Proje & Takım Arkadaşı", Tur = "Sosyal" },
        new() { Id = Guid.Parse("11111111-0000-0000-0000-000000000007"), Ad = "Genel Sosyal", Tur = "Sosyal" },

        // Kayıp
        new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000001"), Ad = "Elektronik & Aksesuar", Tur = "Kayip" },
        new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000002"), Ad = "Cüzdan, Kimlik & Kart", Tur = "Kayip" },
        new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000003"), Ad = "Kırtasiye, Çanta & Kitap", Tur = "Kayip" },
        new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000004"), Ad = "Giyim & Kişisel Eşya", Tur = "Kayip" },
        new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000005"), Ad = "Anahtar", Tur = "Kayip" },
        new() { Id = Guid.Parse("22222222-0000-0000-0000-000000000006"), Ad = "Diğer Kayıp", Tur = "Kayip" },

        // Buluntu
        new() { Id = Guid.Parse("33333333-0000-0000-0000-000000000001"), Ad = "Elektronik & Aksesuar", Tur = "Buluntu" },
        new() { Id = Guid.Parse("33333333-0000-0000-0000-000000000002"), Ad = "Cüzdan, Kimlik & Kart", Tur = "Buluntu" },
        new() { Id = Guid.Parse("33333333-0000-0000-0000-000000000003"), Ad = "Kırtasiye, Çanta & Kitap", Tur = "Buluntu" },
        new() { Id = Guid.Parse("33333333-0000-0000-0000-000000000004"), Ad = "Giyim & Kişisel Eşya", Tur = "Buluntu" },
        new() { Id = Guid.Parse("33333333-0000-0000-0000-000000000005"), Ad = "Anahtar", Tur = "Buluntu" },
        new() { Id = Guid.Parse("33333333-0000-0000-0000-000000000006"), Ad = "Diğer Buluntu", Tur = "Buluntu" }
    };

    public static IReadOnlyList<CategoryDto> AsDtos(string? tur = null)
    {
        var list = Items.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(tur))
        {
            var normalizedTur = char.ToUpperInvariant(tur[0]) + tur.Substring(1).ToLowerInvariant();
            list = list.Where(k => k.Tur == normalizedTur);
        }
        return list.OrderBy(k => k.Tur).ThenBy(k => k.Ad).Select(k => new CategoryDto(k.Id, k.Ad, k.Tur)).ToList();
    }
}

