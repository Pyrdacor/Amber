﻿/*
 * PositionBuffer.cs - Buffer for shader position data
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

internal class PositionBuffer(State state, bool staticData) : Buffer<short>(state, staticData)
{
    public override int Dimension => 2;

	bool UpdatePositionData(short[] buffer, int index, Tuple<short, short> position)
    {
        bool changed = false;
        short x = position.Item1;
        short y = position.Item2;

        if (buffer[index + 0] != x ||
            buffer[index + 1] != y)
        {
            buffer[index + 0] = x;
            buffer[index + 1] = y;
            changed = true;
        }

        return changed || index == Size;
    }

    public int Add(short x, short y, int index = -1)
    {
        return Add(UpdatePositionData, Tuple.Create(x, y), index);
    }

    public void Update(int index, short x, short y)
    {
        Update(UpdatePositionData, index, Tuple.Create(x, y));
    }

    public void TransformAll(Func<int, Tuple<short, short>, Tuple<short, short>> updater)
    {
        bool TransformPositionData(short[] buffer, int index, Tuple<short, short>? _)
        {
            bool changed = false;
            var position = Tuple.Create(buffer[index + 0], buffer[index + 1]);
            var newPosition = updater(index, position);
            short x = newPosition.Item1;
            short y = newPosition.Item2;

            if (buffer[index + 0] != x ||
                buffer[index + 1] != y)
            {
                buffer[index + 0] = x;
                buffer[index + 1] = y;
                changed = true;
            }

            return changed || index == Size;
        }

        for (int i = 0; i < Size; ++i)
        {
            Update<Tuple<short, short>?>(TransformPositionData, i, null);
        }
    }
}
