using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
// BoxCollider не обязателен, но нужен если используешь "удар без смещения" через растяжение коллайдера
public class DesktopHandController : MonoBehaviour
{
    [Header("Camera / Depth")]
    public Camera cam;
    public Transform depthAnchor;            // обычно — голова
    public float depthOffset = 0.12f;        // базовый отступ от плоскости якоря вдоль взгляда камеры
    public float scrollDepthStep = 0.15f;    // колесо мыши — подстройка глубины

    [Header("Manual Offset (camera space)")]
    [Tooltip("Смещение в Оси Камеры: (Right, Up, Forward). Например (0,0,-0.3) — держим кулак на 30см ближе к камере, чем плоскость головы.")]
    public Vector3 offsetLocal = new Vector3(0f, 0f, -0.30f);
    public bool enableOffsetHotkeys = true;
    [Tooltip("SHIFT + колесо = X (Right), CTRL + колесо = Y (Up), ALT + колесо = Z (Forward)")]
    public float offsetStep = 0.05f;

    [Header("Follow (base)")]
    public float followSpeed = 26f;
    public float maxSpeed = 40f;
    public float followLerp = 0.6f;

    [Header("Punch (optional, no forward shift)")]
    public KeyCode punchKey = KeyCode.Mouse1; // ПКМ
    public bool punchByColliderStretch = true; // если есть BoxCollider — растягиваем его; иначе импульс
    public float punchExtend = 0.5f;           // насколько вытягиваем коллайдер по Z
    public float punchOutTime = 0.06f;
    public float returnTime = 0.12f;
    public bool stretchVisual = false;         // тянем ли визуал вместе с коллайдером
    public float punchImpulse = 3.0f;          // fallback: импульс, если нет BoxCollider
    public float punchMaxAddSpeed = 20f;       // повышаем лимит скорости во время удара

    Rigidbody rb;
    BoxCollider box;
    Vector3 baseScale, baseCenter, baseSize;

    float manualDepthOffset;
    bool punching;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        box = GetComponent<BoxCollider>();
        if (box)
        {
            baseCenter = box.center;
            baseSize = box.size;
        }
        baseScale = transform.localScale;
    }

    void Update()
    {
        // Глубина колесиком
        float wheel = Input.mouseScrollDelta.y;

        if (enableOffsetHotkeys && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
            offsetLocal.x += wheel * offsetStep;           // Right
        else if (enableOffsetHotkeys && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
            offsetLocal.y += wheel * offsetStep;           // Up
        else if (enableOffsetHotkeys && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            offsetLocal.z += wheel * offsetStep;           // Forward
        else if (Mathf.Abs(wheel) > 0.0001f)
            manualDepthOffset += wheel * scrollDepthStep;  // обычная глубина

        // Удар
        if (Input.GetKeyDown(punchKey) && !punching)
            StartCoroutine(PunchRoutine());
    }

    IEnumerator PunchRoutine()
    {
        punching = true;

        if (punchByColliderStretch && box)
        {
            // растягиваем коллайдер вперёд (центр остаётся на месте) — без смещения позиции
            yield return StretchCollider(true, punchOutTime);
            yield return StretchCollider(false, returnTime);
        }
        else
        {
            // fallback: короткий физ. импульс (центр чуть сместится физикой)
            Vector3 fwd = (cam ? cam.transform.forward : transform.forward);
            rb.AddForce(fwd * punchImpulse, ForceMode.Impulse);
            yield return new WaitForSeconds(punchOutTime + returnTime);
        }

        punching = false;
    }

    IEnumerator StretchCollider(bool extend, float time)
    {
        float dz = extend ? punchExtend : 0f;

        Vector3 startCenter = box.center;
        Vector3 startSize = box.size;
        Vector3 targetCenter = new Vector3(baseCenter.x, baseCenter.y, baseCenter.z + dz * 0.5f);
        Vector3 targetSize = new Vector3(baseSize.x, baseSize.y, baseSize.z + dz);

        Vector3 startScale = transform.localScale;
        Vector3 targetScale = stretchVisual
            ? new Vector3(baseScale.x, baseScale.y, baseScale.z * (targetSize.z / baseSize.z))
            : baseScale;

        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, time));
            k = k * k * (3f - 2f * k);
            box.center = Vector3.Lerp(startCenter, targetCenter, k);
            box.size = Vector3.Lerp(startSize, targetSize, k);
            if (stretchVisual) transform.localScale = Vector3.Lerp(startScale, targetScale, k);
            yield return null;
        }

        box.center = targetCenter;
        box.size = targetSize;
        if (stretchVisual) transform.localScale = targetScale;
    }

    void FixedUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // Базовая цель — пересечение луча с плоскостью через depthAnchor
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 baseTarget;
        if (depthAnchor != null)
        {
            Plane plane = new Plane(-cam.transform.forward, depthAnchor.position);
            float t;
            if (!plane.Raycast(ray, out t)) t = 1.6f;
            baseTarget = ray.origin + ray.direction * t + cam.transform.forward * (depthOffset + manualDepthOffset);
        }
        else
        {
            float defaultDepth = 1.6f + manualDepthOffset;
            baseTarget = ray.origin + ray.direction * defaultDepth;
        }

        // ПРИМЕНЯЕМ РУЧНОЕ СМЕЩЕНИЕ (в осях камеры)
        Vector3 camRight = cam.transform.right;
        Vector3 camUp = cam.transform.up;
        Vector3 camFwd = cam.transform.forward;
        Vector3 manual = camRight * offsetLocal.x + camUp * offsetLocal.y + camFwd * offsetLocal.z;

        Vector3 target = baseTarget + manual;

        float maxAllowed = punching ? (maxSpeed + punchMaxAddSpeed) : maxSpeed;
        Vector3 diff = target - rb.position;
        Vector3 desiredVel = diff * followSpeed;
        Vector3 newVel = Vector3.Lerp(rb.velocity, desiredVel, followLerp);
        rb.velocity = Vector3.ClampMagnitude(newVel, maxAllowed);
    }
}
