using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ImpactSource : MonoBehaviour
{
    [Header("Damage")]
    public float damageMultiplier = 2.5f;
    public float minRelativeSpeed = 1.0f;
    public float maxDamagePerHit = 25f;

    [Header("Feedback")]
    public AudioSource audioSource;
    public AudioClip[] hitClips;
    public float pitchJitter = 0.06f;

    public float ComputeDamage(Collision c)
    {
        float v = c.relativeVelocity.magnitude;
        if (v < minRelativeSpeed) return 0f;
        float dmg = v * damageMultiplier;
        return Mathf.Min(dmg, maxDamagePerHit);
    }

    public void PlayHitSound(float normalizedStrength = 0.5f)
    {
        if (audioSource == null || hitClips == null || hitClips.Length == 0) return;
        var clip = hitClips[Random.Range(0, hitClips.Length)];
        audioSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter) + Mathf.Lerp(0f, 0.15f, normalizedStrength);
        audioSource.PlayOneShot(clip, Mathf.Lerp(0.4f, 1f, normalizedStrength));
    }
}