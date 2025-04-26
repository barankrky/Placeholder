# PlaceHolder

## Konsept

Karakter, kıyamet sonrası bir dünyada geçmektedir. Oyuncu karakteri, kıyamette annesini kaybettiği için akli dengesi yerinde olmayan bir kuryedir. Dünyayı kıyamet hiç kopmamış gibi algılar. Kıyamet kopmadan önce kendisine sipariş verilen son ürünü sahibine teslim etmekle görevlidir.

## Geliştirme Planı

Oyun, oyuncunun yatay olarak ilerlediği 2D serbest dolaşım oyunudur. İlk odak noktası, başlangıç sahnesi, yıkık bir ev, etkileşimli nesneler ve karakterin dünyayı algılama biçimini yansıtan diyaloglardır.

### Aşamalar:

1.  **Yıkık Ev Sahnesi Tasarımı ve Kurulumu:** Oyuncunun başladığı yıkık evi temsil eden bir Unity sahnesi oluşturulacak.
2.  **Etkileşimli Nesneler ve Çevre Etkileşimi:** Eve etkileşimli nesneler yerleştirilecek (örneğin, radyo, kırık eşyalar, notlar).
3.  **Karakter Diyalog Sistemi:** Düşmanlarla karşılaşıldığında veya nesnelerle etkileşime girildiğinde karakterin diyaloglarını göstermek için bir sistem uygulanacak.
4.  **Temel Karakter Hareketi ve Sınırlar:** Karakterin yatayda serbestçe hareket etmesini sağlamak için `PlayerController.cs` scripti güncellenecek.
5.  **Temel Savaş Mekaniklerinin Entegrasyonu (Sonraki Aşama):** Evden dış dünyaya geçişte düşmanlarla karşılaşma ve savaş mekanikleri entegre edilecek.

### Mermaid Diyagramı:

```mermaid
graph TD
    A[Oyun Başladı] --> B(Sahne Yükle - Yıkık Ev);
    B --> C(Karakter Oluştur/Yerleştir);
    C --> C1(Kamera Takibi Başlat);
    B --> D(Ev Ortamı ve Etkileşimli Objeleri Oluştur);
    D --> D1(Sınırları Oluştur - Fiziksel Engel);
    C --> E{Karakter Hareket Girişi Var mı?};
    E -- Evet --> F(Karakteri Hareket Ettir);
    F --> C1;
    F --> G{Etkileşimli Objeye Yakın mı?};
    G -- Evet --> H(Etkileşim İkonu Göster);
    H --> I{Etkileşim Tuşuna Basıldı mı?};
    I -- Evet --> J(Etkileşim Mantığını Çalıştır - Radyo/Diyalog vb.);
    J --> G;
    G -- Hayır --> E;
    F -- Hayır --> E;
    I -- Hayır --> H;
    D1 --> K{Karakter Sınıra Çarptı mı?};
    K -- Evet --> L(İlerlemeyi Engelle);
    L --> E;
