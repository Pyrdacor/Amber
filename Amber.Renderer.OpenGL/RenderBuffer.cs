﻿/*
 * RenderBuffer.cs - Renders several data buffers
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
using Amber.Renderer.OpenGL.Buffers;
using Amber.Renderer.OpenGL.Shaders;
using ColorBuffer = Amber.Renderer.OpenGL.Buffers.ColorBuffer;

namespace Amber.Renderer.OpenGL;

internal class RenderBuffer : IDisposable
{
    bool disposed = false;
	readonly State state;
	readonly int textureFactor = 1;
	readonly VertexArrayObject vertexArrayObject;
    readonly Dictionary<BufferPurpose, IBuffer> buffers;
    readonly IndexBuffer indexBuffer;
    readonly LayerFeatures features;

    public bool NeedsBlending { get; }

	public RenderBuffer(State state, BaseShader shader, LayerFeatures features, int textureFactor = 1)
    {
        this.state = state;
        this.features = features;
        this.textureFactor = textureFactor;
		vertexArrayObject = new VertexArrayObject(state, shader.ShaderProgram);

        buffers = shader.SetupBuffers(vertexArrayObject);

		indexBuffer = new IndexBuffer(state);
		vertexArrayObject.AddBuffer("index", indexBuffer);

        NeedsBlending = shader.NeedsBlending;
	}

    private TBuffer? GetBuffer<TBuffer, T>(BufferPurpose purpose)
        where TBuffer : Buffer<T>
		where T : unmanaged, IEquatable<T>
	{
        return buffers.GetValueOrDefault(purpose) as TBuffer;
    }

    private void SetupDisplayLayer(int index, ISizedDrawable drawable)
    {
        var layerBuffer = GetBuffer<ByteBuffer, byte>(BufferPurpose.DisplayLayer);

		if (layerBuffer != null)
		{
            byte layer = features.HasFlag(LayerFeatures.DisplayLayers) && drawable is ILayeredDrawable layered
                ? layered.DisplayLayer
                : (byte)MathUtil.Limit(0, drawable.Position.Y + drawable.Size.Height + drawable.BaseLineOffset, 255);

			int layerBufferIndex = layerBuffer.Add(layer, index);
			layerBuffer.Add(layer, layerBufferIndex + 1);
			layerBuffer.Add(layer, layerBufferIndex + 2);
			layerBuffer.Add(layer, layerBufferIndex + 3);
		}
	}

    public int GetDrawIndex(IColoredRect coloredRect,
        PositionTransformation? positionTransformation,
        SizeTransformation? sizeTransformation)
    {
        var position = new FloatPosition(coloredRect.Position);
        var size = new FloatSize(coloredRect.Size);

        if (positionTransformation != null)
            position = positionTransformation(position);

        if (sizeTransformation != null)
            size = sizeTransformation(size);

        var positionBuffer = GetBuffer<FloatPositionBuffer, float>(BufferPurpose.Position2D);

		int index = positionBuffer!.Add(position.X, position.Y);
        positionBuffer.Add(position.X + size.Width, position.Y, index + 1);
        positionBuffer.Add(position.X + size.Width, position.Y + size.Height, index + 2);
        positionBuffer.Add(position.X, position.Y + size.Height, index + 3);

        indexBuffer.InsertQuad(index / 4);

        SetupDisplayLayer(index, coloredRect);

        var colorBuffer = GetBuffer<ColorBuffer, byte>(BufferPurpose.Color);

        if (colorBuffer != null)
        {
            var color = coloredRect.Color;

            int colorBufferIndex = colorBuffer.Add(color, index);
            colorBuffer.Add(color, colorBufferIndex + 1);
            colorBuffer.Add(color, colorBufferIndex + 2);
            colorBuffer.Add(color, colorBufferIndex + 3);
        }

        return index;
    }

    public int GetDrawIndex(ISprite sprite, PositionTransformation? positionTransformation,
        SizeTransformation? sizeTransformation, byte? textColorIndex = null)
    {
        var position = new FloatPosition(sprite.Position);
        var spriteSize = new Size(sprite.Size);
        var textureOffset = new Position(sprite.TextureOffset);
        var textureSize = new Size(sprite.TextureSize ?? spriteSize);

        if (sprite.ClipRect != null)
        {
            float textureWidthFactor = (float)spriteSize.Width / textureSize.Width;
            float textureHeightFactor = (float)spriteSize.Height / textureSize.Height;
            float oldX = position.X;
            float oldY = position.Y;
            int oldWidth = spriteSize.Width;
            int oldHeight = spriteSize.Height;
            sprite.ClipRect.Value.Clip(ref position, ref spriteSize);
            int textureX = textureOffset.X;
            int textureY = textureOffset.Y + MathUtil.Round((position.Y - oldY) / textureHeightFactor);
			textureSize = new(textureSize.Width - MathUtil.Round((oldWidth - spriteSize.Width) / textureWidthFactor),
                textureSize.Height - MathUtil.Round((oldHeight - spriteSize.Height) / textureHeightFactor));

            if (sprite.MirrorX)
            {
                float oldRight = oldX + oldWidth;
                float newRight = position.X + spriteSize.Width;
				textureX += MathUtil.Round((oldRight - newRight) / textureWidthFactor);
            }
            else
            {
				textureX += MathUtil.Round((position.X - oldX) / textureWidthFactor);
            }

            textureOffset = new(textureX, textureY);
        }

        var size = new FloatSize(spriteSize);

        if (positionTransformation != null)
            position = positionTransformation(position);

        if (sizeTransformation != null)
            size = sizeTransformation(size);

		var positionBuffer = GetBuffer<FloatPositionBuffer, float>(BufferPurpose.Position2D);

		int index = positionBuffer!.Add(position.X, position.Y);
        positionBuffer.Add(position.X + size.Width, position.Y, index + 1);
        positionBuffer.Add(position.X + size.Width, position.Y + size.Height, index + 2);
        positionBuffer.Add(position.X, position.Y + size.Height, index + 3);

        indexBuffer.InsertQuad(index / 4);

		var paletteIndexBuffer = GetBuffer<ByteBuffer, byte>(BufferPurpose.PaletteIndex);

		if (paletteIndexBuffer != null)
        {
            int paletteIndexBufferIndex = paletteIndexBuffer.Add(sprite.PaletteIndex, index);
            paletteIndexBuffer.Add(sprite.PaletteIndex, paletteIndexBufferIndex + 1);
            paletteIndexBuffer.Add(sprite.PaletteIndex, paletteIndexBufferIndex + 2);
            paletteIndexBuffer.Add(sprite.PaletteIndex, paletteIndexBufferIndex + 3);
        }

		var opaqueBuffer = GetBuffer<ByteBuffer, byte>(BufferPurpose.Opaque);

		if (opaqueBuffer != null)
		{
            byte opaque = (byte)(sprite.Opaque ? 1 : 0);

			int opaqueBufferIndex = opaqueBuffer.Add(opaque, index);
			opaqueBuffer.Add(opaque, opaqueBufferIndex + 1);
			opaqueBuffer.Add(opaque, opaqueBufferIndex + 2);
			opaqueBuffer.Add(opaque, opaqueBufferIndex + 3);
		}

		var textureOffsetBuffer = GetBuffer<PositionBuffer, short>(BufferPurpose.TextureCoordinates);

		if (textureOffsetBuffer != null)
        {
            textureSize = new Size(textureSize.Width * textureFactor, textureSize.Height * textureFactor);

            if (sprite.MirrorX)
            {
                int textureOffsetBufferIndex = textureOffsetBuffer.Add((short)(textureOffset.X + textureSize.Width), (short)textureOffset.Y, index);
				textureOffsetBuffer.Add((short)textureOffset.X, (short)textureOffset.Y, textureOffsetBufferIndex + 1);
				textureOffsetBuffer.Add((short)textureOffset.X, (short)(textureOffset.Y + textureSize.Height), textureOffsetBufferIndex + 2);
				textureOffsetBuffer.Add((short)(textureOffset.X + textureSize.Width), (short)(textureOffset.Y + textureSize.Height), textureOffsetBufferIndex + 3);
            }
            else
            {
                int textureOffsetBufferIndex = textureOffsetBuffer.Add((short)textureOffset.X, (short)textureOffset.Y, index);
                textureOffsetBuffer.Add((short)(textureOffset.X + textureSize.Width), (short)textureOffset.Y, textureOffsetBufferIndex + 1);
                textureOffsetBuffer.Add((short)(textureOffset.X + textureSize.Width), (short)(textureOffset.Y + textureSize.Height), textureOffsetBufferIndex + 2);
                textureOffsetBuffer.Add((short)textureOffset.X, (short)(textureOffset.Y + textureSize.Height), textureOffsetBufferIndex + 3);
            }
        }

		SetupDisplayLayer(index, sprite);

		var maskColorBuffer = GetBuffer<ByteBuffer, byte>(BufferPurpose.MaskColorIndex);

		if (maskColorBuffer != null)
        {
            byte color = sprite.MaskColorIndex ?? byte.MaxValue;

            int maskColorBufferIndex = maskColorBuffer.Add(color, index);
            maskColorBuffer.Add(color, maskColorBufferIndex + 1);
            maskColorBuffer.Add(color, maskColorBufferIndex + 2);
            maskColorBuffer.Add(color, maskColorBufferIndex + 3);
        }

		var transparentColorBuffer = GetBuffer<ByteBuffer, byte>(BufferPurpose.TransparentColorIndex);

		if (transparentColorBuffer != null)
		{
			byte color = sprite.TransparentColorIndex ?? 0;

			int transparentColorBufferIndex = transparentColorBuffer.Add(color, index);
			transparentColorBuffer.Add(color, transparentColorBufferIndex + 1);
			transparentColorBuffer.Add(color, transparentColorBufferIndex + 2);
			transparentColorBuffer.Add(color, transparentColorBufferIndex + 3);
		}

		/*if (textColorIndexBuffer != null)
        {
            if (textColorIndex == null)
                throw new AmbermoonException(ExceptionScope.Render, "No text color index given but text color index buffer is active.");

            int textColorIndexBufferIndex = textColorIndexBuffer.Add(textColorIndex.Value, index);
            textColorIndexBuffer.Add(textColorIndex.Value, textColorIndexBufferIndex + 1);
            textColorIndexBuffer.Add(textColorIndex.Value, textColorIndexBufferIndex + 2);
            textColorIndexBuffer.Add(textColorIndex.Value, textColorIndexBufferIndex + 3);
        }

        if (alphaBuffer != null)
        {
            byte alpha = sprite is AlphaSprite alphaSprite ? alphaSprite.Alpha : (byte)0xff;
            int alphaBufferIndex = alphaBuffer.Add(alpha, index);
            alphaBuffer.Add(alpha, alphaBufferIndex + 1);
            alphaBuffer.Add(alpha, alphaBufferIndex + 2);
            alphaBuffer.Add(alpha, alphaBufferIndex + 3);
        }*/

		return index;
    }

    public void UpdatePosition(int index, ISizedDrawable drawable,
        PositionTransformation? positionTransformation, SizeTransformation? sizeTransformation)
    {
		var position = new FloatPosition(drawable.Position);
        var size = new FloatSize(drawable.Size);

		drawable.ClipRect?.Clip(ref position, ref size);

		if (positionTransformation != null)
            position = positionTransformation(position);

        if (sizeTransformation != null)
            size = sizeTransformation(size);

		var positionBuffer = GetBuffer<FloatPositionBuffer, float>(BufferPurpose.Position2D);

		positionBuffer!.Update(index, position.X, position.Y);
        positionBuffer.Update(index + 1, position.X + size.Width, position.Y);
        positionBuffer.Update(index + 2, position.X + size.Width, position.Y + size.Height);
        positionBuffer.Update(index + 3, position.X, position.Y + size.Height);

        if (!features.HasFlag(LayerFeatures.DisplayLayers))
        {
            // Based on lower Y (baseline)
			SetupDisplayLayer(index, drawable);
		}
    }

    public void UpdateMaskColor(int index, byte? maskColor)
    {
		var maskColorBuffer = GetBuffer<ByteBuffer, byte>(BufferPurpose.MaskColorIndex);

		if (maskColorBuffer != null)
        {
            var color = maskColor ?? 0;
            maskColorBuffer.Update(index, color);
            maskColorBuffer.Update(index + 1, color);
            maskColorBuffer.Update(index + 2, color);
            maskColorBuffer.Update(index + 3, color);
        }
    }

	public void UpdateTransparentColor(int index, byte? transparentColor)
	{
		var transparentColorBuffer = GetBuffer<ByteBuffer, byte>(BufferPurpose.TransparentColorIndex);

		if (transparentColorBuffer != null)
		{
			var color = transparentColor ?? 0;
			transparentColorBuffer.Update(index, color);
			transparentColorBuffer.Update(index + 1, color);
			transparentColorBuffer.Update(index + 2, color);
			transparentColorBuffer.Update(index + 3, color);
		}
	}

	public void UpdateOpaque(int index, byte opaque)
	{
		var opaqueBuffer = GetBuffer<ByteBuffer, byte>(BufferPurpose.Opaque);

		if (opaqueBuffer != null)
		{
			opaqueBuffer.Update(index, opaque);
			opaqueBuffer.Update(index + 1, opaque);
			opaqueBuffer.Update(index + 2, opaque);
			opaqueBuffer.Update(index + 3, opaque);
		}
	}

	public void UpdateTextureOffset(int index, ISprite sprite)
    {
		var textureOffsetBuffer = GetBuffer<PositionBuffer, short>(BufferPurpose.TextureCoordinates);

		if (textureOffsetBuffer == null)
            return;

        var position = new FloatPosition(sprite.Position);
        var spriteSize = new Size(sprite.Size);
        var textureOffset = new Position(sprite.TextureOffset);
        var textureSize = new Size(sprite.TextureSize ?? spriteSize);

        if (sprite is IAnimatedSprite animatedSprite && animatedSprite.CurrentFrameIndex > 0)
        {
            textureOffset = new(textureOffset.X + animatedSprite.CurrentFrameIndex * textureSize.Width, textureOffset.Y);
        }

        if (sprite.ClipRect != null)
        {
            float textureWidthFactor = (float)spriteSize.Width / textureSize.Width;
            float textureHeightFactor = (float)spriteSize.Height / textureSize.Height;
            float oldX = position.X;
            float oldY = position.Y;
            int oldWidth = spriteSize.Width;
            int oldHeight = spriteSize.Height;

            sprite.ClipRect.Value.Clip(ref position, ref spriteSize);
            int textureX = textureOffset.X;
            int textureY = textureOffset.Y + MathUtil.Round((position.Y - oldY) / textureHeightFactor);
            textureSize = new(textureSize.Width - MathUtil.Round((oldWidth - spriteSize.Width) / textureWidthFactor),
                textureSize.Height - MathUtil.Round((oldHeight - spriteSize.Height) / textureHeightFactor));

            if (sprite.MirrorX)
            {
                float oldRight = oldX + oldWidth;
                float newRight = position.X + spriteSize.Width;
                textureX += MathUtil.Round((oldRight - newRight) / textureWidthFactor);
            }
            else
            {
                textureX += MathUtil.Round((position.X - oldX) / textureWidthFactor);
            }

            textureOffset = new(textureX, textureY);
        }

        textureSize = new Size(textureSize.Width * textureFactor, textureSize.Height * textureFactor);

        if (sprite.MirrorX)
        {
            textureOffsetBuffer.Update(index, (short)(textureOffset.X + textureSize.Width), (short)textureOffset.Y);
            textureOffsetBuffer.Update(index + 1, (short)textureOffset.X, (short)textureOffset.Y);
            textureOffsetBuffer.Update(index + 2, (short)textureOffset.X, (short)(textureOffset.Y + textureSize.Height));
            textureOffsetBuffer.Update(index + 3, (short)(textureOffset.X + textureSize.Width), (short)(textureOffset.Y + textureSize.Height));
        }
        else
        {
            textureOffsetBuffer.Update(index, (short)textureOffset.X, (short)textureOffset.Y);
            textureOffsetBuffer.Update(index + 1, (short)(textureOffset.X + textureSize.Width), (short)textureOffset.Y);
            textureOffsetBuffer.Update(index + 2, (short)(textureOffset.X + textureSize.Width), (short)(textureOffset.Y + textureSize.Height));
            textureOffsetBuffer.Update(index + 3, (short)textureOffset.X, (short)(textureOffset.Y + textureSize.Height));
        }
    }

    public void UpdateColor(int index, Color color)
    {
		var colorBuffer = GetBuffer<ColorBuffer, byte>(BufferPurpose.Color);

		if (colorBuffer != null)
        {
            colorBuffer.Update(index, color);
            colorBuffer.Update(index + 1, color);
            colorBuffer.Update(index + 2, color);
            colorBuffer.Update(index + 3, color);
        }
    }

    public void UpdateDisplayLayer(int index, byte displayLayer)
    {
		var layerBuffer = GetBuffer<ByteBuffer, byte>(BufferPurpose.DisplayLayer);

		if (layerBuffer != null)
        {
            layerBuffer.Update(index, displayLayer);
            layerBuffer.Update(index + 1, displayLayer);
            layerBuffer.Update(index + 2, displayLayer);
            layerBuffer.Update(index + 3, displayLayer);
        }
    }

    public void UpdateAlpha(int index, byte alpha)
    {
		var alphaBuffer = GetBuffer<ByteBuffer, byte>(BufferPurpose.Alpha);

		if (alphaBuffer != null)
        {
            alphaBuffer.Update(index, alpha);
            alphaBuffer.Update(index + 1, alpha);
            alphaBuffer.Update(index + 2, alpha);
            alphaBuffer.Update(index + 3, alpha);
        }
    }

    public void UpdatePaletteIndex(int index, byte paletteIndex)
    {
		var paletteIndexBuffer = GetBuffer<ByteBuffer, byte>(BufferPurpose.PaletteIndex);

		if (paletteIndexBuffer != null)
        {
            paletteIndexBuffer.Update(index, paletteIndex);
            paletteIndexBuffer.Update(index + 1, paletteIndex);
            paletteIndexBuffer.Update(index + 2, paletteIndex);
            paletteIndexBuffer.Update(index + 3, paletteIndex);
        }
    }

    public void FreeDrawIndex(int index)
    {
        foreach (var buffer in buffers)
        {
            // Note: The inverse index freeing is crucial as the next free index is taken from the end of the list.
            if (buffer.Value is FloatPositionBuffer positionBuffer)
            {
				// ensure it is not visible
				positionBuffer.Update(index + 3, float.MaxValue, float.MaxValue);
				positionBuffer.Update(index + 2, float.MaxValue, float.MaxValue);
				positionBuffer.Update(index + 1, float.MaxValue, float.MaxValue);
				positionBuffer.Update(index + 0, float.MaxValue, float.MaxValue);
			}
            else if (buffer.Value is VectorBuffer vectorBuffer)
			{
				// ensure it is not visible
				vectorBuffer.Update(index + 3, float.MaxValue, float.MaxValue, float.MaxValue);
				vectorBuffer.Update(index + 2, float.MaxValue, float.MaxValue, float.MaxValue);
				vectorBuffer.Update(index + 1, float.MaxValue, float.MaxValue, float.MaxValue);
				vectorBuffer.Update(index + 0, float.MaxValue, float.MaxValue, float.MaxValue);
			}

			buffer.Value.Remove(index + 3);
            buffer.Value.Remove(index + 2);
            buffer.Value.Remove(index + 1);
            buffer.Value.Remove(index + 0);
        }
    }

    public void Render()
    {
        if (disposed)
            return;

        vertexArrayObject.Bind();

        unsafe
        {
            vertexArrayObject.Lock();

            try
            {
                if (buffers.TryGetValue(BufferPurpose.Position2D, out var buffer))
                    state.Gl.DrawElements(PrimitiveType.Triangles, (uint)(buffer.Size / 4) * 3, DrawElementsType.UnsignedInt, (void*)0);
                else if (buffers.TryGetValue(BufferPurpose.Position3D, out buffer))
					state.Gl.DrawElements(PrimitiveType.Triangles, (uint)buffer.Size / 2, DrawElementsType.UnsignedInt, (void*)0);
            }
            catch
            {
                // ignore for now
            }
            finally
            {
                vertexArrayObject.Unlock();
            }
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            vertexArrayObject?.Dispose();

            foreach (var buffer in buffers)
                buffer.Value.Dispose();
            buffers.Clear();

			indexBuffer?.Dispose();

            disposed = true;
        }
    }
}
