-- Her ilan türü için en az bir kategori sağlar. Mevcut kayıtları değiştirmez.
INSERT INTO public."Kategoriler" ("Id", "Ad", "Tur")
SELECT gen_random_uuid(), 'Genel', tur
FROM (VALUES ('Sosyal'), ('Kayip'), ('Buluntu')) AS varsayilan(tur)
WHERE NOT EXISTS (SELECT 1 FROM public."Kategoriler" k WHERE k."Tur" = varsayilan.tur)
ON CONFLICT ("Ad", "Tur") DO NOTHING;
