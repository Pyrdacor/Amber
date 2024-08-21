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

using Amber.Common;
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
        uniform float {ColorKeyName};
        uniform float {AllowTransparencyName};
        in vec2 varTexCoord;
        flat in float palIndex;
        flat in float maskColIndex;
        flat in float noTransparency;
        
        void main()
        {{
            vec4 pixelColor = vec4(0);
            
            if ({UsePaletteName} > 0.5f)
            {{
                float colorIndex = texture({TextureName}, varTexCoord).r * 255.0f;
                
                if (colorIndex < 0.5f && noTransparency < 0.5f && {AllowTransparencyName} >= 0.5f)
                    discard;
                else
                {{
                    if (colorIndex > {PaletteSizeName} - 0.5f)
                        colorIndex = 0.0f;
                    pixelColor = texture({PaletteName}, vec2((colorIndex + 0.5f) / {PaletteSizeName}, (palIndex + 0.5f) / {PaletteCountName}));
                }}
            }}
            else
            {{
                pixelColor = texture({TextureName}, varTexCoord);
                if (pixelColor.a < 0.5f && noTransparency < 0.5f && {AllowTransparencyName} >= 0.5f)
                    discard;
            }}
           
            if (maskColIndex >= 0.5f)
                pixelColor = texture({PaletteName}, vec2((maskColIndex + 0.5f) / {PaletteSizeName}, (palIndex + 0.5f) / {PaletteCountName}));

            if (noTransparency >= 0.5f || {AllowTransparencyName} < 0.5f)
                pixelColor.a = 1.0f;
                
            {FragmentOutColorName} = pixelColor;
        }}
    ";

    protected static string TextureVertexShader(State state) => GetVertexShaderHeader(state) + $@"
        in vec2 {PositionName};
        in ivec2 {TexCoordName};
        in uint {LayerName};
        in uint {PaletteIndexName};
        in uint {MaskColorIndexName};
        in uint {OpaqueName};
        uniform uvec2 {AtlasSizeName};
        uniform float {ZName};
        uniform mat4 {ProjectionMatrixName};
        uniform mat4 {ModelViewMatrixName};
        out vec2 varTexCoord;
        flat out float palIndex;
        flat out float maskColIndex;
        flat out float noTransparency;
        
        void main()
        {{
            vec2 atlasFactor = vec2(1.0f / float({AtlasSizeName}.x), 1.0f / float({AtlasSizeName}.y));
            vec2 pos = vec2({PositionName}.x + 0.49f, {PositionName}.y + 0.49f);
            varTexCoord = atlasFactor * vec2({TexCoordName}.x, {TexCoordName}.y);
            palIndex = float({PaletteIndexName});
            maskColIndex = float({MaskColorIndexName});
            noTransparency = float({OpaqueName});
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
		Add(BufferPurpose.TextureCoordinates, TexCoordName, new PositionBuffer(State, false));
		Add(BufferPurpose.DisplayLayer, LayerName, new ByteBuffer(State, true));
		Add(BufferPurpose.PaletteIndex, PaletteIndexName, new ByteBuffer(State, true));
		Add(BufferPurpose.MaskColorIndex, MaskColorIndexName, new ByteBuffer(State, true));
        Add(BufferPurpose.Opaque, OpaqueName, new ByteBuffer(State, true));

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

    public void SetColorKey(byte colorIndex)
    {
        if (colorIndex > 31)
            throw new AmberException(ExceptionScope.Render, "Color index must be in the range 0 to 31.");

        shaderProgram.SetInput(ColorKeyName, (float)colorIndex);
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
}
