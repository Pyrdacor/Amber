﻿/*
 * TextureShader.cs - Shader for textured 2D sprites
 *
 * Copyright (C) 2024  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using static Shader;

internal class TextureShader : BaseShader
{
    // The palette has a size of 32xNumPalettes pixels.
    // Each row represents one palette of 32 colors.
    // So the palette index determines the pixel row.
    // The column is the palette color index from 0 to 31.
    protected static string TextureFragmentShader(State state) => GetFragmentShaderHeader(state) + $@"
        uniform float {UsePaletteName};
        uniform float {PaletteCountName};
        uniform sampler2D {SamplerName};
        uniform sampler2D {PaletteName};
        uniform float {ColorKeyName};
        in vec2 varTexCoord;
        flat in float palIndex;
        flat in float maskColIndex;
        
        void main()
        {{
            vec4 pixelColor = vec4(0);
            
            if ({UsePaletteName} > 0.5f)
            {{
                float colorIndex = texture({SamplerName}, varTexCoord).r * 255.0f;
                
                if (colorIndex < 0.5f)
                    discard;
                else
                {{
                    if (colorIndex >= 31.5f)
                        colorIndex = 0.0f;
                    pixelColor = texture({PaletteName}, vec2((colorIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {PaletteCountName}));
                }}
            }}
            else
            {{
                pixelColor = texture({SamplerName}, varTexCoord);
                if (pixelColor.a < 0.5f)
                    discard;
            }}
            
            if (maskColIndex < 0.5f)
                {FragmentOutColorName} = pixelColor;
            else
                {FragmentOutColorName} = texture({PaletteName}, vec2((maskColIndex + 0.5f) / 32.0f, (palIndex + 0.5f) / {PaletteCountName}));
        }}
    ";

    protected static string TextureVertexShader(State state) => GetVertexShaderHeader(state) + $@"
        in vec2 {PositionName};
        in ivec2 {TexCoordName};
        in uint {LayerName};
        in uint {PaletteIndexName};
        in uint {MaskColorIndexName};
        uniform uvec2 {AtlasSizeName};
        uniform float {ZName};
        uniform mat4 {ProjectionMatrixName};
        uniform mat4 {ModelViewMatrixName};
        out vec2 varTexCoord;
        flat out float palIndex;
        flat out float maskColIndex;
        
        void main()
        {{
            vec2 atlasFactor = vec2(1.0f / float({AtlasSizeName}.x), 1.0f / float({AtlasSizeName}.y));
            vec2 pos = vec2({PositionName}.x + 0.49f, {PositionName}.y + 0.49f);
            varTexCoord = atlasFactor * vec2({TexCoordName}.x, {TexCoordName}.y);
            palIndex = float({PaletteIndexName});
            maskColIndex = float({MaskColorIndexName});
            float z = 1.0f - {ZName} - float({LayerName}) * 0.00001f;
            gl_Position = {ProjectionMatrixName} * {ModelViewMatrixName} * vec4(pos, z, 1.0f);
        }}
    ";

    TextureShader(State state)
        : this(state, TextureFragmentShader(state), TextureVertexShader(state))
    {

    }

    protected TextureShader(State state, string fragmentShaderCode, string vertexShaderCode)
        : base(state, fragmentShaderCode, vertexShaderCode)
    {

    }

    public void UsePalette(bool use)
    {
        shaderProgram.SetInput(UsePaletteName, use ? 1.0f : 0.0f);
    }

    public void SetSampler(int textureUnit = 0)
    {
        shaderProgram.SetInput(SamplerName, textureUnit);
    }

    public void SetPalette(int textureUnit = 1)
    {
        shaderProgram.SetInput(PaletteName, textureUnit);
    }

    public void SetAtlasSize(uint width, uint height)
    {
        shaderProgram.SetInputVector2(AtlasSizeName, width, height);
    }

    public void SetColorKey(byte colorIndex)
    {
        if (colorIndex > 31)
            throw new AmberException(ExceptionScope.Render, "Color index must be in the range 0 to 31.");

        shaderProgram.SetInput(ColorKeyName, (float)colorIndex);
    }

    public void SetPaletteCount(int count)
    {
        shaderProgram.SetInput(PaletteCountName, (float)count);
    }

    public new static IShader Create(State state) => new TextureShader(state);
}
