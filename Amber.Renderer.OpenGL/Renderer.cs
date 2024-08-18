/*
 * Rendererer.cs - Implementation of a OpenGL renderer
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
using Amber.Renderer;
using Amber.Renderer.Common;
using Amber.Renderer.OpenGL;
using System.Numerics;

namespace Ambermoon.Renderer.OpenGL
{
    public class Renderer : IRenderer, IDisposable
    {
        bool disposed = false;
        readonly State state;
		readonly LayerFactory layerFactory;
		readonly TextureFactory textureFactory;
		readonly List<ILayer> layers = [];

        public Renderer(IContextProvider contextProvider, Size size, Size virtualSize)
        {
            state = new(contextProvider);
			layerFactory = new(state);
			textureFactory = new(state);

			state.Gl.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);

			state.Gl.Enable(EnableCap.DepthTest);
			state.Gl.DepthRange(0.0f, 1.0f);
			state.Gl.DepthFunc(DepthFunction.Lequal);
			state.Gl.Disable(EnableCap.CullFace);

			state.Gl.Disable(EnableCap.Blend);
			state.Gl.BlendEquationSeparate(BlendEquationModeEXT.FuncAdd, BlendEquationModeEXT.FuncAdd);
			state.Gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.Zero);

			state.ProjectionMatrix2D = Matrix4x4.CreateOrthographic(virtualSize.Width, virtualSize.Height, 0, 1);

			Resize(size);
        }

		public Size Size => throw new NotImplementedException();

		public IReadOnlyList<ILayer> Layers => layers.Cast<ILayer>().ToList().AsReadOnly();

		public ILayerFactory LayerFactory => layerFactory;

		public ITextureFactory TextureFactory => textureFactory;

		public void Render()
		{
			foreach (var layer in layers)
				layer.Render(this);
		}

		public void Resize(Size size)
		{
			//state.ProjectionMatrix3D = Matrix4x4.CreatePerspectiveFieldOfView(FovY3D, aspect, 0.1f, 40.0f * Global.DistancePerBlock); // Max 3D map dimension is 41

			state.ClearMatrices();
			state.PushModelViewMatrix(Matrix4x4.Identity);
			state.PushProjectionMatrix(state.ProjectionMatrix2D);

			state.Gl.Viewport(0, 0, (uint)size.Width, (uint)size.Height);
		}

		public Position ToScreen(Position position)
		{
			throw new NotImplementedException();
		}

		public Size ToScreen(Size size)
		{
			throw new NotImplementedException();
		}

		public Position FromScreen(Position position)
		{
			throw new NotImplementedException();
		}

		public Size FromScreen(Size size)
		{
			throw new NotImplementedException();
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
		}
	}
}
