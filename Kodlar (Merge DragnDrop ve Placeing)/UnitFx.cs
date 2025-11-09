using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class UnitFx : MonoBehaviour
{
    public enum FxState
    {
        None,
        MergeTarget,    // ritmik büyü-küçül + (ops.) renk parlaması
        MergeSuccess,   // kısa onay
        MergeFail,      // kısa red
        Good,
        Bad
    }

    [Header("Ölçek Pulsu")]
    public bool scalePulseAçık = true;
    [Min(1f)] public float pulseScale = 1.08f;
    [Min(0.05f)] public float pulsePeriod = 0.5f;
    public Vector2 eksenÇarpanı = new Vector2(1f, 1f);
    public AnimationCurve pulseEğrisi;

    [Header("Renk Parlaması (opsiyonel)")]
    public bool renkPulseAçık = false;
    public Color pulseRenk = Color.white;
    [Range(0f, 1f)] public float renkŞiddet = 0.35f;

    [Header("Görsel Hedefler")]
    public Transform visualRoot;        // boşsa child SR ya da root
    public SpriteRenderer renkTarget;   // boşsa visualRoot altında bulunur

    // runtime
    private FxState _state = FxState.None;
    private Coroutine _pulseCo;
    private Vector3 _baseScale = Vector3.one;
    private Color _baseColor = Color.white;
    private bool _inited;
    private bool _pendingResync;

    void Awake()
    {
        if (visualRoot == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>(true);
            visualRoot = sr != null ? sr.transform : transform;
        }
        if (renkTarget == null)
        {
            renkTarget = (visualRoot != null)
                ? visualRoot.GetComponentInChildren<SpriteRenderer>(true)
                : GetComponentInChildren<SpriteRenderer>(true);
        }

        _baseScale = (visualRoot != null ? visualRoot.localScale : Vector3.one);
        _baseColor = (renkTarget != null ? renkTarget.color : Color.white);
        _inited = true;
    }

    void OnEnable()
    {
        if (!_inited)
        {
            _baseScale = (visualRoot != null ? visualRoot.localScale : Vector3.one);
            _baseColor = (renkTarget != null ? renkTarget.color : Color.white);
            _inited = true;
        }
        StartCoroutine(ResyncNextFrame());
        ApplyStateImmediate();
    }

    void OnDisable() { StopAllFx(); }
    void OnDestroy() { StopAllFx(); }

    /// Unit görseli grid’e göre yeniden kurulduğunda Unit çağırır.
    public void MarkVisualChanged() { _pendingResync = true; }

    IEnumerator ResyncNextFrame()
    {
        while (enabled)
        {
            if (_pendingResync)
            {
                _pendingResync = false;
                yield return null; // bir frame bekle
                if (visualRoot != null) _baseScale = visualRoot.localScale;
                if (renkTarget != null) _baseColor = renkTarget.color;

                if (_state == FxState.MergeTarget && _pulseCo == null)
                    _pulseCo = StartCoroutine(PulseLoop());
            }
            yield return null;
        }
    }

    public void SetState(FxState newState)
    {
        if (_state == newState) return;
        _state = newState;
        ApplyStateImmediate();
    }

    void ApplyStateImmediate()
    {
        if (_pulseCo != null) StopCoroutine(_pulseCo);
        _pulseCo = null;

        if (visualRoot == null) return;

        switch (_state)
        {
            default:
            case FxState.None:
            case FxState.Good:
            case FxState.Bad:
                visualRoot.localScale = _baseScale;
                if (renkTarget != null) renkTarget.color = _baseColor;
                break;

            case FxState.MergeTarget:
                _pulseCo = StartCoroutine(PulseLoop());
                break;

            case FxState.MergeSuccess:
                _pulseCo = StartCoroutine(PunchOnce(1.12f, 0.18f));
                break;

            case FxState.MergeFail:
                _pulseCo = StartCoroutine(PunchOnce(0.92f, 0.18f));
                break;
        }
    }

    IEnumerator PulseLoop()
    {
        float t = 0f;
        while (_state == FxState.MergeTarget)
        {
            t += Time.deltaTime;

            float norm;
            if (pulseEğrisi != null && pulseEğrisi.length > 0)
            {
                float u = Mathf.Repeat(t / Mathf.Max(0.01f, pulsePeriod), 1f);
                norm = Mathf.Clamp01(pulseEğrisi.Evaluate(u));
            }
            else
            {
                float phase = (Mathf.PI * 2f) * t / Mathf.Max(0.01f, pulsePeriod);
                norm = (Mathf.Sin(phase) * 0.5f + 0.5f);
            }

            if (scalePulseAçık)
            {
                float s = Mathf.Lerp(1f, pulseScale, norm);
                var mul = new Vector3(s * eksenÇarpanı.x, s * eksenÇarpanı.y, 1f);
                visualRoot.localScale = Vector3.Scale(_baseScale, mul);
            }

            if (renkPulseAçık && renkTarget != null)
            {
                Color c = Color.Lerp(_baseColor, pulseRenk, norm * renkŞiddet);
                renkTarget.color = c;
            }

            yield return null;
        }

        visualRoot.localScale = _baseScale;
        if (renkTarget != null) renkTarget.color = _baseColor;
        _pulseCo = null;
    }

    IEnumerator PunchOnce(float targetMul, float dur)
    {
        float t = 0f;
        Vector3 start = _baseScale;
        Vector3 peak = _baseScale * targetMul;

        while (t < 1f)
        {
            t += Time.deltaTime / (dur * 0.5f);
            float e = SmoothStep01(t);
            visualRoot.localScale = Vector3.Lerp(start, peak, e);
            yield return null;
        }

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / (dur * 0.5f);
            float e = SmoothStep01(t);
            visualRoot.localScale = Vector3.Lerp(peak, _baseScale, e);
            yield return null;
        }

        visualRoot.localScale = _baseScale;
        _pulseCo = null;
        if (_state == FxState.MergeSuccess || _state == FxState.MergeFail)
            _state = FxState.None;
    }

    static float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private void StopAllFx()
    {
        if (_pulseCo != null) { StopCoroutine(_pulseCo); _pulseCo = null; }
        if (visualRoot != null) visualRoot.localScale = _baseScale;
        if (renkTarget != null) renkTarget.color = _baseColor;
    }
}
