/*
 * VertexArrayBuffer.cs - OpenGL VAO handling
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

namespace Amber.Renderer.OpenGL;

using Buffers;

// VAO
internal class VertexArrayObject : IDisposable
{
	readonly State state;
	readonly ShaderProgram program;
	readonly object vaoLock = new();
	readonly Dictionary<string, IBuffer> buffers = [];
    readonly Dictionary<string, int> bufferLocations = [];
	uint index = 0;
	bool disposed = false;
    bool buffersAreBound = false;

    public static VertexArrayObject? ActiveVAO { get; private set; } = null;

    public VertexArrayObject(State state, ShaderProgram program)
    {
        this.program = program;
        this.state = state;

        Create();
    }

    void Create()
    {
        index = state.Gl.GenVertexArray();
    }

    public void Lock()
    {
        Monitor.Enter(vaoLock);
    }

    public void Unlock()
    {
        Monitor.Exit(vaoLock);
    }

    public void AddBuffer(string name, IBuffer buffer)
    {
        buffers.Add(name, buffer);
    }

    public void BindBuffers()
    {
        if (buffersAreBound)
            return;

        lock (vaoLock)
        {
            program.Use();
            InternalBind(true);

            foreach (var buffer in buffers)
                bufferLocations[buffer.Key] = (int)program.BindInputBuffer(buffer.Key, buffer.Value);

            buffersAreBound = true;
        }
    }

    public void UnbindBuffers()
    {
        if (!buffersAreBound)
            return;

        lock (vaoLock)
        {
            program.Use();
            InternalBind(true);

            foreach (var buffer in buffers)
            {
                program.UnbindInputBuffer((uint)bufferLocations[buffer.Key]);
                bufferLocations[buffer.Key] = -1;
            }

            buffersAreBound = false;
        }
    }

    public void Bind()
    {
        InternalBind(false);
    }

    void InternalBind(bool bindOnly)
    {
        lock (vaoLock)
        {
            if (ActiveVAO != this)
            {
                state.Gl.BindVertexArray(index);
                program.Use();
                ActiveVAO = this;
            }

            if (!bindOnly)
            {
                bool buffersChanged = false;

                // ensure that all buffers are up to date
                foreach (var buffer in buffers)
                {
                    if (buffer.Value.RecreateUnbound())
                        buffersChanged = true;
                }

                if (buffersChanged)
                {
                    UnbindBuffers();
                    BindBuffers();
                }
            }
        }
    }

    public static void Bind(VertexArrayObject vao)
    {
        if (vao != null)
            vao.Bind();
        else
            ActiveVAO?.Unbind();
    }

    public void Unbind()
    {
        if (ActiveVAO == this)
        {
            state.Gl.BindVertexArray(0);
            ActiveVAO = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }

    void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                if (index != 0)
                {
                    if (ActiveVAO == this)
                        Unbind();

                    state.Gl.DeleteVertexArray(index);
                    index = 0;
                }

                disposed = true;
            }
        }
    }
}