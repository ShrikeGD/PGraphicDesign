using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ChildImageCrossfadeSlideshow : MonoBehaviour
{
    [Header("Timing (seconds)")]
    [Tooltip("Total time for one full cycle through all child images.")]
    public float totalDuration = 10f;

    [Tooltip("Duration of the crossfade between images.")]
    public float fadeDuration = 1f;

    [Tooltip("Loop the slideshow when it reaches the end?")]
    public bool loop = true;

    [Header("Images (auto-populated on Validate)")]
    [Tooltip("All child Images that will be crossfaded. Auto-collected from children.")]
    public List<Image> childImages = new List<Image>();

    private Coroutine slideshowRoutine;

    private void OnValidate()
    {
        // In editor, keep this list synced to child Images
        AutoCollectChildImages();
    }

    private void Awake()
    {
        // Safety: in case object is instantiated at runtime and OnValidate didn't run
        if (childImages == null || childImages.Count == 0)
        {
            AutoCollectChildImages();
        }
    }

    private void OnEnable()
    {
        // Do not start slideshow in edit mode (Scene view) – only in play
        if (!Application.isPlaying)
            return;

        if (slideshowRoutine != null)
        {
            StopCoroutine(slideshowRoutine);
        }

        // Clean nulls once at start
        if (childImages == null)
            childImages = new List<Image>();

        childImages.RemoveAll(img => img == null);

        if (childImages.Count == 0 || totalDuration <= 0f)
            return;

        slideshowRoutine = StartCoroutine(SlideshowCoroutine());
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        if (slideshowRoutine != null)
        {
            StopCoroutine(slideshowRoutine);
            slideshowRoutine = null;
        }
    }

    private void AutoCollectChildImages()
    {
        if (childImages == null)
            childImages = new List<Image>();
        else
            childImages.Clear();

        // Get all child Images, including inactive ones
        var imgs = GetComponentsInChildren<Image>(true);

        foreach (var img in imgs)
        {
            // Skip the root's own Image if it has one – only true children
            if (img.gameObject == gameObject)
                continue;

            childImages.Add(img);
        }
    }

    private IEnumerator SlideshowCoroutine()
    {
        int slideCount = childImages.Count;
        if (slideCount == 0)
            yield break;

        // Handle edge case: only one image -> just show it, no fading
        if (slideCount == 1)
        {
            ResetAllImages();
            var only = childImages[0];
            if (only != null)
            {
                only.gameObject.SetActive(true);
                only.canvasRenderer.SetAlpha(1f);
            }
            yield break;
        }

        ResetAllImages();

        // Timing per slide based on total duration
        float timePerImage = totalDuration / slideCount;
        fadeDuration = Mathf.Min(fadeDuration, timePerImage);
        float holdTime = Mathf.Max(0f, timePerImage - fadeDuration);

        int currentIndex = 0;
        Image current = childImages[currentIndex];

        // Setup first image
        current.gameObject.SetActive(true);
        current.canvasRenderer.SetAlpha(1f);

        while (true)
        {
            int nextIndex = currentIndex + 1;
            if (nextIndex >= slideCount)
            {
                if (!loop)
                    yield break;

                nextIndex = 0; // wrap
            }

            Image next = childImages[nextIndex];

            // Ensure the next image is active and fully transparent to start
            next.gameObject.SetActive(true);
            next.canvasRenderer.SetAlpha(0f);

            // Hold the current image fully visible
            if (holdTime > 0f)
                yield return new WaitForSeconds(holdTime);

            // Crossfade current -> next
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / fadeDuration);

                if (current != null)
                    current.canvasRenderer.SetAlpha(1f - a);
                if (next != null)
                    next.canvasRenderer.SetAlpha(a);

                yield return null;
            }

            // Snap to final values
            if (current != null)
            {
                current.canvasRenderer.SetAlpha(0f);
                current.gameObject.SetActive(false);
            }

            if (next != null)
                next.canvasRenderer.SetAlpha(1f);

            currentIndex = nextIndex;
            current = next;
        }
    }

    private void ResetAllImages()
    {
        if (childImages == null) return;

        foreach (var img in childImages)
        {
            if (img == null) continue;

            img.gameObject.SetActive(false);
            img.canvasRenderer.SetAlpha(0f);
        }
    }
}
