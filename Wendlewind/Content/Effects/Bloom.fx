#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0
    #define PS_SHADERMODEL ps_4_0
#endif

// =============================================================================
// BLOOM SHADER
// Two-pass Gaussian blur for creating bloom/glow post-processing effects.
// Use for lightning flashes, bright particles, etc.
// =============================================================================

// Shader parameters
float2 BlurDirection;       // (1,0) for horizontal, (0,1) for vertical
float2 TexelSize;           // 1.0 / texture dimensions
float BloomIntensity;       // Multiplier for bloom brightness
float BloomThreshold;       // Minimum brightness to bloom

sampler2D SceneSampler : register(s0);

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

// Extract bright pixels for blooming
float4 BrightPassPS(VertexShaderOutput input) : COLOR
{
    float4 color = tex2D(SceneSampler, input.TexCoord);
    
    // Calculate luminance
    float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));
    
    // Extract only pixels above threshold
    float bloomAmount = saturate((luminance - BloomThreshold) / (1.0 - BloomThreshold));
    
    return color * bloomAmount * BloomIntensity;
}

// Gaussian blur pass
float4 BlurPS(VertexShaderOutput input) : COLOR
{
    // 9-tap Gaussian weights
    float weights[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };
    
    float4 result = tex2D(SceneSampler, input.TexCoord) * weights[0];
    
    for (int i = 1; i < 5; i++)
    {
        float2 offset = BlurDirection * TexelSize * i;
        result += tex2D(SceneSampler, input.TexCoord + offset) * weights[i];
        result += tex2D(SceneSampler, input.TexCoord - offset) * weights[i];
    }
    
    return result * input.Color;
}

// Combine original scene with bloom
float4 CombinePS(VertexShaderOutput input) : COLOR
{
    float4 original = tex2D(SceneSampler, input.TexCoord);
    // The bloom texture would be passed as a second sampler in practice
    // For now, just apply intensity
    return original * input.Color;
}

technique BrightPass
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL BrightPassPS();
    }
}

technique GaussianBlur
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL BlurPS();
    }
}

technique Combine
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL CombinePS();
    }
}



