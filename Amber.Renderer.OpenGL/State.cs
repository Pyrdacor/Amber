/*
 * State.cs - OpenGL state
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

using System.Numerics;
using System.Text.RegularExpressions;

namespace Amber.Renderer.OpenGL;

internal partial class State : IEquatable<State>
{
    public readonly int OpenGLVersionMajor = 0;
    public readonly int OpenGLVersionMinor = 0;
    public readonly bool Embedded = false;
    public readonly int GLSLVersionMajor = 0;
    public readonly int GLSLVersionMinor = 0;
    readonly string contextIdentifier;
	readonly Stack<Matrix4x4> projectionMatrixStack = new();
	readonly Stack<Matrix4x4> modelViewMatrixStack = new();

	public State(IContextProvider contextProvider)
    {
        contextIdentifier = contextProvider.Identifier;

        Gl = GL.GetApi(contextProvider);

        var openGLVersion = Gl.GetStringS(StringName.Version).TrimStart();

        if (openGLVersion.StartsWith("OpenGL"))
            openGLVersion = openGLVersion[6..].TrimStart();

        if (openGLVersion.StartsWith("ES"))
        {
            Embedded = true;
            openGLVersion = openGLVersion[2..].TrimStart();
        }

        var versionRegex = VersionRegex();
        var match = versionRegex.Match(openGLVersion);

        if (!match.Success || match.Index != 0 || match.Groups.Count < 3)
        {
            throw new Exception("OpenGL is not supported or the version could not be determined.");
        }

        OpenGLVersionMajor = int.Parse(match.Groups[1].Value);
        OpenGLVersionMinor = int.Parse(match.Groups[2].Value);

        if (OpenGLVersionMajor >= 2 || Embedded) // glsl is supported since OpenGL 2.0
        {
            var glslVersion = Gl.GetStringS(StringName.ShadingLanguageVersion);

            while (true)
            {
                if (glslVersion.StartsWith("OpenGL"))
                    glslVersion = glslVersion[6..].TrimStart();
                else if (glslVersion.StartsWith("ES"))
                    glslVersion = glslVersion[2..].TrimStart();
                else if (glslVersion.StartsWith("GLSL"))
                    glslVersion = glslVersion[4..].TrimStart();
                else
                    break;
            }

            match = versionRegex.Match(glslVersion);

            if (match.Success && match.Index == 0 && match.Groups.Count >= 3)
            {
                GLSLVersionMajor = int.Parse(match.Groups[1].Value);
                GLSLVersionMinor = int.Parse(match.Groups[2].Value);
            }
        }
    }

	public GL Gl { get; }
	public Matrix4x4 ProjectionMatrix2D { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 ProjectionMatrix3D { get; set; } = Matrix4x4.Identity;
    public Matrix4x4 FullScreenProjectionMatrix2D { get; set; } = Matrix4x4.Identity;
	public Matrix4x4 CurrentProjectionMatrix => (projectionMatrixStack.Count == 0) ? Matrix4x4.Identity : projectionMatrixStack.Peek();
	public Matrix4x4 CurrentModelViewMatrix => (modelViewMatrixStack.Count == 0) ? Matrix4x4.Identity : modelViewMatrixStack.Peek();

	public void PushProjectionMatrix(Matrix4x4 matrix)
    {
        projectionMatrixStack.Push(matrix);
    }

    public void PushModelViewMatrix(Matrix4x4 matrix)
    {
        modelViewMatrixStack.Push(matrix);
    }

    public Matrix4x4 PopProjectionMatrix()
    {
        return projectionMatrixStack.Pop();
    }

    public Matrix4x4 PopModelViewMatrix()
    {
        return modelViewMatrixStack.Pop();
    }

    public void RestoreProjectionMatrix(Matrix4x4 matrix)
    {
        if (projectionMatrixStack.Contains(matrix))
        {
            while (CurrentProjectionMatrix != matrix)
                projectionMatrixStack.Pop();
        }
        else
            PushProjectionMatrix(matrix);
    }

    public void RestoreModelViewMatrix(Matrix4x4 matrix)
    {
        if (modelViewMatrixStack.Contains(matrix))
        {
            while (CurrentModelViewMatrix != matrix)
                modelViewMatrixStack.Pop();
        }
        else
            PushModelViewMatrix(matrix);
    }

    public void ClearMatrices()
    {
        projectionMatrixStack.Clear();
        modelViewMatrixStack.Clear();
    }

    public bool Equals(State? other)
    {
        if (other == null)
            return false;

        return contextIdentifier == other.contextIdentifier &&
            OpenGLVersionMajor == other.OpenGLVersionMajor &&
            OpenGLVersionMinor == other.OpenGLVersionMinor &&
            GLSLVersionMajor == other.GLSLVersionMajor &&
            GLSLVersionMinor == other.GLSLVersionMinor;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not State state)
            return false;

        return Equals(state);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(contextIdentifier,
            OpenGLVersionMajor, OpenGLVersionMinor,
            GLSLVersionMajor, GLSLVersionMinor);
    }

	[GeneratedRegex(@"([0-9]+)\.([0-9]+)", RegexOptions.Compiled)]
	private static partial Regex VersionRegex();
}
