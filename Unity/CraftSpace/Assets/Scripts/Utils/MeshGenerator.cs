using UnityEngine;

namespace CraftSpace.Utils
{
    public static class MeshGenerator
    {
        /// <summary>
        /// Creates a flat, table-oriented quad mesh with the right orientation for displaying covers
        /// </summary>
        /// <param name="width">Width of the quad</param>
        /// <param name="height">Height of the quad</param>
        /// <param name="pivot">Pivot point for the mesh (0,0 is bottom left, 0.5,0.5 is center)</param>
        /// <returns>A new mesh instance</returns>
        public static Mesh CreateFlatQuad(float width, float height, Vector2 pivot = default)
        {
            Mesh mesh = new Mesh();
            
            // Default pivot to center if not specified
            if (pivot == default)
                pivot = new Vector2(0.5f, 0.5f);
                
            // Calculate offsets based on pivot
            float xOffset = -width * pivot.x;
            float yOffset = -height * pivot.y;
            
            // Vertices - with Y as up, flat on the XZ plane (table-oriented)
            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(xOffset, 0, yOffset),                       // Bottom left
                new Vector3(xOffset + width, 0, yOffset),               // Bottom right
                new Vector3(xOffset, 0, yOffset + height),              // Top left
                new Vector3(xOffset + width, 0, yOffset + height)       // Top right
            };
            
            // UVs - standard mapping
            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),     // Bottom left
                new Vector2(1, 0),     // Bottom right
                new Vector2(0, 1),     // Top left
                new Vector2(1, 1)      // Top right
            };
            
            // Triangles - two triangles forming the quad
            int[] triangles = new int[6]
            {
                0, 2, 1,    // First triangle
                2, 3, 1     // Second triangle
            };
            
            // Set the mesh data
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            
            // Recalculate normals and bounds
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        /// <summary>
        /// Updates an existing mesh to match new dimensions while maintaining orientation
        /// </summary>
        /// <param name="mesh">The mesh to update</param>
        /// <param name="width">New width</param>
        /// <param name="height">New height</param>
        /// <param name="pivot">Pivot point (0,0 is bottom left, 0.5,0.5 is center)</param>
        public static void ResizeQuadMesh(Mesh mesh, float width, float height, Vector2 pivot = default)
        {
            if (mesh == null)
                return;
                
            // Default pivot to center if not specified
            if (pivot == default)
                pivot = new Vector2(0.5f, 0.5f);
                
            // Calculate offsets based on pivot
            float xOffset = -width * pivot.x;
            float yOffset = -height * pivot.y;
            
            // Get existing vertices
            Vector3[] vertices = mesh.vertices;
            
            // Update vertex positions
            vertices[0] = new Vector3(xOffset, 0, yOffset);                  // Bottom left
            vertices[1] = new Vector3(xOffset + width, 0, yOffset);          // Bottom right
            vertices[2] = new Vector3(xOffset, 0, yOffset + height);         // Top left
            vertices[3] = new Vector3(xOffset + width, 0, yOffset + height); // Top right
            
            // Update the mesh
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
        }

        // Add this method to create a properly sized cover mesh
        public static Mesh CreateCoverMesh(float width, float height, Vector2 pivot = default)
        {
            // Use our existing CreateFlatQuad logic
            return CreateFlatQuad(width, height, pivot);
        }

        /// <summary>
        /// Creates a flat quad with a standard book cover aspect ratio (2:3)
        /// </summary>
        /// <param name="maxWidth">Maximum width</param>
        /// <param name="maxHeight">Maximum height</param>
        /// <returns>A new mesh instance with book proportions</returns>
        public static Mesh CreateStandardBookCoverMesh(float maxWidth, float maxHeight)
        {
            // Standard book cover ratio is approximately 2:3 (width:height)
            const float BOOK_ASPECT_RATIO = 2f/3f;
            
            float width, height;
            
            // Calculate dimensions that fit within max boundaries while maintaining book ratio
            if (maxWidth * (1f/BOOK_ASPECT_RATIO) <= maxHeight)
            {
                // Width is the limiting factor
                width = maxWidth;
                height = width * (1f/BOOK_ASPECT_RATIO);
            }
            else
            {
                // Height is the limiting factor
                height = maxHeight;
                width = height * BOOK_ASPECT_RATIO;
            }
            
            // Create the quad with calculated dimensions
            return CreateFlatQuad(width, height);
        }
    }
} 