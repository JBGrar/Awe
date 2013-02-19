#region File Description
//-----------------------------------------------------------------------------
// ModelViewerControl.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace AweEditor
{
    /// <summary>
    /// Example control inherits from GraphicsDeviceControl, and displays
    /// a voxel terrain. The main form class is responsible for loading
    /// the terrain: this control just displays it.
    /// </summary>
    class TerrainViewerControl : GraphicsDeviceControl
    {
        /// <summary>
        /// Gets or sets the current voxel terrain.
        /// </summary>
		public VoxelTerrain VoxelTerrain
		{
			get
			{
				return voxelTerrain;
			}
			set
			{
				voxelTerrain = value;
				if (voxelTerrain != null)
				{
					for (int cx = 0; cx < 32; cx++)
					{
						for (int cz = 0; cz < 32; cz++)
						{
							for (int y = 0; y < 256; y++)
							{
								for (int z = 0; z < 16; z++)
								{
									for (int x = 0; x < 16; x++)
									{
										BlockType temp = voxelTerrain.GetBlock(cx, cz, x, y, z);
										if (temp != BlockType.Air)
										{
											instances.Add(new BlockInstance(temp,cx*16+x,y,cz*16+z));
										}

									}
								}
							}
						}
					}
				}
			}
		}


        VoxelTerrain voxelTerrain;

		/// <summary>
		/// Gets or sets the current model.
		/// </summary>
		public Model Model
		{
			get { return model; }

			set
			{
				model = value;

				if (model != null)
				{
					MeasureModel();
				}
			}
		}

		Model model;

		DynamicVertexBuffer instanceVertexBuffer;


		// To store instance transform matrices in a vertex buffer, we use this custom
		// vertex type which encodes 4x4 matrices as a set of four Vector4 values.
		static VertexDeclaration instanceVertexDeclaration = new VertexDeclaration
		(
			new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
			new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
			new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
			new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3)
		);

		List<BlockInstance> instances;
		Matrix[] instanceTransforms;
		Matrix[] instancedModelBones;

		// Cache information about the model size and position.
		Matrix[] boneTransforms;
		Vector3 modelCenter;
		float modelRadius;


		// Timer controls the rotation speed.
		Stopwatch timer;


		/// <summary>
		/// Initializes the control.
		/// </summary>
		protected override void Initialize()
		{
			// Start the animation timer.
			timer = Stopwatch.StartNew();
			instances = new List<BlockInstance>();
			//instances.Add(new BlockInstance());
			// Hook the idle event to constantly redraw our animation.
			Application.Idle += delegate { Invalidate(); };
		}


		/// <summary>
		/// Draws the control.
		/// </summary>
		protected override void Draw()
		{
			// Clear to the default control background color.
			Color backColor = new Color(BackColor.R, BackColor.G, BackColor.B);

			GraphicsDevice.Clear(backColor);

			if (model != null)
			{
				// Compute camera matrices.
				//float rotation = 0;

				/*Vector3 eyePosition = modelCenter;

				eyePosition.Z += modelRadius * 2;
				eyePosition.Y += modelRadius;
				*/
				//float aspectRatio = GraphicsDevice.Viewport.AspectRatio;

				//float nearClip = modelRadius / 100;
				//float farClip = modelRadius * 100;

				//Matrix world = Matrix.CreateRotationY(rotation);

				Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 15),
											  Vector3.Zero, Vector3.Up);
				Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
																	GraphicsDevice.Viewport.AspectRatio,
																	1,
																	100);
				// Set renderstates for drawing 3D models.
				GraphicsDevice.BlendState = BlendState.Opaque;
				GraphicsDevice.DepthStencilState = DepthStencilState.Default;

				Array.Resize(ref instanceTransforms, instances.Count);

				for (int i = 0; i < instances.Count; i++)
				{
					instanceTransforms[i] = instances[i].Transform;
				}

				DrawModelHardwareInstancing(model, instancedModelBones,
												instanceTransforms, view, projection);
				
				// Draw the model.
				/*
				foreach (ModelMesh mesh in model.Meshes)
				{
					foreach (BasicEffect effect in mesh.Effects)
					{
						effect.World = boneTransforms[mesh.ParentBone.Index] * world;
						effect.View = view;
						effect.Projection = projection;

						effect.EnableDefaultLighting();
						effect.PreferPerPixelLighting = true;
						effect.SpecularPower = 16;
					}

					mesh.Draw();
				}*/
			}
		}


		/// <summary>
		/// Efficiently draws several copies of a piece of geometry using hardware instancing.
		/// </summary>
		void DrawModelHardwareInstancing(Model model, Matrix[] modelBones,
										 Matrix[] instances, Matrix view, Matrix projection)
		{
			if (instances.Length == 0)
				return;

			// If we have more instances than room in our vertex buffer, grow it to the neccessary size.
			if ((instanceVertexBuffer == null) ||
				(instances.Length > instanceVertexBuffer.VertexCount))
			{
				if (instanceVertexBuffer != null)
					instanceVertexBuffer.Dispose();

				instanceVertexBuffer = new DynamicVertexBuffer(GraphicsDevice, instanceVertexDeclaration,
															   instances.Length, BufferUsage.WriteOnly);
			}

			// Transfer the latest instance transform matrices into the instanceVertexBuffer.
			instanceVertexBuffer.SetData(instances, 0, instances.Length, SetDataOptions.Discard);

			foreach (ModelMesh mesh in model.Meshes)
			{
				foreach (ModelMeshPart meshPart in mesh.MeshParts)
				{
					// Tell the GPU to read from both the model vertex buffer plus our instanceVertexBuffer.
					GraphicsDevice.SetVertexBuffers(
						new VertexBufferBinding(meshPart.VertexBuffer, meshPart.VertexOffset, 0),
						new VertexBufferBinding(instanceVertexBuffer, 0, 1)
					);

					GraphicsDevice.Indices = meshPart.IndexBuffer;

					// Set up the instance rendering effect.
					Effect effect = meshPart.Effect;

					effect.CurrentTechnique = effect.Techniques["HardwareInstancing"];

					effect.Parameters["World"].SetValue(modelBones[mesh.ParentBone.Index]);
					effect.Parameters["View"].SetValue(view);
					effect.Parameters["Projection"].SetValue(projection);

					// Draw all the instance copies in a single call.
					foreach (EffectPass pass in effect.CurrentTechnique.Passes)
					{
						pass.Apply();

						GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
															   meshPart.NumVertices, meshPart.StartIndex,
															   meshPart.PrimitiveCount, instances.Length);
					}
				}
			}
		}


		/// <summary>
		/// Whenever a new model is selected, we examine it to see how big
		/// it is and where it is centered. This lets us automatically zoom
		/// the display, so we can correctly handle models of any scale.
		/// </summary>
		void MeasureModel()
		{
			// Look up the absolute bone transforms for this model.
			boneTransforms = new Matrix[model.Bones.Count];
			instancedModelBones = new Matrix[model.Bones.Count];
			model.CopyAbsoluteBoneTransformsTo(instancedModelBones);
			model.CopyAbsoluteBoneTransformsTo(boneTransforms);

			// Compute an (approximate) model center position by
			// averaging the center of each mesh bounding sphere.
			modelCenter = Vector3.Zero;

			foreach (ModelMesh mesh in model.Meshes)
			{
				BoundingSphere meshBounds = mesh.BoundingSphere;
				Matrix transform = boneTransforms[mesh.ParentBone.Index];
				Vector3 meshCenter = Vector3.Transform(meshBounds.Center, transform);

				modelCenter += meshCenter;
			}

			modelCenter /= model.Meshes.Count;

			// Now we know the center point, we can compute the model radius
			// by examining the radius of each mesh bounding sphere.
			modelRadius = 0;

			foreach (ModelMesh mesh in model.Meshes)
			{
				BoundingSphere meshBounds = mesh.BoundingSphere;
				Matrix transform = boneTransforms[mesh.ParentBone.Index];
				Vector3 meshCenter = Vector3.Transform(meshBounds.Center, transform);

				float transformScale = transform.Forward.Length();

				float meshRadius = (meshCenter - modelCenter).Length() +
								   (meshBounds.Radius * transformScale);

				modelRadius = Math.Max(modelRadius, meshRadius);
			}
			foreach (BlockInstance inst in instances)
			{
				inst.Update();
			}
		}
    }
}
