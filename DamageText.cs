using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    public float flyDuration = 1f;  // Duration to fly up
    public float fadeDuration = 1f; // Duration to fade out
    public float flyHeight = 1.5f;  // How high the damage number flies
    private TextMeshPro damageText;
    private Vector3 startPos;
    private float startTime;
    private bool isFading = false;

    public void SetDamageText(float dmg)
    {
        Debug.Log($"Damage Passed in : {dmg}");
        this.damageText.text = dmg.ToString();
    }

    void Start()
    {
        // Store the starting position of the damage number
        startPos = transform.position;

        // Get the TextMeshPro component
        damageText = GetComponent<TextMeshPro>();

        // Set the initial alpha to 1 (fully visible)
        Color color = damageText.color;
        color.a = 1f;
        damageText.color = color;

        // Start the flying animation
        startTime = Time.time;
    }

    void Update()
    {
        // Handle the flying and fading effect
        if (!isFading)
        {
            // Fly up over time
            float elapsedTime = Time.time - startTime;
            if (elapsedTime < flyDuration)
            {
                transform.position = startPos + Vector3.up * (flyHeight * (elapsedTime / flyDuration));
            }
            else
            {
                // Start fading after flying up
                isFading = true;
                startTime = Time.time;  // Reset start time for fading
            }
        }
        else
        {
            // Fade out over time
            float fadeElapsedTime = Time.time - startTime;
            if (fadeElapsedTime < fadeDuration)
            {
                float alpha = 1 - (fadeElapsedTime / fadeDuration);
                Color color = damageText.color;
                color.a = alpha;
                damageText.color = color;

                // Slowly fall down as it fades
                transform.position = startPos + Vector3.up * (flyHeight * (1 - (fadeElapsedTime / fadeDuration)));
            }
            else
            {
                // Destroy the object after fading
                Destroy(gameObject);
            }
        }
    }
}
