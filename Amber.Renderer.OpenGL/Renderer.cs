/*
 * Rendererer.cs - Implementation of a OpenGL renderer
 *
 * Copyright (C) 2024-2026  Robert Schneckenhaus <robert.schneckenhaus@web.de>
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
using Amber.Common;
using Amber.Renderer.Common;

namespace Amber.Renderer.OpenGL
{
    public class Renderer : IRenderer, IDisposable
    {
        bool disposed = false;
        readonly State state;
		readonly LayerFactory layerFactory;
		readonly TextureFactory textureFactory;
		readonly Camera3D camera;
		readonly List<ILayer> layers = [];
		readonly Size virtualSize;

        public Renderer(IContextProvider contextProvider, Size size, Size virtualSize)
        {
			this.virtualSize = virtualSize;
            state = new(contextProvider);
			layerFactory = new(state);
			textureFactory = new(state);

			state.Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

			state.Gl.Enable(EnableCap.DepthTest);
			state.Gl.DepthRange(0.0f, 1.0f);
			state.Gl.DepthFunc(DepthFunction.Lequal);
            state.Gl.Disable(EnableCap.CullFace);
            state.Gl.Enable(EnableCap.CullFace);
            state.Gl.CullFace(GLEnum.Back);
            state.Gl.FrontFace(FrontFaceDirection.CW);
            state.Gl.Enable(EnableCap.Texture2D);

			state.Gl.BlendEquationSeparate(BlendEquationModeEXT.FuncAdd, BlendEquationModeEXT.FuncAdd);
			state.Gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.Zero);

            state.VirtualProjectionMatrix2D = Matrix4.CreateOrtho2D(0, virtualSize.Width, 0, virtualSize.Height, 0, 1);

            camera = new Camera3D(position: new Vector3(5, 5, 0));

            Resize(size);
        }

		public Size Size { get; private set; }

		public IReadOnlyList<ILayer> Layers => layers.Cast<ILayer>().ToList().AsReadOnly();

		public ILayerFactory LayerFactory => layerFactory;

		public ITextureFactory TextureFactory => textureFactory;

		public ICamera3D Camera => camera;

		public void Render()
		{
			foreach (var layer in layers)
				layer.Render(this);
		}

		public void Resize(Size size)
		{
			Size = size;

            state.WindowProjectionMatrix2D = Matrix4.CreateOrtho2D(0, size.Width, 0, size.Height, 0, 1);
            state.ProjectionMatrix3D = Matrix4.CreatePerspective(60.0f, (float)size.Width / size.Height, 0.1f, 1000.0f);

			state.ClearMatrices();
			state.PushModelViewMatrix(Matrix4.Identity);
			state.PushProjectionMatrix(state.VirtualProjectionMatrix2D);

            // TODO: viewport offset
            state.Gl.Viewport(0, 0, (uint)size.Width, (uint)size.Height);
		}

		public Position ToScreen(Position position)
		{
			// TODO: viewport offset

			float factorX = (float)Size.Width / virtualSize.Width;
			float factorY = (float)Size.Height / virtualSize.Height;

			return new(MathUtil.Round(factorX * position.X), MathUtil.Round(factorY * position.Y));
		}

		public Size ToScreen(Size size)
		{
            float factorX = (float)Size.Width / virtualSize.Width;
            float factorY = (float)Size.Height / virtualSize.Height;

            return new(MathUtil.Round(factorX * size.Width), MathUtil.Round(factorY * size.Height));
        }

		public Position FromScreen(Position position)
		{
            // TODO: viewport offset

            float factorX = (float)virtualSize.Width / Size.Width;
            float factorY = (float)virtualSize.Height / Size.Height;

            return new(MathUtil.Round(factorX * position.X), MathUtil.Round(factorY * position.Y));
        }

		public Size FromScreen(Size size)
		{
            float factorX = (float)virtualSize.Width / Size.Width;
            float factorY = (float)virtualSize.Height / Size.Height;

            return new(MathUtil.Round(factorX * size.Width), MathUtil.Round(factorY * size.Height));
        }

		public void AddLayer(ILayer layer)
		{
			layers.Remove(layer);
			layers.Add(layer);
		}

		public void RemoveLayer(ILayer layer)
		{
			layers.Remove(layer);
		}

        public void Dispose()
		{
			if (disposed)
				return;

			// TODO

			disposed = true;
		}
	}
}
