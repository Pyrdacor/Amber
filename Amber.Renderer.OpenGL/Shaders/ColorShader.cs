/*
 * ColorShader.cs - Basic shader for colored 2D shapes
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
using System.Collections.Generic;
using static Shader;

internal class ColorShader : BaseShader
{
    static string ColorFragmentShader(State state) => GetFragmentShaderHeader(state) + $@"
            flat in vec4 pixelColor;
            
            void main()
            {{
                {FragmentOutColorName} = pixelColor;
            }}
        ";

    static string ColorVertexShader(State state) => GetVertexShaderHeader(state) + $@"
            in vec2 {PositionName};
            in uint {LayerName};
            in uvec4 {ColorName};
            uniform float {ZName};
            uniform mat4 {ProjectionMatrixName};
            uniform mat4 {ModelViewMatrixName};
            flat out vec4 pixelColor;

            void main()
            {{
                vec2 pos = vec2({PositionName}.x + 0.49f, {PositionName}.y + 0.49f);
                pixelColor = vec4(float({ColorName}.r) / 255.0f, float({ColorName}.g) / 255.0f, float({ColorName}.b) / 255.0f, float({ColorName}.a) / 255.0f);
                
                gl_Position = {ProjectionMatrixName} * {ModelViewMatrixName} * vec4(pos, 1.0f - {ZName} - float({LayerName}) * 0.00001f, 1.0f);
            }}
        ";

	public ColorShader(State state)
        : base(state, ColorFragmentShader(state), ColorVertexShader(state))
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
		Add(BufferPurpose.DisplayLayer, LayerName, new ByteBuffer(State, true));
		Add(BufferPurpose.Color, ColorName, new ColorBuffer(State, false));

		return buffers;
	}
}
