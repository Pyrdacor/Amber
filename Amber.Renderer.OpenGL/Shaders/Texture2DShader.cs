/*
 * Texture2DShader.cs - Shader for textured 2D sprites
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

using Amber.Renderer.OpenGL.Buffers;
using static Shader;

internal class Texture2DShader : BaseShader, IPaletteShader
{
	// The palette has a size of {PaletteSizeName}x{PaletteCountName} pixels.
	// Each row represents one palette of {PaletteSizeName} colors.
	// So the palette index determines the pixel row.
	// The column is the palette color index from 0 to {PaletteSizeName}-1.
	protected static string TextureFragmentShader(State state) => GetFragmentShaderHeader(state) + $@"
        uniform float {UsePaletteName};
        uniform float {PaletteSizeName};
        uniform float {PaletteCountName};
        uniform sampler2D {TextureName};
        uniform sampler2D {PaletteName};
        uniform float {AllowTransparencyName};
        uniform float {AllowAlphaName};
        in vec2 varTexCoord;
        flat in float varPaletteIndex;
        flat in float varMaskColorIndex;
        flat in float varTransparentColorIndex;
        flat in float varNoTransparency;
        flat in float varAlpha;
        
        void main()
        {{
            vec4 pixelColor = vec4(0);
            
            if ({UsePaletteName} > 0.5f)
            {{
                float colorIndex = texture({TextureName}, varTexCoord).r * 255.0f;
                float transparentPixel = 0.0f;

                if (abs(colorIndex - varTransparentColorIndex) < 0.5f)
                    transparentPixel = 1.0f;
                
                if (transparentPixel >= 0.5f && varNoTransparency < 0.5f && {AllowTransparencyName} >= 0.5f)
                    discard;
                else
                {{
                    if (colorIndex > {PaletteSizeName} - 0.5f)
                        colorIndex = 0.0f;
                    pixelColor = texture({PaletteName}, vec2((colorIndex + 0.5f) / {PaletteSizeName}, (varPaletteIndex + 0.5f) / {PaletteCountName}));
                    if (pixelColor.a < 0.5f && varNoTransparency < 0.5f && {AllowTransparencyName} >= 0.5f)
                        discard;
                }}

                if (varMaskColorIndex < {PaletteSizeName} - 0.5f)
                    pixelColor = texture({PaletteName}, vec2((varMaskColorIndex + 0.5f) / {PaletteSizeName}, (varPaletteIndex + 0.5f) / {PaletteCountName}));
            }}
            else
            {{
                pixelColor = texture({TextureName}, varTexCoord);
                if (pixelColor.a < 0.5f && varNoTransparency < 0.5f && {AllowTransparencyName} >= 0.5f)
                    discard;
            }}

            if ({AllowAlphaName} >= 0.5f)
                pixelColor.a = varAlpha;
            else
                pixelColor.a = 1.0f;
                
            {FragmentOutColorName} = pixelColor;
        }}
    ";

    protected static string TextureVertexShader(State state) => GetVertexShaderHeader(state) + $@"
        in vec2 {PositionName};
        in ivec2 {TextureCoordName};
        in uint {LayerName};
        in uint {PaletteIndexName};
        in uint {MaskColorIndexName};
        in uint {TransparentColorIndexName};
        in uint {OpaqueName};
        in uint {AlphaName};
        uniform uvec2 {AtlasSizeName};
        uniform float {ZName};
        uniform mat4 {ProjectionMatrixName};
        uniform mat4 {ModelViewMatrixName};
        out vec2 varTexCoord;
        flat out float varPaletteIndex;
        flat out float varMaskColorIndex;
        flat out float varTransparentColorIndex;
        flat out float varNoTransparency;
        flat out float varAlpha;
        
        void main()
        {{
            vec2 atlasFactor = vec2(1.0f / float({AtlasSizeName}.x), 1.0f / float({AtlasSizeName}.y));
            vec2 pos = vec2({PositionName}.x + 0.49f, {PositionName}.y + 0.49f);
            varTexCoord = atlasFactor * vec2({TextureCoordName}.x, {TextureCoordName}.y);
            varPaletteIndex = float({PaletteIndexName});
            varMaskColorIndex = float({MaskColorIndexName});
            varTransparentColorIndex = float({TransparentColorIndexName});
            varNoTransparency = float({OpaqueName});
            varAlpha = float({AlphaName}) / 255.0f;
            float z = clamp(1.0f - {ZName} - float({LayerName}) * 0.00001f, 0.0f, 1.0f);
            gl_Position = {ProjectionMatrixName} * {ModelViewMatrixName} * vec4(pos, z, 1.0f);
        }}
    ";

    public Texture2DShader(State state)
        : this(state, TextureFragmentShader(state), TextureVertexShader(state))
    {

    }

    protected Texture2DShader(State state, string fragmentShaderCode, string vertexShaderCode)
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

		Add(BufferPurpose.Position2D, PositionName, new FloatPositionBuffer(State, false));
		Add(BufferPurpose.TextureCoordinates, TextureCoordName, new PositionBuffer(State, false));
		Add(BufferPurpose.DisplayLayer, LayerName, new ByteBuffer(State, true));
		Add(BufferPurpose.PaletteIndex, PaletteIndexName, new ByteBuffer(State, true));
		Add(BufferPurpose.MaskColorIndex, MaskColorIndexName, new ByteBuffer(State, true));
		Add(BufferPurpose.TransparentColorIndex, TransparentColorIndexName, new ByteBuffer(State, true));
		Add(BufferPurpose.Opaque, OpaqueName, new ByteBuffer(State, true));
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
}
