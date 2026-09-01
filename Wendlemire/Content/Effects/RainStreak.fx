#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0
    #define PS_SHADERMODEL ps_4_0
#endif

// =============================================================================
// RAIN STREAK SHADER
// Creates smooth gradient rain/snow streaks with motion blur effect.
// Renders elongated particles with soft edges and leading-edge brightness.
// =============================================================================

// Shader parameters
float4 StreakColor;         // Base color of the streak (RGBA)
float4 TipColor;            // Color at the leading tip (usually brighter)
float HeadSharpness;        // How sharp the leading edge is (1.0-3.0)
float TailFalloff;          // How gradually the tail fades (0.5-2.0)
float EdgeSoftness;         // Horizontal edge softness (0.0-1.0)

sampler2D TextureSampler : register(s0);

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 RainPS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TexCoord;
    
    // Vertical gradient: bright at top (leading edge), fading toward bottom (tail)
    // UV.y = 0 at top, 1 at bottom
    float headToTail = saturate(uv.y);
    
    // Sharp leading edge that fades to soft tail
    // Use saturate to ensure non-negative base for pow
    float verticalAlpha = pow(saturate(1.0 - headToTail), TailFalloff);
    
    // Add brightness boost at the leading tip
    float tipBoost = pow(saturate(1.0 - headToTail * HeadSharpness), 2.0);
    
    // Horizontal edge softness (fade toward left/right edges)
    // abs() ensures non-negative, saturate clamps to 0-1
    float horizontalDist = saturate(abs(uv.x - 0.5) * 2.0);
    float horizontalAlpha = 1.0 - pow(horizontalDist, 1.0 / max(EdgeSoftness, 0.1));
    horizontalAlpha = saturate(horizontalAlpha);
    
    // Combine alphas
    float alpha = verticalAlpha * horizontalAlpha;
    
    // Blend between streak color and bright tip
    float4 finalColor = lerp(StreakColor, TipColor, tipBoost * 0.5);
    finalColor.a *= alpha * input.Color.a;
    finalColor.rgb *= input.Color.rgb;
    
    return finalColor;
}

technique RainTechnique
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL RainPS();
    }
}

