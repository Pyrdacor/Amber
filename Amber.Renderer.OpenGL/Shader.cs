/*
 * Shader.cs - GLSL shader handling
 *
 * Copyright (C) 2024  Robert Schneckenhaus <robert.schneckenhaus@web.de>
 *
 * This file is part of the Amber project.
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

using Amber.Common;

namespace Amber.Renderer.OpenGL;

internal class Shader : IDisposable
{
    public enum Type
    {
        Fragment,
        Vertex
    }

	internal static readonly string FragmentOutColorName = "outColor";
	internal static readonly string PositionName = "position";
	internal static readonly string ModelViewMatrixName = "mvMat";
	internal static readonly string ProjectionMatrixName = "projMat";
	internal static readonly string ColorName = "color";
	internal static readonly string ZName = "z";
	internal static readonly string LayerName = "layer";
	internal static readonly string UsePaletteName = "usePalette";
	internal static readonly string TexCoordName = "texCoord";
	internal static readonly string TextureName = "sampler";
	internal static readonly string AtlasSizeName = "atlasSize";
	internal static readonly string PaletteName = "palette";
	internal static readonly string PaletteIndexName = "paletteIndex";
	internal static readonly string ColorKeyName = "colorKeyIndex";
	internal static readonly string MaskColorIndexName = "maskColorIndex";
	internal static readonly string TransparentColorIndexName = "transparentColorIndex";	
	internal static readonly string PaletteCountName = "palCount";
    internal static readonly string PaletteSizeName = "palSize";
	internal static readonly string OpaqueName = "opaque";
    internal static readonly string AllowTransparencyName = "allowTransparency";

	readonly State state;
	readonly string code = "";
    bool disposed = false;

    public Type ShaderType { get; }
    public uint ShaderIndex { get; private set; } = 0;

    public Shader(State state, Type type, string code)
    {
        this.state = state;
        ShaderType = type;
        this.code = code;

        Create();
    }

    void Create()
    {
        ShaderIndex = state.Gl.CreateShader
        (
            ShaderType == Type.Fragment
                ? GLEnum.FragmentShader
                : GLEnum.VertexShader
        );

        state.Gl.ShaderSource(ShaderIndex, code);
        state.Gl.CompileShader(ShaderIndex);

        string infoLog = state.Gl.GetShaderInfoLog(ShaderIndex);

        if (!string.IsNullOrWhiteSpace(infoLog))
            throw new AmberException(ExceptionScope.Render, $"Failed to create shader: {infoLog.Trim()}");
    }

	internal static string GetFragmentShaderHeader(State state)
	{
#if GLES
            string header = $"#version {state.GLSLVersionMajor}{state.GLSLVersionMinor:00} es\n";
#else
		string header = $"#version {state.GLSLVersionMajor}{state.GLSLVersionMinor}\n";
#endif

		header += "\n";
		header += "#ifdef GL_ES\n";
		header += " precision highp float;\n";
		header += " precision highp int;\n";
		header += "#endif\n";
		header += "\n";
		header += $"out vec4 {FragmentOutColorName};\n";

		return header;
	}

	internal static string GetVertexShaderHeader(State state)
	{
#if GLES
            return $"#version {state.GLSLVersionMajor}{state.GLSLVersionMinor:00} es\n\n";
#else
		return $"#version {state.GLSLVersionMajor}{state.GLSLVersionMinor}\n\n";
#endif
	}


	public void AttachToProgram(ShaderProgram program)
    {
        program.AttachShader(this);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            if (ShaderIndex != 0)
            {
                state.Gl.DeleteShader(ShaderIndex);
                ShaderIndex = 0;
            }

            disposed = true;
        }
    }
}