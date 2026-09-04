/*
 * ShaderProgram.cs - GLSL shader program handling
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
using Amber.Renderer.OpenGL.Buffers;

namespace Amber.Renderer.OpenGL;

internal class ShaderProgram : IDisposable
{
    readonly State state;
    Shader? fragmentShader = null;
    Shader? vertexShader = null;
    bool disposed = false;
    readonly bool useNormalUniform = true;

    public uint ProgramIndex { get; private set; } = 0;
    public bool Loaded { get; private set; } = false;
    public bool Linked { get; private set; } = false;
    public static ShaderProgram? ActiveProgram { get; private set; } = null;

    public ShaderProgram(State state)
    {
        this.state = state;

        useNormalUniform = state.OpenGLVersionMajor < 4 || (state.OpenGLVersionMajor == 4 && state.OpenGLVersionMinor < 1);

        Create();
    }

    public ShaderProgram(State state, Shader fragmentShader, Shader vertexShader)
        : this(state)
    {
        AttachShader(fragmentShader);
        AttachShader(vertexShader);

        Link(false);
    }

    void Create()
    {
        ProgramIndex = state.Gl.CreateProgram();
    }

    public void AttachShader(Shader shader)
    {
        if (shader == null)
            return;

        if (ProgramIndex == 0)
            throw new AmberException(ExceptionScope.Render, "Tried to attach a shader to a disposed program.");

        if (shader.ShaderIndex == 0)
            throw new AmberException(ExceptionScope.Render, "Tried to attach disposed shader to program.");

		if (shader.ShaderType == Shader.Type.Fragment)
        {
            if (fragmentShader == shader)
                return;

            if (fragmentShader != null)
                state.Gl.DetachShader(ProgramIndex, fragmentShader.ShaderIndex);

            fragmentShader = shader;
            state.Gl.AttachShader(ProgramIndex, shader.ShaderIndex);
        }
        else if (shader.ShaderType == Shader.Type.Vertex)
        {
            if (vertexShader == shader)
                return;

            if (vertexShader != null)
                state.Gl.DetachShader(ProgramIndex, vertexShader.ShaderIndex);

            vertexShader = shader;
            state.Gl.AttachShader(ProgramIndex, shader.ShaderIndex);
        }

        Linked = false;
        Loaded = fragmentShader != null && vertexShader != null;
    }

    public void Link(bool detachShaders)
    {
        if (!Linked)
        {
            if (!Loaded)
                throw new AmberException(ExceptionScope.Render, "Shader program was not loaded.");

            state.Gl.LinkProgram(ProgramIndex);

            string infoLog = state.Gl.GetProgramInfoLog(ProgramIndex);

            if (!string.IsNullOrWhiteSpace(infoLog))
                throw new AmberException(ExceptionScope.Render, $"Error linking program: {infoLog.Trim()}");

            Linked = true;
        }

        if (detachShaders)
        {
            if (fragmentShader != null)
            {
                state.Gl.DetachShader(ProgramIndex, fragmentShader.ShaderIndex);
                fragmentShader = null;
            }

            if (vertexShader != null)
            {
                state.Gl.DetachShader(ProgramIndex, vertexShader.ShaderIndex);
                vertexShader = null;
            }

            Loaded = false;
        }
    }

    public void Use()
    {
        if (ActiveProgram == this)
            return;

        if (!Linked)
            throw new AmberException(ExceptionScope.Render, "Shader program was not linked.");

        state.Gl.UseProgram(ProgramIndex);
        ActiveProgram = this;
    }

    public uint BindInputBuffer(string name, IBuffer buffer)
    {
        if (ActiveProgram != this)
            throw new AmberException(ExceptionScope.Render, "Shader program is not active.");

        var location = GetLocation(name, attribute: true);

        buffer.Bind();

        state.Gl.EnableVertexAttribArray(location);

        unsafe
        {
            if (buffer.Type == VertexAttribPointerType.Float)
                state.Gl.VertexAttribPointer(location, buffer.Dimension, buffer.Type, buffer.Normalized, 0, (void*)0);
            else
                state.Gl.VertexAttribIPointer(location, buffer.Dimension, (VertexAttribIType)buffer.Type, 0, (void*)0);
        }

        return location;
    }

    public void UnbindInputBuffer(uint location)
    {
        state.Gl.DisableVertexAttribArray(location);
    }

    uint GetLocation(string name, bool attribute = false)
    {
        if (attribute)
            return (uint)state.Gl.GetAttribLocation(ProgramIndex, name);

        return (uint)state.Gl.GetUniformLocation(ProgramIndex, name);
    }

    void CallUniform(Action oldVersion, Action newVersion)
    {
        if (useNormalUniform)
        {
            var activeProgram = ActiveProgram;
            Use();
            oldVersion?.Invoke();
            if (activeProgram != ActiveProgram)
            {
                if (activeProgram == null)
                    state.Gl.UseProgram(0);
                else
                    activeProgram.Use();
                ActiveProgram = activeProgram;
            }
        }
        else
            newVersion?.Invoke();
    }

    public void SetInputMatrix(string name, float[] matrix, bool transpose)
    {
        var location = GetLocation(name);

        switch (matrix.Length)
        {
            case 4: // 2x2
                CallUniform
                (
                    () => state.Gl.UniformMatrix2((int)location, 1, transpose, matrix),
                    () => state.Gl.ProgramUniformMatrix2(ProgramIndex, (int)location, 1, transpose, matrix)
                );
                break;
            case 9: // 3x3
                CallUniform
                (
                    () => state.Gl.UniformMatrix3((int)location, 1, transpose, matrix),
                    () => state.Gl.ProgramUniformMatrix3(ProgramIndex, (int)location, 1, transpose, matrix)
                );
                break;
            case 16: // 4x4
                CallUniform
                (
                    () => state.Gl.UniformMatrix4((int)location, 1, transpose, matrix),
                    () => state.Gl.ProgramUniformMatrix4(ProgramIndex, (int)location, 1, transpose, matrix)
                );
                break;
            default:
                throw new AmberException(ExceptionScope.Render, "Unsupported matrix dimensions. Valid are 2x2, 3x3 or 4x4.");
        }
    }

    public void SetInput(string name, bool value)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform1((int)location, value ? 1 : 0),
            () => state.Gl.ProgramUniform1(ProgramIndex, (int)location, value ? 1 : 0)
        );
    }

    public void SetInput(string name, float value)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform1((int)location, value),
            () => state.Gl.ProgramUniform1(ProgramIndex, (int)location, value)
        );
    }

    public void SetInput(string name, double value)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform1((int)location, (float)value),
            () => state.Gl.ProgramUniform1(ProgramIndex, (int)location, (float)value)
        );
    }

    public void SetInput(string name, int value)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform1((int)location, value),
            () => state.Gl.ProgramUniform1(ProgramIndex, (int)location, value)
        );
    }

    public void SetInput(string name, uint value)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform1((int)location, value),
            () => state.Gl.ProgramUniform1(ProgramIndex, (int)location, value)
        );
    }

    public void SetInputColorArray(string name, Color[] colors)
    {
        var bytes = new byte[colors.Length * 4];
        int index = 0;

        foreach (var color in colors)
        {
            bytes[index++] = color.R;
            bytes[index++] = color.G;
            bytes[index++] = color.B;
            bytes[index++] = color.A;
        }

        SetInputColorArray(name, bytes);
    }

    public void SetInputColorArray(string name, byte[] array)
    {
        var location = GetLocation(name);
        var normalizedArray = array.Select(i => i / 255.0f).ToArray();

        CallUniform
        (
            () => state.Gl.Uniform4((int)location, (uint)array.Length, normalizedArray),
            () => state.Gl.ProgramUniform4(ProgramIndex, (int)location, (uint)array.Length, normalizedArray)
        );
    }

    public void SetInputColor(string name, Color color)
    {
        var location = GetLocation(name);
        float[] normalizedArray = [color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f];

        CallUniform
        (
            () => state.Gl.Uniform4((int)location, normalizedArray),
            () => state.Gl.ProgramUniform4(ProgramIndex, (int)location, normalizedArray)
        );
    }

    public void SetInputVector2(string name, float x, float y)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform2((int)location, x, y),
            () => state.Gl.ProgramUniform2(ProgramIndex, (int)location, x, y)
        );
    }

    public void SetInputVector2(string name, int x, int y)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform2((int)location, x, y),
            () => state.Gl.ProgramUniform2(ProgramIndex, (int)location, x, y)
        );
    }

    public void SetInputVector2(string name, uint x, uint y)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform2((int)location, x, y),
            () => state.Gl.ProgramUniform2(ProgramIndex, (int)location, x, y)
        );
    }

    public void SetInputVector3(string name, float x, float y, float z)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform3((int)location, x, y, z),
            () => state.Gl.ProgramUniform3(ProgramIndex, (int)location, x, y, z)
        );
    }

    public void SetInputVector3(string name, int x, int y, int z)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform3((int)location, x, y, z),
            () => state.Gl.ProgramUniform3(ProgramIndex, (int)location, x, y, z)
        );
    }

    public void SetInputVector3(string name, uint x, uint y, uint z)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform3((int)location, x, y, z),
            () => state.Gl.ProgramUniform3(ProgramIndex, (int)location, x, y, z)
        );
    }

    public void SetInputVector4(string name, float x, float y, float z, float w)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform4((int)location, x, y, z, w),
            () => state.Gl.ProgramUniform4(ProgramIndex, (int)location, x, y, z, w)
        );
    }

    public void SetInputVector4(string name, int x, int y, int z, int w)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform4((int)location, x, y, z, w),
            () => state.Gl.ProgramUniform4(ProgramIndex, (int)location, x, y, z, w)
        );
    }

    public void SetInputVector4(string name, uint x, uint y, uint z, uint w)
    {
        var location = GetLocation(name);

        CallUniform
        (
            () => state.Gl.Uniform4((int)location, x, y, z, w),
            () => state.Gl.ProgramUniform4(ProgramIndex, (int)location, x, y, z, w)
        );
    }

    public void Dispose()
    {
        if (!disposed)
        {
            if (ProgramIndex != 0)
            {
                if (ActiveProgram == this)
                {
                    state.Gl.UseProgram(0);
                    ActiveProgram = null;
                }

                state.Gl.DeleteProgram(ProgramIndex);
                ProgramIndex = 0;
            }

            disposed = true;
        }
    }
}
