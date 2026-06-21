/*
 * Texture3DShader.cs - Shader for textured 3D objects
 *
 * Copyright (C) 2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of Amber.
 *
 * Amber is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Amber is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Amber. If not, see <http://www.gnu.org/licenses/>.
 */

namespace Amber.Renderer.OpenGL.Shaders;

using Amber.Common;
using Amber.Renderer.OpenGL.Buffers;
using static Shader;

internal class Texture3DShader : BaseShader, IPaletteShader
{
    // The palette has a size of 32xNumPalettes pixels.
    // Each row represents one palette of 32 colors.
    // So the palette index determines the pixel row.
    // The column is the palette color index from 0 to 31.
    protected static string TextureFragmentShader(State state) => GetFragmentShaderHeader(state) + $@"
        uniform float {UsePaletteName};
        uniform float {PaletteSizeName};
        uniform float {PaletteCountName};
        uniform sampler2D {TextureName};
        uniform sampler2D {PaletteName};
        uniform float {AllowTransparencyName};
        uniform float {AllowAlphaName};
        uniform float {LightIntensityName};
        uniform vec4 {ColorReplacementName}[16];
        uniform float {AllowColorReplacementName};
        uniform float {SkyColorIndexName};
        uniform vec4 {SkyReplacementColorName};
        uniform vec4 {FogColorName};
        uniform float {FogStartDistanceName};
        uniform float {FogEndDistanceName};
        uniform float {FogEnabledName};
        uniform float {FadeFactorName};
        uniform vec4 {TintColorName};
        in vec2 varTexCoord;
        in float varViewDistance;
        flat in float varPaletteIndex;
        flat in vec2 varTextureEndCoord;
        flat in float varAlphaEnabled;

        vec4 applyLight(vec4 color)
        {{
            return vec4(max(vec3(0), color.rgb + vec3({LightIntensityName}) - vec3(1)), color.a);
        }}
       
        void main()
        {{
            vec4 pixelColor = vec4(0.0f);

            if ({UsePaletteName} > 0.5f)
            {{
                float colorIndex = texture({TextureName}, varTexCoord).r * 255.0f;

                if ({AllowTransparencyName} > 0.5f && colorIndex < 0.5f)
                    discard;

                if (abs(colorIndex - {SkyColorIndexName}) < 0.5f)
                    pixelColor = {SkyReplacementColorName};           
                else
                {{
                    pixelColor = {AllowColorReplacementName} > 0.5f && colorIndex < 15.5f
                        ? {ColorReplacementName}[int(colorIndex + 0.5f)]
                        : texture({PaletteName}, vec2((colorIndex + 0.5f) / {PaletteSizeName}, (varPaletteIndex + 0.5f) / {PaletteCountName}));
                    pixelColor = applyLight(pixelColor);
                }}
            }}
            else
            {{
                pixelColor = texture({TextureName}, varTexCoord);
                pixelColor = applyLight(pixelColor);
            }}

            if ({AllowAlphaName} > 0.5f && varAlphaEnabled > 0.5f)
            {{
                if (pixelColor.a < 0.5f || {LightIntensityName} < 0.01f)
                    discard;

                pixelColor *= {TintColorName};
            }}
            else
            {{
                pixelColor.a = 1.0f;
                pixelColor.rgb *= {TintColorName}.rgb;
            }}
            
            if ({FogEnabledName} > 0.5f && {FogColorName}.a > 0.001f)
            {{
                float fogFactor = clamp(
                    (varViewDistance - {FogStartDistanceName}) / ({FogEndDistanceName} - {FogStartDistanceName}),
                    0.0f,
                    1.0f
                );

                pixelColor = mix(pixelColor, {FogColorName}, fogFactor);
            }}
            if ({FadeFactorName} < 0.9999f)
            {{
                pixelColor = pixelColor * {FadeFactorName};
            }}

            {FragmentOutColorName} = pixelColor;
        }}
    ";

    protected static string TextureVertexShader(State state) => GetVertexShaderHeader(state) + $@"
        in vec3 {PositionName};
        in ivec2 {TextureCoordName};
        in uint {PaletteIndexName};
        in uint {AlphaName};
        uniform uvec2 {AtlasSizeName};
        uniform mat4 {ProjectionMatrixName};
        uniform mat4 {ModelViewMatrixName};
        out vec2 varTexCoord;
        out float varViewDistance;
        flat out float varPaletteIndex;
        flat out float varAlphaEnabled;
        
        void main()
        {{
            vec2 atlasFactor = vec2(1.0f / float({AtlasSizeName}.x), 1.0f / float({AtlasSizeName}.y));
            varTexCoord = atlasFactor * vec2(float({TextureCoordName}.x), float({TextureCoordName}.y));
            varPaletteIndex = float({PaletteIndexName});
            varAlphaEnabled = float({AlphaName});
            gl_Position = {ProjectionMatrixName} * {ModelViewMatrixName} * vec4({PositionName}, 1.0f);
            
            vec4 viewPos = {ModelViewMatrixName} * vec4({PositionName}, 1.0f);
            varViewDistance = length(viewPos.xyz);
        }}
    ";

    public override ShaderProjection Projection => ShaderProjection.Perspective;

    public Texture3DShader(State state)
        : this(state, TextureFragmentShader(state), TextureVertexShader(state))
    {

    }

    protected Texture3DShader(State state, string fragmentShaderCode, string vertexShaderCode)
        : base(state, fragmentShaderCode, vertexShaderCode)
    {

    }

    public override Dictionary<BufferPurpose, IBuffer> SetupBuffers(VertexArrayObject vertexArrayObject)
    {
        var buffers = new Dictionary<BufferPurpose, IBuffer>();

        void Add(BufferPurpose purpose, string name, IBuffer buffer)
        {
            vertexArrayObject.AddBuffer(name, buffer);
            buffers.Add(purpose, buffer);
        }

        Add(BufferPurpose.Position3D, PositionName, new VectorBuffer(State, false));
        Add(BufferPurpose.TextureCoordinates, TextureCoordName, new PositionBuffer(State, false));
        Add(BufferPurpose.PaletteIndex, PaletteIndexName, new ByteBuffer(State, true));
        Add(BufferPurpose.Alpha, AlphaName, new ByteBuffer(State, false));

        return buffers;
    }

    public void UsePalette(bool use)
    {
        shaderProgram.SetInput(UsePaletteName, use ? 1.0f : 0.0f);
    }

    public void SetTexture(int textureUnit = 0)
    {
        shaderProgram.SetInput(TextureName, textureUnit);
    }

    public void SetPalette(int textureUnit = 1)
    {
        shaderProgram.SetInput(PaletteName, textureUnit);
    }

    public void SetAtlasSize(uint width, uint height)
    {
        shaderProgram.SetInputVector2(AtlasSizeName, width, height);
    }

    public void SetPaletteSize(int size)
    {
        shaderProgram.SetInput(PaletteSizeName, (float)size);
    }

    public void SetPaletteCount(int count)
    {
        shaderProgram.SetInput(PaletteCountName, (float)count);
    }

    public void AllowTransparency(bool allow)
    {
        shaderProgram.SetInput(AllowTransparencyName, allow ? 1.0f : 0.0f);
    }

    public void AllowAlpha(bool allow)
    {
        shaderProgram.SetInput(AllowAlphaName, allow ? 1.0f : 0.0f);
    }

    public void AllowColorReplacement(bool allow)
    {
        shaderProgram.SetInput(AllowColorReplacementName, allow ? 1.0f : 0.0f);
    }

    public void SetLightIntensity(float lightIntensity)
    {
        shaderProgram.SetInput(LightIntensityName, lightIntensity);
    }

    // 16 colors = 64 bytes
    public void SetReplacementColors(Color[] replacementColors)
    {
        shaderProgram.SetInputColorArray(ColorReplacementName, replacementColors);
    }

    public void SetSkyColorIndex(int skyColorIndex)
    {
        shaderProgram.SetInput(SkyColorIndexName, (float)skyColorIndex);
    }

    public void SetSkyReplacementColor(Color skyColor)
    {
        shaderProgram.SetInputColor(SkyReplacementColorName, skyColor);
    }

    public void SetFogColor(Color fogColor)
    {
        shaderProgram.SetInputColor(FogColorName, fogColor);
    }

    /// <summary>
    /// The distance when the fog starts.
    /// </summary>
    public void SetFogStartDistance(float distance)
    {
        shaderProgram.SetInput(FogStartDistanceName, distance);
    }

    /// <summary>
    /// The distance when the fog will become totally opaque.
    /// </summary>
    public void SetFogEndDistance(float distance)
    {
        shaderProgram.SetInput(FogEndDistanceName, distance);
    }

    public void EnableFog(bool enable)
    {
        shaderProgram.SetInput(FogEnabledName, enable ? 1.0f : 0.0f);
    }

    public void SetFadeFactor(float factor)
    {
        shaderProgram.SetInput(FadeFactorName, factor);
    }

    public void SetTintColor(Color color)
    {
        shaderProgram.SetInputColor(TintColorName, color);
    }
}
