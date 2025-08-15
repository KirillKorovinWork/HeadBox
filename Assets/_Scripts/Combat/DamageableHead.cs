using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody))]
public class DamageableHead : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float regenPerSecond = 0f;
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDead;

    [Header("FX")]
    public ParticleSystem lightImpactVfx;
    public ParticleSystem heavyImpactVfx;
    public AudioSource audioSource;
    public AudioClip[] thudClips;
    public GameObject decalPrefab;
    public int decalPoolSize = 20;

    [Header("Tuning")]
    public float heavyThreshold = 12f;
    public float decalBaseSize = 0.1f;
    public float decalMaxSize = 0.3f;

    private float _hp;
    private DecalPool _decalPool;

    void Awake()
    {
        _hp = maxHealth;
        _decalPool = new DecalPool(decalPrefab, decalPoolSize, this.transform);
    }

    void Update()
    {
        if (regenPerSecond > 0f && _hp > 0f && _hp < maxHealth)
        {
            _hp = Mathf.Min(maxHealth, _hp + regenPerSecond * Time.deltaTime);
            OnHealthChanged?.Invoke(_hp / maxHealth);
        }
    }

    void OnCollisionEnter(Collision c)
    {
        var src = c.collider.GetComponentInParent<ImpactSource>();
        if (src == null) return;

        float dmg = src.ComputeDamage(c);
        if (dmg <= 0f) return;

        ApplyDamage(dmg);

        var contact = c.GetContact(0);
        SpawnFeedback(contact.point, contact.normal, dmg, src);

        if (dmg >= heavyThreshold)
        {
            var slowmo = FindObjectOfType<SlowMoPulse>();
            if (slowmo) slowmo.Pulse(0.2f, 0.12f);
        }
    }

    void ApplyDamage(float dmg)
    {
        if (_hp <= 0f) return;
        _hp = Mathf.Max(0f, _hp - dmg);
        OnHealthChanged?.Invoke(_hp / maxHealth);
        if (_hp <= 0f) OnDead?.Invoke();
    }

    void SpawnFeedback(Vector3 point, Vector3 normal, float dmg, ImpactSource src)
    {
        float t = Mathf.InverseLerp(0f, heavyThreshold*1.5f, dmg);

        var vfx = (dmg >= heavyThreshold) ? heavyImpactVfx : lightImpactVfx;
        if (vfx)
        {
            vfx.transform.position = point;
            vfx.transform.rotation = Quaternion.LookRotation(normal);
            vfx.Play();
        }

        _decalPool.Spawn(point + normal * 0.005f,
                         Quaternion.LookRotation(normal),
                         Mathf.Lerp(decalBaseSize, decalMaxSize, t));

        if (audioSource && thudClips != null && thudClips.Length > 0)
        {
            var clip = thudClips[Random.Range(0, thudClips.Length)];
            audioSource.pitch = 1f + Random.Range(-0.05f, 0.05f) + t*0.1f;
            audioSource.PlayOneShot(clip, Mathf.Lerp(0.5f, 1f, t));
        }

        src.PlayHitSound(t);
    }
}