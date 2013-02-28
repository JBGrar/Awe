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
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AweEditor;
#endregion

namespace AweEditor
{
    /// <summary>
    /// The states in which the EditorViewerControl can be
    /// </summary>
    public enum EditorState
    {
        None,
        VoxelTerrain,
        Model,
        Texture,
    }

    /// <summary>
    /// Example control inherits from GraphicsDeviceControl, and displays
    /// a spinning 3D model. The main form class is responsible for loading
    /// the model: this control just displays it.
    /// </summary>
    class EditorViewerControl : GraphicsDeviceControl
    {
        EditorState editorState = EditorState.None;

        // Timer controls the rotation speed.
        Stopwatch timer;

        // SpriteBatch draws sprites on-screen
        SpriteBatch spriteBatch;

        #region Terrain Fields

        /// <summary>
        /// Gets or sets the VoxelTerrain
        /// </summary>
        public VoxelTerrain VoxelTerrain
        {
            get { return voxelTerrain; }
            set { 
                voxelTerrain = value;
                editorState = EditorState.VoxelTerrain;
            }
        }

        VoxelTerrain voxelTerrain;

        #endregion

        #region Model Fields

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

                editorState = EditorState.Model;
            }
        }

        Model model;
        
        // Cache information about the model size and position.
        Matrix[] boneTransforms;
        Vector3 modelCenter;
        float modelRadius;

        #endregion

        #region Texture Fields

        /// <summary>
        /// Gets or sets the current texture
        /// </summary>
        public Texture2D Texture 
        {
            get { return texture; }
            set { 
                texture = value;

                if (texture != null)
                {
                    MeasureTexture();
                }

                editorState = EditorState.Texture;
            }
        }

        Texture2D texture;

        Rectangle textureBounds;

        #endregion


        /// <summary>
        /// Initializes the control.
        /// </summary>
        protected override void Initialize()
        {
            // Start the animation timer.
            timer = Stopwatch.StartNew();

            // Create the SpriteBatch
            spriteBatch = new SpriteBatch(GraphicsDevice);

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

            // Render according to current editor state
            switch (editorState)
            {
                case EditorState.VoxelTerrain:
                    break;

                case EditorState.Model:
                    DrawModel();
                    break;

                case EditorState.Texture:
                    DrawTexture();
                    break;

                default:
                    break;
            }
        }

        #region Drawing Methods

        /// <summary>
        /// Draw the current voxel terrain
        /// </summary>
        private void DrawVoxelTerrain()
        {
        }


        /// <summary>
        /// Draw the current model
        /// </summary>
        private void DrawModel()
        {
            if (model != null)
            {
                // Compute camera matrices.
                float rotation = (float)timer.Elapsed.TotalSeconds;

                Vector3 eyePosition = modelCenter;

                eyePosition.Z += modelRadius * 2;
                eyePosition.Y += modelRadius;

                float aspectRatio = GraphicsDevice.Viewport.AspectRatio;

                float nearClip = modelRadius / 100;
                float farClip = modelRadius * 100;

                Matrix world = Matrix.CreateRotationY(rotation);
                Matrix view = Matrix.CreateLookAt(eyePosition, modelCenter, Vector3.Up);
                Matrix projection = Matrix.CreatePerspectiveFieldOfView(1, aspectRatio,
                                                                    nearClip, farClip);

                // Draw the model.
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
                }
            }
        }

        /// <summary>
        /// Draw the current texture
        /// </summary>
        public void DrawTexture()
        {
            if (texture != null)
            {
                spriteBatch.Begin();
                spriteBatch.Draw(texture, textureBounds, Color.White);
                spriteBatch.End();
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Whenever a new texture is selected, we center it in the window
        /// </summary>
        private void MeasureTexture()
        {
            textureBounds = texture.Bounds;
            Vector2 clientSize = new Vector2(ClientSize.Width, ClientSize.Height);
            Vector2 textureSize = new Vector2(textureBounds.Width, textureBounds.Height);
            Vector2 offset = (clientSize - textureSize) / 2.0f;
            textureBounds.X = (int)offset.X;
            textureBounds.Y = (int)offset.Y;
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

                modelRadius = Math.Max(modelRadius,  meshRadius);
            }
        }

        #endregion
    }
}