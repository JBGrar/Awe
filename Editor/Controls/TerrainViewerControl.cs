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
			get { return voxelTerrain; }
			set { voxelTerrain = value;
			GetInstancesFromVoxelTerrain();
			}
		}

		

		VoxelTerrain voxelTerrain;

		List<BlockInstance> instances;
		Matrix[] instanceTransforms;
		private Model instancedModel;
		Matrix[] instancedModelBones;
		DynamicVertexBuffer instanceVertexBuffer;

		public Model InstancedModel
		{
			get
			{
				return instancedModel;
			}
			set
			{
				if (value != null)
				{
					instancedModel = value;
					instancedModelBones = new Matrix[InstancedModel.Bones.Count];
					instancedModel.CopyAbsoluteBoneTransformsTo(instancedModelBones);
				}
			}
		}

		private void GetInstancesFromVoxelTerrain()
		{
			instances = new List<BlockInstance>();
			BlockType temp;
			if (voxelTerrain != null)
			{
				for (int cz = 0; cz < 32; cz++)
				{
					for (int cx = 0; cx < 32; cx++)
					{
						for (int y = 0; y < 256; y++)
						{
							for (int z = 0; z < 16; z++)
							{
								for (int x = 0; x < 16; x++)
								{
									temp = voxelTerrain.GetBlockInChunk(cz, cx, x, y, z);
									if (temp != BlockType.Air) 
										instances.Add(new BlockInstance(x + (cx * 32), y, z + (cz * 32), voxelTerrain.GetBlockInChunk(cz, cx, x, y, z)));
								}
							}
						}
					}
				}
			}
			MessageBox.Show("" + instances.Count);
		}

		// To store instance transform matrices in a vertex buffer, we use this custom
		// vertex type which encodes 4x4 matrices as a set of four Vector4 values.
		static VertexDeclaration instanceVertexDeclaration = new VertexDeclaration
		(
			new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
			new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
			new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
			new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3)
		);


		/*Vector3 modelCenter;

		public Model InstanceModel
		{
			get
			{
				return instanceModel;
			}
			set
			{
				instanceModel = value;

				if (instanceModel != null)
				{
					MeasureModel();
				}
			}
		}

		private Model instanceModel;
		private Matrix[] instanceModelBones;
		float modelRadius;
		DynamicVertexBuffer instanceVertexBuffer;
		List<BlockInstance> instances;
		Matrix[] instanceTransforms;

		static VertexDeclaration instanceVertexDeclaration = new VertexDeclaration
		(
			new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
			new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 1),
			new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 2),
			new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 3)
		);

		private void MeasureModel()
		{
			instanceModelBones = new Matrix[instanceModel.Bones.Count];

			instanceModel.CopyAbsoluteBoneTransformsTo(instanceModelBones);

			// Compute an (approximate) model center position by
			// averaging the center of each mesh bounding sphere.
			modelCenter = Vector3.Zero;

			foreach (ModelMesh mesh in instanceModel.Meshes)
			{
				BoundingSphere meshBounds = mesh.BoundingSphere;
				Matrix transform = instanceModelBones[mesh.ParentBone.Index];
				Vector3 meshCenter = Vector3.Transform(meshBounds.Center, transform);

				modelCenter += meshCenter;
			}

			modelCenter /= instanceModel.Meshes.Count;

			// Now we know the center point, we can compute the model radius
			// by examining the radius of each mesh bounding sphere.
			modelRadius = 0;

			foreach (ModelMesh mesh in instanceModel.Meshes)
			{
				BoundingSphere meshBounds = mesh.BoundingSphere;
				Matrix transform = instanceModelBones[mesh.ParentBone.Index];
				Vector3 meshCenter = Vector3.Transform(meshBounds.Center, transform);

				float transformScale = transform.Forward.Length();

				float meshRadius = (meshCenter - modelCenter).Length() +
								   (meshBounds.Radius * transformScale);

				modelRadius = Math.Max(modelRadius, meshRadius);
			}
		}

		/// <summary>
		/// Initializes the control.
		/// </summary>
*/

		protected override void Initialize()
		{
			// Hook the idle event to constantly redraw our animation.
			Application.Idle += delegate { Invalidate(); };
			
		}

		protected override void Draw()
		{
			// Clear to the default control background color.
			Color backColor = new Color(BackColor.R, BackColor.G, BackColor.B);

			GraphicsDevice.Clear(backColor);
			voxelTerrain = new VoxelTerrain();
			if (voxelTerrain != null)
			{
				Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 15),
												Vector3.Zero, Vector3.Up);
				
				Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4,
                                                                    GraphicsDevice.Viewport.AspectRatio,
                                                                    1, 
                                                                    100);

				//Renderstates for drawing 3d models
				GraphicsDevice.BlendState = BlendState.Opaque;
				GraphicsDevice.DepthStencilState = DepthStencilState.Default;

				//Gather instance transform matrices into a single array.
				Array.Resize(ref instanceTransforms, instances.Count);

				for (int i = 0; i < instances.Count; i++)
				{
					instanceTransforms[i] = instances[i].Transform;
				}

				//Draw All the instances
				DrawInstancedModels(instancedModel, instancedModelBones,
									instanceTransforms, view, projection);

			}
			throw new NotImplementedException();
		}


		void DrawInstancedModels(Model model, Matrix[] modelBones,
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

		/*
		/// <summary>
		/// Draws the control.
		/// </summary>
		protected override void Draw()
		{
			// Clear to the default control background color.
			Color backColor = new Color(BackColor.R, BackColor.G, BackColor.B);

			GraphicsDevice.Clear(backColor);
			Matrix view = Matrix.CreateLookAt(new Vector3(0, 0, 15),
												Vector3.Zero, Vector3.Up);

			Matrix projection = Matrix.CreatePerspective(MathHelper.PiOver4,
															GraphicsDevice.Viewport.AspectRatio,
															1,
															100);

			GraphicsDevice.BlendState = BlendState.Opaque;
			GraphicsDevice.DepthStencilState = DepthStencilState.Default;

			Array.Resize(ref instanceTransforms, instances.Count);

			for (int i = 0; i < instances.Count; i++)
			{
				instanceTransforms[i] = instances[i].Transform;
			}

			if((instanceVertexBuffer==null)||(instanceTransforms.Length>instanceVertexBuffer.VertexCount))
			{
				if (instanceVertexBuffer != null)
					instanceVertexBuffer.Dispose();

				instanceVertexBuffer = new DynamicVertexBuffer(GraphicsDevice, instanceVertexDeclaration,
											   instanceTransforms.Length, BufferUsage.WriteOnly);

			}

			// Transfer the latest instance transform matrices into the instanceVertexBuffer.
			instanceVertexBuffer.SetData(instanceTransforms, 0, instanceTransforms.Length, SetDataOptions.Discard);

			foreach (ModelMesh mesh in instanceModel.Meshes)
			{
				foreach (ModelMeshPart meshPart in mesh.MeshParts)
				{
					// Tell the GPU to read from both the model vertex buffer plus our instanceVertexBuffer.
					GraphicsDevice.SetVertexBuffers(
						new VertexBufferBinding(meshPart.VertexBuffer, meshPart.VertexOffset, 0),
						new VertexBufferBinding(instanceVertexBuffer, 0, 1)
					);

					GraphicsDevice.Indices = meshPart.IndexBuffer;

					Effect effect = meshPart.Effect;

					effect.CurrentTechnique = effect.Techniques["HardwareInstancing"];

					effect.Parameters["World"].SetValue(instanceModelBones[mesh.ParentBone.Index]);
					effect.Parameters["View"].SetValue(view);
					effect.Parameters["Projection"].SetValue(projection);

					foreach (EffectPass pass in effect.CurrentTechnique.Passes)
					{
						pass.Apply();
						GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0,
									   meshPart.NumVertices, meshPart.StartIndex,
									   meshPart.PrimitiveCount, instanceTransforms.Length);
					}
				}
			}
			
		}*/
	}
}
