/*
 * IBuffer.cs - Interface for render data buffers
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

namespace Amber.Renderer.OpenGL.Buffers;

public enum BufferPurpose
{
    Position2D,
    Position3D,
    Color,
    Alpha,
    TextureCoordinates,
    TextureSize,
    DisplayLayer,
    PaletteIndex,
    MaskColorIndex,
}

internal interface IBuffer : IDisposable
{
    int Dimension { get; }
    bool Normalized { get; }
    int Size { get; }
	VertexAttribPointerType Type { get; }

	void Bind();
    void Unbind();
    bool RecreateUnbound();
    void Remove(int index);
}
