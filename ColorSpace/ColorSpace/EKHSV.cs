using UnityEngine;
using UnityEngine.UI; // Required for Image components if you're using them

// Ensures this script runs in the editor, allowing real-time updates when parameters change.
[ExecuteInEditMode]
public class HueShiftPaletteGenerator : MonoBehaviour
{
    [Header("Input Color")]
    [Tooltip("The starting color from which the palette will be generated.")]
    public Color baseColor = Color.white;

    [Header("Luminance Steps (Value in HSV)")]
    [Tooltip("The minimum luminance (HSV Value) for the generated palette steps.")]
    [Range(0, 1)] public float minLuminance = 0.1f;
    [Tooltip("The maximum luminance (HSV Value) for the generated palette steps.")]
    [Range(0, 1)] public float maxLuminance = 1.0f;
    [Tooltip("The number of color steps to generate in the palette.")]
    public int numberOfSteps = 8; // Fixed at 8 as per request, but adjustable here

    [Header("Hue Shift over Luminance")]
    [Tooltip("If true, a smoothstep function will be used instead of the AnimationCurve for hue shift over luminance.")]
    public bool useSmoothstepForLuminanceShift = true;

    [Tooltip("X-axis: Target Luminance (Value, 0-1). Y-axis: Multiplier for Max Hue Shift. " +
             "A curve going from (0,1) to (1,0) will apply max shift to dark colors, no shift to bright colors.")]
    public AnimationCurve hueShiftByLuminanceCurve = AnimationCurve.Linear(0, 1, 1, 0); // Default curve

    [Tooltip("The luminance value where the smoothstep transition for max hue shift begins (full shift).")]
    [Range(0, 1)] public float smoothstepStartLuminance = 0.2f; // E.g., at V=0.2 and below, full shift
    [Tooltip("The luminance value where the smoothstep transition for max hue shift ends (no shift).")]
    [Range(0, 1)] public float smoothstepEndLuminance = 0.8f; // E.g., at V=0.8 and above, no shift

    [Tooltip("The maximum allowed hue shift in degrees (e.g., 45 degrees).")]
    [Range(0, 180)] public float maxHueShiftDegrees = 70f;

    [Header("Hue Shift over Hue Range")]
    [Tooltip("The central hue (in degrees, 0-360) around which the sine wave pivots for hue shifting. " +
             "E.g., 120 for Green. Hues clockwise will shift one way, counter-clockwise the other.")]
    [Range(0, 360)] public float huePivotDegrees = 60f;
    [Tooltip("X-axis: Original Hue (0-1). Y-axis: Multiplier for the sine wave's effect on hue shift. " +
             "Allows custom dampening/amplification for specific hue ranges (e.g., less shift for blues).")]
    public AnimationCurve hueShiftByHueRangeCurve = AnimationCurve.Linear(0, 1, 1, 1); // Default: consistent shift

    [Header("Saturation Adjustment")]
    [Tooltip("Multiplier for how much saturation changes as luminance changes. " +
             "Positive value: Saturation increases as luminance decreases (for richer darks).")]
    [Range(0, 1)] public float saturationAdjustmentFactor = 0.5f;

    [Header("Generated Palette")]
    [Tooltip("The array to store the generated colors. Visible in the Inspector.")]
    public Color[] generatedPalette;

    [Header("UI Display (Optional)")]
    [Tooltip("Assign UI Image components here to display the generated colors in your scene. Can be less than numberOfSteps.")]
    public Image[] colorDisplayImages; 

    // Called when the script is loaded or a value is changed in the Inspector.
    // This allows real-time updates in the editor.
    private void OnValidate()
    {
        // Ensure smoothstep start/end makes sense
        if (smoothstepStartLuminance > smoothstepEndLuminance)
        {
            // Clamp end to start if it's smaller to maintain a valid range
            smoothstepEndLuminance = smoothstepStartLuminance; 
        }
        GeneratePalette();
    }

    // Call this method to generate the palette, e.g., from a Start() method or a UI button.
    // (Still available as a ContextMenu item for manual trigger if needed, though OnValidate() handles most cases)
    [ContextMenu("Generate Palette Now")]
    public void GeneratePalette()
    {
        if (numberOfSteps <= 0)
        {
            generatedPalette = new Color[0];
            Debug.LogWarning("Number of steps must be greater than 0.");
            // Also clear UI if steps become 0
            UpdateColorDisplayImages(); 
            return;
        }

        generatedPalette = new Color[numberOfSteps];

        // Get the base H, S, V from the input color (Unity's HSV uses 0-1 range for all components)
        float baseH, baseS, baseV;
        Color.RGBToHSV(baseColor, out baseH, out baseS, out baseV);

        for (int i = 0; i < numberOfSteps; i++)
        {
            // Calculate the target Luminance (Value) for this step, evenly spaced
            // Handle single step case to prevent division by zero in (numberOfSteps - 1)
            float t = (numberOfSteps == 1) ? 0.5f : (float)i / (numberOfSteps - 1); 
            float targetV_normalized = Mathf.Lerp(minLuminance, maxLuminance, t);

            // --- HUE CALCULATION ---
            // The hue calculation for each step starts from the base hue of the input color.
            float currentHue_normalized = baseH;

            // 1. Calculate the base hue shift magnitude based on the target luminance of this step.
            //    This is where the curve vs. smoothstep logic comes in.
            float luminanceShiftMultiplier;
            if (useSmoothstepForLuminanceShift)
            {
                // We want max shift (1.0) when targetV_normalized is at or below smoothstepStartLuminance
                // and no shift (0.0) when targetV_normalized is at or above smoothstepEndLuminance.
                // 1 - SmoothStep(start, end, value) achieves this inverse behavior.
                luminanceShiftMultiplier = 1.0f - Mathf.SmoothStep(smoothstepStartLuminance, smoothstepEndLuminance, targetV_normalized);
            }
            else
            {
                // (Curve X-axis: targetV_normalized, Y-axis: multiplier [0-1] for maxHueShiftDegrees)
                luminanceShiftMultiplier = hueShiftByLuminanceCurve.Evaluate(targetV_normalized);
            }
            float currentLuminanceHueShiftMagnitude = luminanceShiftMultiplier * maxHueShiftDegrees;

            // 2. Calculate the raw directional shift based on the sine wave.
            //    This creates the "yellow to orange, blue to purple, green as center" effect.
            //    Convert normalized hue (0-1) to degrees (0-360) for sin function.
            float currentHue_degrees = currentHue_normalized * 360f;
            float rawDirectionalShift = Mathf.Sin((currentHue_degrees - huePivotDegrees) * Mathf.Deg2Rad); // Mathf.Deg2Rad converts degrees to radians

            // 3. Apply the user-defined hue range curve to modify the directional shift.
            //    (Curve X-axis: currentHue_normalized, Y-axis: multiplier [0-1] for the sine wave effect)
            float hueRangeMultiplier = hueShiftByHueRangeCurve.Evaluate(currentHue_normalized);
            float hueRangeAdjustedDirectionalShift = rawDirectionalShift * hueRangeMultiplier;

            // 4. Combine all parts to get the final hue shift in degrees.
            float finalHueShiftDegrees = currentLuminanceHueShiftMagnitude * hueRangeAdjustedDirectionalShift;

            // 5. Apply the shift to the hue and wrap it around 0-360 degrees.
            float newHue_degrees = currentHue_degrees + finalHueShiftDegrees;
            newHue_degrees = newHue_degrees % 360f;
            if (newHue_degrees < 0) newHue_degrees += 360f; // Ensure positive hue
            float newHue_normalized = newHue_degrees / 360f; // Convert back to 0-1 for HSVToRGB


            // --- SATURATION CALCULATION ---
            // Saturation typically increases as luminance decreases to maintain vibrancy in darker shades.
            // Calculate how much Value has decreased from the original Value.
            float deltaV_normalized = baseV - targetV_normalized;
            
            // Adjust saturation: If targetV is much lower than baseV, deltaV is positive, increasing saturation.
            // If targetV is higher than baseV, deltaV is negative, decreasing saturation.
            float newSaturation_normalized = baseS + (deltaV_normalized * saturationAdjustmentFactor);
            newSaturation_normalized = Mathf.Clamp01(newSaturation_normalized); // Clamp to [0, 1]


            // --- Final Color Conversion ---
            // Convert the calculated HSV values back to RGB.
            generatedPalette[i] = Color.HSVToRGB(newHue_normalized, newSaturation_normalized, targetV_normalized);
        }

        // Update UI Image components if assigned
        UpdateColorDisplayImages();
    }

    // Helper method to update assigned UI Image components
    void UpdateColorDisplayImages()
    {
        // Check if the array itself is null before trying to access its elements or length
        if (colorDisplayImages == null || generatedPalette == null) return;

        // Iterate through the display images, assigning colors up to the minimum of the two array lengths
        for (int i = 0; i < Mathf.Min(colorDisplayImages.Length, generatedPalette.Length); i++)
        {
            // Only update if the image reference at this index is not null
            if (colorDisplayImages[i] != null)
            {
                colorDisplayImages[i].color = generatedPalette[i];
            }
        }

        // If there are more display images than generated palette colors, clear the extra images
        for (int i = generatedPalette.Length; i < colorDisplayImages.Length; i++)
        {
            if (colorDisplayImages[i] != null)
            {
                // Clear extra images by setting them to a neutral color, e.g., white.
                // Using white might be less confusing than Color.clear, as clear would make them invisible.
                colorDisplayImages[i].color = Color.white; 
            }
        }
    }

    // Optional: Reset generated palette and UI when script is disabled or component is removed
    private void OnDisable()
    {
        if (colorDisplayImages != null)
        {
            foreach (Image img in colorDisplayImages)
            {
                if (img != null)
                {
                    img.color = Color.white; // Reset display images to white
                }
            }
        }
        generatedPalette = new Color[0]; // Clear generated palette in inspector
    }
}