/*
 * BaseShader.cs - Basic shader
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

internal abstract class BaseShader : IShader
{
    internal ShaderProgram shaderProgram;

	protected BaseShader(State state, string fragmentShaderCode, string vertexShaderCode)
	{
		var fragmentShader = new Shader(state, Shader.Type.Fragment, fragmentShaderCode);
		var vertexShader = new Shader(state, Shader.Type.Vertex, vertexShaderCode);

		shaderProgram = new ShaderProgram(state, fragmentShader, vertexShader);

		State = state;
	}

	private protected State State { get; }

	public ShaderProgram ShaderProgram => shaderProgram;

	public abstract Dictionary<BufferPurpose, IBuffer> SetupBuffers(VertexArrayObject vertexArrayObject);

	public void UpdateMatrices(State state)
	{
		shaderProgram.SetInputMatrix(ModelViewMatrixName, state.CurrentModelViewMatrix.ToArray(), true);
		shaderProgram.SetInputMatrix(ProjectionMatrixName, state.CurrentProjectionMatrix.ToArray(), true);
	}

	public void Use()
	{
		if (shaderProgram != ShaderProgram.ActiveProgram)
			shaderProgram.Use();
	}

	public void SetZ(float z)
    {
        shaderProgram.SetInput(ZName, z);
    }
}
