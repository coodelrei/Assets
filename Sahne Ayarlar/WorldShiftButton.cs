using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Aynı butonla aç/kapa:
/// - ON: Kamera kayar (yukarı/aşağı), oyun durur (opsiyonel), panel sağdan içeri kayar.
/// - OFF: Kamera base konuma döner, panel sağa çıkar, oyun devam eder.
/// Canvas (Overlay) UI sabit kalır.
/// </summary>
[DisallowMultipleComponent]
public class WorldShiftButton : MonoBehaviour
{
    [Header("Kamera")]
    [Tooltip("Boşsa Camera.main kullanılır.")]
    public Camera targetCamera;
    [Tooltip("ON olduğunda kameranın gideceği Y offset (dünya birimi). +Y yukarı, -Y aşağı.")]
    public float shiftY = 0.5f;
    [Tooltip("İlk basışta 'ON' durumunda kameranın yukarı mı (true) yoksa aşağı mı (false) gitmesi?")]
    public bool moveUpOnFirstPress = true;

    [Header("Kamera Animasyonu")]
    public bool animateCamera = true;
    [Range(0.05f, 2f)] public float cameraDuration = 0.25f;
    public AnimationCurve cameraEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Oyun Durumu")]
    [Tooltip("ON durumunda oyunu durdur (Time.timeScale=0).")]
    public bool pauseWhenOn = true;

    [Header("Panel (UI)")]
    [Tooltip("Sağdan içeri kayacak olan panel (RectTransform).")]
    public RectTransform slidingPanel;
    [Tooltip("Panel animasyonu aktif olsun mu?")]
    public bool animatePanel = true;
    [Range(0.05f, 2f)] public float panelDuration = 0.3f;
    public AnimationCurve panelEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Panelin gizli konumu için sağa atılacak mesafe (anchoredPosition.x’e +).")]
    public float panelOffscreenAddX = 800f;
    [Tooltip("Toggle ON başladığında paneli otomatik göster.")]
    public bool showPanelOnToggleOn = true;

    // Runtime
    Vector3 _baseCamPos;
    bool _haveBaseCamPos;
    bool _isOn;                 // Toggle state: ON (kayıklı & panel açık) / OFF
    bool _pausedByUs;
    float _savedTimeScale = 1f;
    Coroutine _coCam;
    Coroutine _coPanel;

    // Panel pos cache
    bool _panelPosCached;
    Vector2 _panelShownPos;
    Vector2 _panelHiddenPos;

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        CacheBaseCamIfNeeded();
        CachePanelPositions(false);
    }

    void OnDisable()
    {
        // Devre dışı kalırken varsa pause’u bırak
        if (_pausedByUs)
        {
            Time.timeScale = (_savedTimeScale > 0f) ? _savedTimeScale : 1f;
            _pausedByUs = false;
        }
    }

    void CacheBaseCamIfNeeded()
    {
        if (targetCamera == null) return;
        if (!_haveBaseCamPos)
        {
            _baseCamPos = targetCamera.transform.position;
            _haveBaseCamPos = true;
        }
    }

    void CachePanelPositions(bool force)
    {
        if (slidingPanel == null) return;
        if (_panelPosCached && !force) return;

        // Panel sahnede "gösterilmek istenen" konumdayken (tasarladığın yerde) dursun:
        _panelShownPos = slidingPanel.anchoredPosition;
        // Gizli konum: gösterilen konumun sağında, panelOffscreenAddX kadar
        _panelHiddenPos = _panelShownPos + new Vector2(Mathf.Abs(panelOffscreenAddX), 0f);

        // Başlangıç state OFF ise paneli gizle
        if (!_isOn)
            slidingPanel.anchoredPosition = _panelHiddenPos;

        _panelPosCached = true;
    }

    /// <summary>UI Button OnClick() içine bağla.</summary>
    public void ToggleShift()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return;

        CacheBaseCamIfNeeded();
        CachePanelPositions(false);

        // Hedef kamera konumu
        Vector3 from = targetCamera.transform.position;
        float dir = moveUpOnFirstPress ? +1f : -1f;
        Vector3 shifted = _baseCamPos + new Vector3(0f, dir * shiftY, 0f);
        Vector3 to = _isOn ? _baseCamPos : shifted;

        // Mevcut animasyonları iptal et
        if (_coCam != null) StopCoroutine(_coCam);
        if (_coPanel != null) StopCoroutine(_coPanel);

        // ON’a geçerken pause
        if (!_isOn && pauseWhenOn && !_pausedByUs)
        {
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _pausedByUs = true;
        }

        // Kamera animasyonu (unscaled)
        _coCam = StartCoroutine(CoMoveCamera(from, to, animateCamera ? cameraDuration : 0f, finishingStateIsOff: _isOn));

        // Panel animasyonu
        if (slidingPanel != null)
        {
            if (!_isOn)
            {
                // ON → panel içeri (sağdan sola)
                if (showPanelOnToggleOn)
                    _coPanel = StartCoroutine(CoSlidePanel(_panelHiddenPos, _panelShownPos, animatePanel ? panelDuration : 0f));
            }
            else
            {
                // OFF → panel dışarı (soldan sağa)
                _coPanel = StartCoroutine(CoSlidePanel(_panelShownPos, _panelHiddenPos, animatePanel ? panelDuration : 0f));
            }
        }

        _isOn = !_isOn;
    }

    IEnumerator CoMoveCamera(Vector3 from, Vector3 to, float dur, bool finishingStateIsOff)
    {
        if (dur <= 0f)
        {
            targetCamera.transform.position = to;
            yield return null;
            // OFF’a dönerken oyunu devam ettir
            if (finishingStateIsOff && _pausedByUs)
            {
                Time.timeScale = (_savedTimeScale > 0f) ? _savedTimeScale : 1f;
                _pausedByUs = false;
            }
            _coCam = null;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            float u = Mathf.Clamp01(t);
            float e = cameraEase != null ? Mathf.Clamp01(cameraEase.Evaluate(u)) : u;
            targetCamera.transform.position = Vector3.Lerp(from, to, e);
            yield return null;
        }
        targetCamera.transform.position = to;

        if (finishingStateIsOff && _pausedByUs)
        {
            Time.timeScale = (_savedTimeScale > 0f) ? _savedTimeScale : 1f;
            _pausedByUs = false;
        }

        _coCam = null;
    }

    IEnumerator CoSlidePanel(Vector2 from, Vector2 to, float dur)
    {
        if (dur <= 0f)
        {
            slidingPanel.anchoredPosition = to;
            yield return null;
            _coPanel = null;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, dur);
            float u = Mathf.Clamp01(t);
            float e = panelEase != null ? Mathf.Clamp01(panelEase.Evaluate(u)) : u;
            slidingPanel.anchoredPosition = Vector2.Lerp(from, to, e);
            yield return null;
        }
        slidingPanel.anchoredPosition = to;
        _coPanel = null;
    }

    // Zorla OFF yap + resume (opsiyonel)
    public void ForceOffAndResume()
    {
        if (targetCamera == null) return;
        CacheBaseCamIfNeeded();
        CachePanelPositions(true);

        if (_coCam != null) StopCoroutine(_coCam);
        if (_coPanel != null) StopCoroutine(_coPanel);

        targetCamera.transform.position = _baseCamPos;

        if (slidingPanel != null)
            slidingPanel.anchoredPosition = _panelHiddenPos;

        if (_pausedByUs)
        {
            Time.timeScale = (_savedTimeScale > 0f) ? _savedTimeScale : 1f;
            _pausedByUs = false;
        }

        _isOn = false;
    }
}
