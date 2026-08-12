# ETDVol - Modern Windows Ses Denetim Uygulaması

**ETDVol**, Windows görev çubuğu üzerinden ses seviyesini fare tekerleğiyle hızlıca ayarlamanızı ve fare tekerleği orta tuşuna tıklayarak bağlı ses cihazlarınız (kulaklık, hoparlör vb.) arasında anında geçiş yapmanızı sağlayan modern, hafif ve şık bir Windows uygulamasıdır.

![ETDVol Logo](app.png)

---

## 🌟 Öne Çıkan Özellikler

- 🔊 **Görev Çubuğunda Ses Scroll'u**: Fare imleci görev çubuğundayken tekerleği yukarı/aşağı çevirerek sesi hızlıca artırıp azaltın.
- 🎧 **Shift + Orta Tuş İle Aygıt Değişimi**: `Shift + Görev Çubuğunda Orta Tuş (Tekerlek Tıklaması)` yaparak ses çıkış cihazlarınızı (Kulaklık, Hoparlör vb.) anında değiştirin.
- 🖥️ **Şık Görsel Gösterge (OSD)**: Ses değiştiğinde veya cihaz değiştiğinde ekranın alt ortasında modern, akıcı OSD paneli belirir.
- ⚙️ **Kolay Ayarlar Menüsü**: OSD paneline veya tepsi simgesine sol tıklayarak ses değişim adımı (%1-%10), ivmelenme ve aktif edilecek cihazlar listesini kolayca yönetin.
- 🇹🇷 / 🇬🇧 **Çift Dil Desteği**: Türkçe ve İngilizce dil seçenekleri.
- 🚀 **Windows Kurulum Sihirbazı & Denetim Masası Entegrasyonu**: Profesyonel kurulum sihirbazı (`ETDVol_Setup.exe`), Windows ile otomatik başlama tercihi ve Denetim Masası Program Ekle/Kaldır entegrasyonu.

---

## 📦 Kurulum & Yayın

Uygulama tek bir bağımsız kurulum dosyası olarak yayınlanmaktadır:

```text
Publish/
└── ETDVol_Setup.exe   (Tüm uygulama bileşenlerini içeren tek parçalık kurulum dosyası)
```

1. **`Publish/ETDVol_Setup.exe`** dosyasını çalıştırın.
2. Kurulum klasörünü ve başlangıç tercihinizi seçip **"Kurulumu Başlat"** butonuna tıklayın.
3. Uygulama kurulup arka planda otomatik çalışmaya başlayacaktır.

---

## 🛠️ Proje Mimarısı (C# .NET 8 WPF)

Proje 3 ana modülden oluşmaktadır:
- **`ETDVol.csproj`**: Görev çubuğu hook'larını dinleyen, OSD gösteren ve arka planda çalışan ana uygulama.
- **`ETDVol.Setup.csproj`**: Uygulamayı hedef klasöre çıkaran, Windows başlangıcına ve Denetim Masasına kaydeden kurulum sihirbazı (`ETDVol_Setup.exe`).
- **`ETDVol.Uninstall.csproj`**: Uygulamayı ve kayıt defteri girdilerini temiz şekilde kaldıran kaldırma sihirbazı (`Uninstall.exe`).

### Projeyi Derleme (Build Instructions)

```bash
# 1. Ana Uygulamayı Derleyin
dotnet publish -c Release ETDVol/ETDVol.csproj -o ETDVol/bin/Release/net8.0-windows/win-x64/publish

# 2. Kaldırma Sihirbazını Derleyin
dotnet publish -c Release ETDVol.Uninstall/ETDVol.Uninstall.csproj -o ETDVol.Uninstall/bin/Release/net8.0-windows/win-x64/publish

# 3. Tek Parça Kurulum Sihirbazını Üretin
dotnet publish -c Release ETDVol.Setup/ETDVol.Setup.csproj -o Publish
```

---

## 📄 Lisans ve Kullanım Şartları

Bu yazılım [LICENSE](LICENSE) dosyası şartlarına tabidir:

1. **Sorumluluk Reddi**: Yazılım "olduğu gibi" sunulmaktadır. Geliştirici/Yazar kullanımından doğabilecek hiçbir doğrudan veya dolaylı zarardan sorumlu tutulamaz.
2. **Marka ve Logo Kısıtlaması**: **"ETDVol"** ismi, resmi logosu, ikonu ve görsel ögeleri saklıdır. İzinsiz ticari amaçlarla veya başka ürünlerde kullanılamaz ve dağıtılamaz.
