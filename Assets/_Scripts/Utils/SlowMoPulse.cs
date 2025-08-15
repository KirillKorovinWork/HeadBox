using UnityEngine;
public class SlowMoPulse : MonoBehaviour
{
    public void Pulse(float timeScale = 0.2f, float duration = 0.12f)
    {
        if (gameObject.activeInHierarchy) StartCoroutine(Run(timeScale, duration));
    }
    System.Collections.IEnumerator Run(float s, float d)
    {
        float prev = Time.timeScale;
        Time.timeScale = s;
        yield return new WaitForSecondsRealtime(d);
        Time.timeScale = prev;
    }
}