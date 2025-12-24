using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class PerfectLoopVideo : MonoBehaviour
{
    [Header("Atanacaklar")]
    public VideoClip videoClip;         // Videoyu buraya sürükle (URL değil!)
    public RawImage targetRawImage;     // Ekranda görünen RawImage

    private VideoPlayer vp;

    void Awake()
    {
        vp = GetComponent<VideoPlayer>();

        // --- KRİTİK AYARLAR ---
        
        // 1. Kaynak Tipi: URL yerine Clip kullanıyoruz.
        // Bu, Unity'nin videoyu hafızada daha iyi yönetmesini sağlar.
        vp.source = VideoSource.VideoClip;
        vp.clip = videoClip;

        // 2. Render Modu: Texture
        vp.renderMode = VideoRenderMode.RenderTexture;

        // 3. Ses Senkronizasyonunu Kapat
        // Videonun sese ayak uydurmaya çalışırken takılmasını önler.
        vp.audioOutputMode = VideoAudioOutputMode.None;

        // 4. Loop Ayarları
        vp.isLooping = true;
        vp.waitForFirstFrame = true;
        
        // SkipOnDrop: true yaparsak işlemci yetişemezse kare atlar (akıcı görünür)
        // false yaparsak her kareyi göstermeye çalışır (yavaşlarsa donar)
        // Loop için genellikle 'true' daha iyidir.
        vp.skipOnDrop = true; 
    }

    void OnEnable()
    {
        // Başlangıçta görüntüyü gizle (Siyah parlamayı önlemek için)
        if (targetRawImage != null) 
            targetRawImage.color = new Color(1, 1, 1, 0);

        StartCoroutine(StartSmoothLoop());
    }

    IEnumerator StartSmoothLoop()
    {
        vp.Prepare();

        // Hazırlanana kadar bekle
        while (!vp.isPrepared)
        {
            yield return null;
        }

        vp.Play();

        // Video oynamaya başladı, ama ilk karenin Texture'a çizilmesi
        // GPU tarafında 1-2 frame gecikebilir.
        // Bunu beklemek "siyah ekran" riskini sıfıra indirir.
        while (vp.frame < 2) // Garanti olsun diye 2. kareyi bekliyoruz
        {
            yield return null;
        }

        // Artık görüntü aktı, RawImage'ı görünür yap
        if (targetRawImage != null) 
            targetRawImage.color = Color.white;
    }
}