using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

// This attribute links our custom editor to the HueShiftPaletteGenerator script
[CustomEditor(typeof(HueShiftPaletteGenerator))]
public class HueShiftPaletteGeneratorEditor : Editor
{
    private HueShiftPaletteGenerator generator; // Reference to the target script
    private Color[] testBaseColors; // An array to store our predefined test base colors

    // SerializedProperty references for efficient drawing and undo/redo
    private SerializedProperty baseColorProp;
    private SerializedProperty minLuminanceProp;
    private SerializedProperty maxLuminanceProp;
    private SerializedProperty numberOfStepsProp;

    private SerializedProperty useSmoothstepForLuminanceShiftProp;
    private SerializedProperty hueShiftByLuminanceCurveProp;
    private SerializedProperty smoothstepStartLuminanceProp;
    private SerializedProperty smoothstepEndLuminanceProp;
    private SerializedProperty maxHueShiftDegreesProp;

    private SerializedProperty huePivotDegreesProp;
    private SerializedProperty hueShiftByHueRangeCurveProp;

    // Saturation adjustment removed as per request
    // private SerializedProperty saturationAdjustmentFactorProp; 

    private SerializedProperty generatedPaletteProp;
    private SerializedProperty colorDisplayImagesProp;

    // For the custom visualizations
    private Texture2D originalHueSpectrumTexture; // Horizontal: Hue, Vertical: Value (0-1)
    private Texture2D shiftedHueSpectrumTexture; // Horizontal: Hue, Vertical: Value (0-1)
    private Texture2D hueRangeCurveOverlayTexture; // New texture for the curve drawn on the original hue bar

    private const int PREVIEW_TEXTURE_WIDTH = 256; // Standard width for clarity
    private const int SPECTRUM_TEXTURE_HEIGHT = 80; // Height for the Hue-Value spectrum (Y-axis for Value)
    private const int HUE_RANGE_CURVE_OVERLAY_HEIGHT = 40; // Height for the hue range curve visualization

    // Called when the editor becomes active (e.g., when the GameObject is selected)
    private void OnEnable()
    {
        generator = (HueShiftPaletteGenerator)target;

        // Get SerializedProperty references for all fields
        baseColorProp = serializedObject.FindProperty("baseColor");
        minLuminanceProp = serializedObject.FindProperty("minLuminance");
        maxLuminanceProp = serializedObject.FindProperty("maxLuminance");
        numberOfStepsProp = serializedObject.FindProperty("numberOfSteps");
        useSmoothstepForLuminanceShiftProp = serializedObject.FindProperty("useSmoothstepForLuminanceShift");
        hueShiftByLuminanceCurveProp = serializedObject.FindProperty("hueShiftByLuminanceCurve");
        smoothstepStartLuminanceProp = serializedObject.FindProperty("smoothstepStartLuminance");
        smoothstepEndLuminanceProp = serializedObject.FindProperty("smoothstepEndLuminance");
        maxHueShiftDegreesProp = serializedObject.FindProperty("maxHueShiftDegrees");
        huePivotDegreesProp = serializedObject.FindProperty("huePivotDegrees");
        hueShiftByHueRangeCurveProp = serializedObject.FindProperty("hueShiftByHueRangeCurve");
        // saturationAdjustmentFactorProp = serializedObject.FindProperty("saturationAdjustmentFactor"); // Removed
        generatedPaletteProp = serializedObject.FindProperty("generatedPalette");
        colorDisplayImagesProp = serializedObject.FindProperty("colorDisplayImages");

        // Define 10 test base hues, evenly spaced, to demonstrate the shifts
        testBaseColors = new Color[10];
        for (int i = 0; i < 10; i++)
        {
            float h = (float)i / 10.0f; // Evenly spaced hue from 0 to 0.9
            testBaseColors[i] = Color.HSVToRGB(h, 0.9f, 0.9f); // High saturation, high value for visibility
        }

        // Initialize preview textures
        GenerateVisualizationsTextures();
    }

    // Called when the inspector window is closed or the object is deselected
    private void OnDisable()
    {
        // Clean up textures created with new Texture2D
        if (originalHueSpectrumTexture != null) DestroyImmediate(originalHueSpectrumTexture);
        if (shiftedHueSpectrumTexture != null) DestroyImmediate(shiftedHueSpectrumTexture);
        if (hueRangeCurveOverlayTexture != null) DestroyImmediate(hueRangeCurveOverlayTexture);
        
        originalHueSpectrumTexture = null;
        shiftedHueSpectrumTexture = null;
        hueRangeCurveOverlayTexture = null;
    }

    // Generates the continuous hue gradients and the curve overlay for visualization
    private void GenerateVisualizationsTextures()
    {
        // --- Original Hue-Luminance Spectrum Texture ---
        if (originalHueSpectrumTexture == null)
            originalHueSpectrumTexture = new Texture2D(PREVIEW_TEXTURE_WIDTH, SPECTRUM_TEXTURE_HEIGHT, TextureFormat.RGB24, false);
        else
            originalHueSpectrumTexture.Reinitialize(PREVIEW_TEXTURE_WIDTH, SPECTRUM_TEXTURE_HEIGHT);

        for (int y = 0; y < SPECTRUM_TEXTURE_HEIGHT; y++)
        {
            // Y goes from 0 (top of texture, V=1) to height-1 (bottom, V=0)
            float v_normalized = 1.0f - ((float)y / (SPECTRUM_TEXTURE_HEIGHT - 1)); 
            for (int x = 0; x < PREVIEW_TEXTURE_WIDTH; x++)
            {
                float h_normalized = (float)x / (PREVIEW_TEXTURE_WIDTH - 1);
                originalHueSpectrumTexture.SetPixel(x, y, Color.HSVToRGB(h_normalized, 1.0f, v_normalized));
            }
        }
        originalHueSpectrumTexture.Apply();

        // --- Shifted Hue-Luminance Spectrum Texture ---
        if (shiftedHueSpectrumTexture == null)
            shiftedHueSpectrumTexture = new Texture2D(PREVIEW_TEXTURE_WIDTH, SPECTRUM_TEXTURE_HEIGHT, TextureFormat.RGB24, false);
        else
            shiftedHueSpectrumTexture.Reinitialize(PREVIEW_TEXTURE_WIDTH, SPECTRUM_TEXTURE_HEIGHT);

        for (int y = 0; y < SPECTRUM_TEXTURE_HEIGHT; y++)
        {
            float v_normalized = 1.0f - ((float)y / (SPECTRUM_TEXTURE_HEIGHT - 1));
            for (int x = 0; x < PREVIEW_TEXTURE_WIDTH; x++)
            {
                float h_normalized = (float)x / (PREVIEW_TEXTURE_WIDTH - 1);

                // Apply the hue shift logic from HueShiftPaletteGenerator for this H and V
                float currentHue_degrees = h_normalized * 360f;

                float luminanceShiftMultiplier;
                if (generator.useSmoothstepForLuminanceShift)
                {
                    luminanceShiftMultiplier = 1.0f - Mathf.SmoothStep(generator.smoothstepStartLuminance, generator.smoothstepEndLuminance, v_normalized);
                }
                else
                {
                    luminanceShiftMultiplier = generator.hueShiftByLuminanceCurve.Evaluate(v_normalized);
                }
                float currentLuminanceHueShiftMagnitude = luminanceShiftMultiplier * generator.maxHueShiftDegrees;

                float rawDirectionalShift = Mathf.Sin((currentHue_degrees - generator.huePivotDegrees) * Mathf.Deg2Rad);
                float hueRangeMultiplier = generator.hueShiftByHueRangeCurve.Evaluate(h_normalized);
                float hueRangeAdjustedDirectionalShift = rawDirectionalShift * hueRangeMultiplier;

                float finalHueShiftDegrees = currentLuminanceHueShiftMagnitude * hueRangeAdjustedDirectionalShift;

                float newHue_degrees = currentHue_degrees + finalHueShiftDegrees;
                newHue_degrees = newHue_degrees % 360f;
                if (newHue_degrees < 0) newHue_degrees += 360f;
                float newHue_normalized = newHue_degrees / 360f;

                // For visualization, keep saturation high (1.0f) to clearly see hue/value changes
                shiftedHueSpectrumTexture.SetPixel(x, y, Color.HSVToRGB(newHue_normalized, 1.0f, v_normalized));
            }
        }
        shiftedHueSpectrumTexture.Apply();


        // --- Hue Range Curve Overlay Texture ---
        if (hueRangeCurveOverlayTexture == null)
            hueRangeCurveOverlayTexture = new Texture2D(PREVIEW_TEXTURE_WIDTH, HUE_RANGE_CURVE_OVERLAY_HEIGHT, TextureFormat.ARGB32, false);
        else
            hueRangeCurveOverlayTexture.Reinitialize(PREVIEW_TEXTURE_WIDTH, HUE_RANGE_CURVE_OVERLAY_HEIGHT);

        // Fill with clear pixels
        Color[] clearPixels = new Color[PREVIEW_TEXTURE_WIDTH * HUE_RANGE_CURVE_OVERLAY_HEIGHT];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = Color.clear;
        hueRangeCurveOverlayTexture.SetPixels(clearPixels);

        // Calculate a reasonable Y-axis scale for the curve based on its actual max value
        float maxMultiplierValue = 1.5f; // Default assumption for Y-axis scale if curve is flat or low
        if (generator.hueShiftByHueRangeCurve.keys.Length > 0)
        {
            foreach(var key in generator.hueShiftByHueRangeCurve.keys)
            {
                if (key.value > maxMultiplierValue) maxMultiplierValue = key.value;
            }
        }
        maxMultiplierValue = Mathf.Max(1.0f, maxMultiplierValue); // Ensure it's at least 1 for proper scaling

        // Draw the curve onto the texture using discrete points and connecting them
        Vector2 prevPixelPoint = Vector2.zero;
        for (int x = 0; x < PREVIEW_TEXTURE_WIDTH; x++)
        {
            float h_normalized = (float)x / (PREVIEW_TEXTURE_WIDTH - 1);
            float multiplier = generator.hueShiftByHueRangeCurve.Evaluate(h_normalized);
            
            // Map multiplier [0, maxMultiplierValue] to Y-pixel [HEIGHT-1, 0]
            // We want Y=0 for max multiplier (top of texture), Y=HEIGHT-1 for min multiplier (bottom)
            int y_pixel = (int)(HUE_RANGE_CURVE_OVERLAY_HEIGHT - 1 - (multiplier / maxMultiplierValue) * (HUE_RANGE_CURVE_OVERLAY_HEIGHT - 1));
            y_pixel = Mathf.Clamp(y_pixel, 0, HUE_RANGE_CURVE_OVERLAY_HEIGHT - 1); // Clamp to texture bounds

            Vector2 currentPixelPoint = new Vector2(x, y_pixel);

            if (x > 0)
            {
                DrawLine(hueRangeCurveOverlayTexture, (int)prevPixelPoint.x, (int)prevPixelPoint.y, (int)currentPixelPoint.x, (int)currentPixelPoint.y, Color.red);
            }
            prevPixelPoint = currentPixelPoint;
        }
        hueRangeCurveOverlayTexture.Apply();
    }

    // Helper function to draw a line in a Texture2D using Bresenham's algorithm (improved for single-pixel lines)
    private void DrawLine(Texture2D tex, int x1, int y1, int x2, int y2, Color col)
    {
        // For drawing 1-pixel wide lines, Bresenham's is fine.
        // For thicker or anti-aliased lines, a more complex algorithm or drawing multiple lines would be needed.

        bool steep = Mathf.Abs(y2 - y1) > Mathf.Abs(x2 - x1);
        if (steep)
        {
            // Swap x and y
            int temp = x1; x1 = y1; y1 = temp;
            temp = x2; x2 = y2; y2 = temp;
        }

        if (x1 > x2)
        {
            // Swap points to draw from left to right
            int temp = x1; x1 = x2; x2 = temp;
            temp = y1; y1 = y2; y2 = temp;
        }

        int dx = x2 - x1;
        int dy = Mathf.Abs(y2 - y1);
        int error = dx / 2;
        int ystep = (y1 < y2) ? 1 : -1;
        int y = y1;

        for (int x = x1; x <= x2; x++)
        {
            if (steep)
            {
                if (y >= 0 && y < tex.width && x >= 0 && x < tex.height) // Check bounds for swapped coords
                    tex.SetPixel(y, x, col);
            }
            else
            {
                if (x >= 0 && x < tex.width && y >= 0 && y < tex.height) // Check bounds for normal coords
                    tex.SetPixel(x, y, col);
            }

            error -= dy;
            if (error < 0)
            {
                y += ystep;
                error += dx;
            }
        }
    }


    // This is where we draw the custom Inspector GUI
    public override void OnInspectorGUI()
    {
        // Store current GUI.changed state
        bool guiChangedInitial = GUI.changed;

        // Always call Update() at the beginning of OnInspectorGUI.
        // This syncs the SerializedObject with the actual object's data.
        serializedObject.Update();

        // --- Draw specific properties using SerializedProperty for better undo/redo ---
        EditorGUILayout.PropertyField(baseColorProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Luminance Steps (Value in HSV)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(minLuminanceProp);
        EditorGUILayout.PropertyField(maxLuminanceProp);
        EditorGUILayout.PropertyField(numberOfStepsProp);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hue Shift over Luminance", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(useSmoothstepForLuminanceShiftProp);

        if (useSmoothstepForLuminanceShiftProp.boolValue)
        {
            EditorGUILayout.PropertyField(smoothstepStartLuminanceProp);
            EditorGUILayout.PropertyField(smoothstepEndLuminanceProp);
            EditorGUILayout.HelpBox("Shift will be full below 'Start Luminance', smoothly fading to no shift at 'End Luminance'.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.PropertyField(hueShiftByLuminanceCurveProp);
            EditorGUILayout.HelpBox("Curve X-axis: Target Luminance (Value, 0-1). Y-axis: Multiplier for Max Hue Shift (0-1).", MessageType.Info);
        }
        EditorGUILayout.PropertyField(maxHueShiftDegreesProp);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hue Shift over Hue Range", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(huePivotDegreesProp);
        EditorGUILayout.PropertyField(hueShiftByHueRangeCurveProp);
        EditorGUILayout.HelpBox("Curve X-axis: Original Hue (0-1). Y-axis: Multiplier for sine wave effect (0-1+).", MessageType.Info);


        // Saturation adjustment removed as per request (section commented out)
        /*
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Saturation Adjustment", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(saturationAdjustmentFactorProp);
        */

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Generated Palette", EditorStyles.boldLabel);
        // Draw the generated palette array as read-only
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(generatedPaletteProp, true); // true for foldout
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("UI Display (Optional)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(colorDisplayImagesProp, true); // true for foldout


        EditorGUILayout.Space(20); // Add some space for separation

        // --- Hue Shift Effect Overview Visualization ---
        EditorGUILayout.LabelField("Hue Shift Effect Overview", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Top bar: Original Hues from full brightness (top) to black (bottom). " +
                                "Bottom bar: Shifted Hues based on your settings. " +
                                "Vertical white line: Hue Pivot. Red Curve: Hue Shift Range Multiplier (relative scale).", MessageType.Info);
        
        // Reserve space for the gradient textures and the curve overlay
        float totalHeightForOverview = SPECTRUM_TEXTURE_HEIGHT * 2 + HUE_RANGE_CURVE_OVERLAY_HEIGHT + 20; // 2 spectrums + curve overlay + spacing
        Rect overviewRect = GUILayoutUtility.GetRect(PREVIEW_TEXTURE_WIDTH, totalHeightForOverview, GUILayout.ExpandWidth(true));
        
        // --- Draw Original Hue-Luminance Spectrum ---
        Rect originalSpectrumRect = new Rect(overviewRect.x, overviewRect.y, overviewRect.width, SPECTRUM_TEXTURE_HEIGHT);
        GUI.DrawTexture(originalSpectrumRect, originalHueSpectrumTexture, ScaleMode.StretchToFill);

        // Draw the hue pivot line on the original spectrum
        float pivotX_original = originalSpectrumRect.x + (generator.huePivotDegrees / 360f) * originalSpectrumRect.width;
        EditorGUI.DrawRect(new Rect(pivotX_original - 1, originalSpectrumRect.y, 2, originalSpectrumRect.height), Color.white);

        // --- Draw Hue Range Curve Overlay ---
        // Position this overlay on top of the original spectrum bar, aligned to its top left
        Rect curveOverlayRect = new Rect(originalSpectrumRect.x, originalSpectrumRect.y, originalSpectrumRect.width, HUE_RANGE_CURVE_OVERLAY_HEIGHT);
        GUI.DrawTexture(curveOverlayRect, hueRangeCurveOverlayTexture, ScaleMode.StretchToFill);


        // --- Draw Shifted Hue-Luminance Spectrum ---
        // Position below the original spectrum and the curve overlay
        Rect shiftedSpectrumRect = new Rect(overviewRect.x, originalSpectrumRect.yMax + HUE_RANGE_CURVE_OVERLAY_HEIGHT + 10, overviewRect.width, SPECTRUM_TEXTURE_HEIGHT);
        GUI.DrawTexture(shiftedSpectrumRect, shiftedHueSpectrumTexture, ScaleMode.StretchToFill);
        
        // Draw the hue pivot line on the shifted spectrum
        float pivotX_shifted = shiftedSpectrumRect.x + (generator.huePivotDegrees / 360f) * shiftedSpectrumRect.width;
        EditorGUI.DrawRect(new Rect(pivotX_shifted - 1, shiftedSpectrumRect.y, 2, shiftedSpectrumRect.height), Color.white);
        

        EditorGUILayout.Space(20); // Add some space for separation

        // --- Test Palette Visualization (10 hues, 10 steps) ---
        EditorGUILayout.LabelField("Test Palette Visualization", EditorStyles.boldLabel); // Corrected to boldLabel
        EditorGUILayout.HelpBox("This section shows how different starting hues (rows) are affected by the palette generation. " +
                                "The columns represent steps from min to max luminance (Value).", MessageType.Info);

        EditorGUILayout.Space();

        // Store the original baseColor and numberOfSteps to restore them later, preventing side effects
        Color originalBaseColor = generator.baseColor;
        int originalNumberOfSteps = generator.numberOfSteps;
        
        // Define number of steps for visualization (can be different from generator.numberOfSteps)
        // Let's use 10 steps as requested for the visualization
        int visualizationSteps = 10; 
        
        // Temporarily set the generator's numberOfSteps for the visualization palette generation
        // This ensures the visualization always uses 10 steps, regardless of the main setting.
        generator.numberOfSteps = visualizationSteps;

        // Loop through each test base hue
        foreach (Color testBaseColor in testBaseColors)
        {
            // Temporarily set the generator's base color to the current test hue
            generator.baseColor = testBaseColor;
            
            // Manually call GeneratePalette. OnValidate might not fire if only baseColor is changed
            // via direct field assignment in the editor script (instead of SerializedProperty).
            // This ensures the palette is regenerated with the temporary baseColor
            generator.GeneratePalette(); 

            // Display the base hue and its generated palette steps
            EditorGUILayout.BeginHorizontal();
            {
                // Display the original base hue swatch
                DrawColorSwatch(testBaseColor, 40, 40); // Base color swatch

                // Display its HSV values for reference
                float h, s, v;
                Color.RGBToHSV(testBaseColor, out h, out s, out v);
                EditorGUILayout.LabelField(
                    $"H:{h * 360:F0} S:{s:F2} V:{v:F2}", // Display hue as degrees
                    GUILayout.Width(100));

                EditorGUILayout.Space(10); // Spacer between base color info and palette

                // Display each step of the generated palette
                if (generator.generatedPalette != null)
                {
                    // Ensure we don't go out of bounds if generatedPalette is smaller than visualizationSteps
                    for (int i = 0; i < Mathf.Min(generator.generatedPalette.Length, visualizationSteps); i++)
                    {
                        Color paletteColor = generator.generatedPalette[i];
                        DrawColorSwatch(paletteColor, 25, 25); // Palette step swatch
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2); // Small space between rows
        }

        // Restore the generator's original baseColor and numberOfSteps
        generator.baseColor = originalBaseColor;
        generator.numberOfSteps = originalNumberOfSteps;
        // Call GeneratePalette once more for the original color to ensure the main UI updates
        // and generatedPalette array reflects the true settings, not visualization settings.
        generator.GeneratePalette(); 

        // Always call ApplyModifiedProperties() at the end of OnInspectorGUI.
        // This writes the changes from the SerializedProperty back to the actual object.
        // If ApplyModifiedProperties returns true, it means a change was committed,
        // or if GUI.changed is true (e.g., AnimationCurve keys moved manually),
        // we should ensure our visualizations are also updated.
        if (serializedObject.ApplyModifiedProperties() || GUI.changed)
        {
            // This ensures textures are regenerated immediately after any property change.
            GenerateVisualizationsTextures();
            Repaint(); // Force the inspector to redraw itself
        }
    }

    // Helper function to draw a colored square swatch
    private void DrawColorSwatch(Color color, float width, float height)
    {
        Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(false));
        EditorGUI.DrawRect(rect, color);
    }
}