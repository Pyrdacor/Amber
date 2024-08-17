﻿/*
 * IndexBuffer.cs - Dynamic buffer for vertex indices
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

using Amber.Common;

namespace Amber.Renderer.OpenGL;

internal class IndexBuffer : BufferObject<uint>
{
    public override int Dimension => 6;

    public IndexBuffer(State state)
        : base(state, true)
    {
        BufferTarget = GLEnum.ElementArrayBuffer;
    }

    bool InsertIndexData(uint[] buffer, int index, uint startIndex)
    {
        buffer[index++] = startIndex + 0;
        buffer[index++] = startIndex + 1;
        buffer[index++] = startIndex + 2;
        buffer[index++] = startIndex + 3;
        buffer[index++] = startIndex + 0;
        buffer[index++] = startIndex + 2;

        return true;
    }

    public void InsertQuad(int quadIndex)
    {
        if (quadIndex >= int.MaxValue / 6)
            throw new AmberException(ExceptionScope.Render, "Too many polygons to render.");

        int arrayIndex = quadIndex * 6; // 2 triangles with 3 vertices each
        uint vertexIndex = (uint)(quadIndex * 4); // 4 different vertices form a quad

        while (Size <= arrayIndex + 6)
        {
            base.Add(InsertIndexData, (uint)vertexIndex, quadIndex);
        }
    }
}
