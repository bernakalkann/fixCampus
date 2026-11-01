-- Canlı şema üzerinde yalnızca bu testin ürettiği kayıtları kullanır.
-- İç blok başarıda da geri alınır; mevcut kayıtlar güncellenmez/silinmez.
DO $kontrol$
DECLARE
    sahip uuid;
    diger uuid;
    kategori uuid;
    sosyal uuid;
    kayip uuid;
    buluntu uuid;
    onceki bigint;
    sonuc bigint;
BEGIN
    SELECT "Id" INTO sahip FROM public."Profiller" ORDER BY "Id" LIMIT 1;
    SELECT "Id" INTO diger FROM public."Profiller" WHERE "Id" <> sahip ORDER BY "Id" LIMIT 1;
    IF sahip IS NULL OR diger IS NULL THEN RAISE EXCEPTION 'İki test profili gerekli'; END IF;
    SELECT count(*) INTO onceki FROM public."Ilanlar";
    BEGIN
        SELECT "Id" INTO kategori FROM public."Kategoriler" WHERE "Tur" = 'Sosyal' LIMIT 1;
        INSERT INTO public."Ilanlar" ("Id","SahipId","KategoriId","Baslik","Aciklama","Tur","Yer","BulusmaTarihi","Durum","OlusturulmaTarihi")
        VALUES (gen_random_uuid(),sahip,kategori,'CRUD TEST SOSYAL','Geçici test kaydı','Sosyal','Kampüs',now()+interval '1 day','Aktif',now()) RETURNING "Id" INTO sosyal;
        SELECT "Id" INTO kategori FROM public."Kategoriler" WHERE "Tur" = 'Kayip' LIMIT 1;
        INSERT INTO public."Ilanlar" ("Id","SahipId","KategoriId","Baslik","Aciklama","Tur","Yer","BulusmaTarihi","Durum","OlusturulmaTarihi")
        VALUES (gen_random_uuid(),sahip,kategori,'CRUD TEST KAYIP','Geçici test kaydı','Kayip','Kampüs',null,'Aktif',now()) RETURNING "Id" INTO kayip;
        SELECT "Id" INTO kategori FROM public."Kategoriler" WHERE "Tur" = 'Buluntu' LIMIT 1;
        INSERT INTO public."Ilanlar" ("Id","SahipId","KategoriId","Baslik","Aciklama","Tur","Yer","BulusmaTarihi","Durum","OlusturulmaTarihi")
        VALUES (gen_random_uuid(),sahip,kategori,'CRUD TEST BULUNTU','Geçici test kaydı','Buluntu','Kampüs',null,'Aktif',now()) RETURNING "Id" INTO buluntu;
        SELECT count(*) INTO sonuc FROM public."Ilanlar" WHERE "Id" IN (sosyal,kayip,buluntu);
        IF sonuc <> 3 THEN RAISE EXCEPTION 'Oluşturma/okuma başarısız'; END IF;
        UPDATE public."Ilanlar" SET "Baslik" = 'CRUD TEST GUNCELLENDI' WHERE "Id" = kayip;
        IF NOT EXISTS (SELECT 1 FROM public."Ilanlar" WHERE "Id" = kayip AND "Baslik" = 'CRUD TEST GUNCELLENDI') THEN RAISE EXCEPTION 'Güncelleme başarısız'; END IF;
        UPDATE public."Ilanlar" SET "Durum" = 'Gizli' WHERE "Id" = buluntu;
        IF EXISTS (SELECT 1 FROM public."Ilanlar" WHERE "Id" = buluntu AND "Durum" <> 'Gizli') THEN RAISE EXCEPTION 'Gizli ilan filtrelemesi başarısız'; END IF;
        BEGIN
            UPDATE public."Ilanlar" SET "BulusmaTarihi" = null WHERE "Id" = sosyal;
            RAISE EXCEPTION 'Sosyal tarih kısıtı uygulanmıyor';
        EXCEPTION WHEN check_violation THEN NULL;
        END;
        BEGIN
            UPDATE public."Ilanlar" SET "KategoriId" = gen_random_uuid() WHERE "Id" = kayip;
            RAISE EXCEPTION 'Kategori yabancı anahtarı uygulanmıyor';
        EXCEPTION WHEN foreign_key_violation THEN NULL;
        END;
        INSERT INTO public."Katilimlar" ("Id","IlanId","ProfilId","KatilimTarihi") VALUES (gen_random_uuid(),sosyal,diger,now());
        INSERT INTO public."IlanFotograflari" ("Id","IlanId","DosyaYolu","YuklemeTarihi") VALUES (gen_random_uuid(),sosyal,'test-only/no-file-created',now());
        INSERT INTO public."Sikayetler" ("Id","IlanId","BildirenId","Neden","Durum","OlusturulmaTarihi") VALUES (gen_random_uuid(),sosyal,diger,'Test','Bekliyor',now());
        DELETE FROM public."Ilanlar" WHERE "Id" = sosyal;
        IF EXISTS (SELECT 1 FROM public."Katilimlar" WHERE "IlanId" = sosyal) OR EXISTS (SELECT 1 FROM public."IlanFotograflari" WHERE "IlanId" = sosyal) OR EXISTS (SELECT 1 FROM public."Sikayetler" WHERE "IlanId" = sosyal) THEN RAISE EXCEPTION 'Cascade silme başarısız'; END IF;
        DELETE FROM public."Ilanlar" WHERE "Id" IN (kayip,buluntu);
        IF (SELECT count(*) FROM public."Ilanlar") <> onceki THEN RAISE EXCEPTION 'Kayıt sayısı değişti'; END IF;
        -- Başarılı bütün işlemler de alt transaction ile geri alınır.
        RAISE EXCEPTION 'Test tamamlandı, test işlemleri geri alınıyor' USING ERRCODE = 'ZX001';
    EXCEPTION WHEN SQLSTATE 'ZX001' THEN NULL;
    END;
    IF (SELECT count(*) FROM public."Ilanlar") <> onceki THEN RAISE EXCEPTION 'Rollback başarısız'; END IF;
END $kontrol$;
SELECT 'PASS: canlı SQL CRUD, üç tür, tarih/FK kısıtları, gizlilik, cascade ve rollback' AS sonuc,
    (SELECT count(*) FROM public."Profiller") AS profil,
    (SELECT count(*) FROM public."Kategoriler") AS kategori,
    (SELECT count(*) FROM public."Ilanlar") AS ilan;
