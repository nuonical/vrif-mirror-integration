using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace BNG
{
    public class NetworkedWhiteBoard : NetworkBehaviour
    {
        [Tooltip("The Render Texture to draw on")]
        public RenderTexture renderTexture;
        [Header("BrushSettings")]
        [Tooltip("Max distance for brush detection")]
        public float maxDistance = 0.2f;
        [Tooltip("Minimum distance between brush positions")]
        public float minBrushDistance = 2f;

        private Material brushMaterial; // Material used for GL drawing

        public Color backGroundColor;
        [Range(0, 1)]
        public float markerAlpha = 0.7f;
        // Define a Brush class to hold properties for each brush
        [System.Serializable]
        public class BrushSettings
        {
            public Grabbable brushGrabbable; //  Bruch Grabbable
            public NetworkIdentity netID;
            public Transform brushTransform;// Transform of the brush
            public Color color = Color.black;// Brush color
            public int sizeY = 20;// Brush size in pixels
            public int sizeX = 20;// Brush size in pixels
            //public int segmentAmount = 60;// Segment amount for circular brushes

            public bool isEraser = false;

            [HideInInspector] public Vector2 lastPosition; // Last drawn position
            [HideInInspector] public bool isFirstDraw = true; // Flag for first draw
            [HideInInspector] public bool isDrawing = false;  // Whether the brush is in contact
        }

        [Header("Add Brushes")]
        public List<BrushSettings> brushes = new List<BrushSettings>(); // List to hold multiple brushes

        private void Start()
        {
            // Initialize the brush material with a simple shader
            brushMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));

            // Set the Render Texture as the main texture of the object's material
            GetComponent<Renderer>().material.mainTexture = renderTexture;

            // Clear the Render Texture at the start
            RenderTexture.active = renderTexture;
            // set the background color
            GL.Clear(true, true, backGroundColor);
            // set the brush color to match the backgroundColor if it is an eraser
            foreach (BrushSettings brush in brushes)
            {
                // set the alpha level of the markers
                brush.color.a = markerAlpha;

                if (brush.isEraser)
                {
                    brush.color = backGroundColor;
                }
            }
            RenderTexture.active = null;

        }

        public void Update()
        {
           
            // Ensure the Render Texture is active for drawing
           // RenderTexture.active = renderTexture;

            // Draw each brush on the texture
            foreach (var brush in brushes)
            {
                // check if the brush is being held to only run functions for the brushs being used
                if (brush.brushGrabbable && brush.brushGrabbable.BeingHeld)
                {
                    DrawBrushOnTexture(brush);
                }
            }

            // Deactivate the Render Texture after drawing
           // RenderTexture.active = null;
        }


        private void DrawBrushOnTexture(BrushSettings brush)
        {
            if (!brush.netID.isOwned)
                return;

            if (brush.brushTransform == null) return; // null check in case a transfrom isn't assigned

            // ray cast from the brush tip transform
            Ray ray = new Ray(brush.brushTransform.position, brush.brushTransform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                // if the raycast from the brush is hitting this game object whick is the board
                if (hit.collider.gameObject == gameObject)
                {
                    // Calculate the brush position in texture space
                    int x = (int)(hit.textureCoord.x * renderTexture.width);
                    int y = (int)((1.0f - hit.textureCoord.y) * renderTexture.height);
                    Vector2 currentPosition = new Vector2(x, y);

                    // only draw when we need to by comparing current position to the last
                    if (!brush.isDrawing)
                    {
                        // Reset when the brush starts drawing again
                        brush.isFirstDraw = true;
                        brush.isDrawing = true;
                    }

                    if (brush.isFirstDraw)
                    {
                       // DrawAtPosition(currentPosition, brush.color, brush.sizeX, brush.sizeY, brush.brushTransform.rotation.eulerAngles.z);
                        CmdSyncDraw(currentPosition, brush.color, brush.sizeX, brush.sizeY, brush.brushTransform.rotation.eulerAngles.z);
                        brush.lastPosition = currentPosition;
                        brush.isFirstDraw = false;
                        return;
                    }

                    // Check if the texture space coordinates wrap around the edges,
                    // this is so if you are drawing on a 3d object if you move from the left to right and up and down across the textures mapped edge
                    // without it, it will draw a line all the way across the texture
                    float deltaX = Mathf.Abs(currentPosition.x - brush.lastPosition.x);
                    float deltaY = Mathf.Abs(currentPosition.y - brush.lastPosition.y);

                    bool crossesHorizontalEdge = deltaX > renderTexture.width / 16; // Crosses left-right edge
                    bool crossesVerticalEdge = deltaY > renderTexture.height / 16; // Crosses top-bottom edge

                    if (crossesHorizontalEdge || crossesVerticalEdge)
                    {
                        // If crossing an edge, do not interpolate. Just draw at the current position
                       // DrawAtPosition(currentPosition, brush.color, brush.sizeX, brush.sizeY, brush.brushTransform.rotation.eulerAngles.z);
                        CmdSyncDraw(currentPosition, brush.color, brush.sizeX, brush.sizeY, brush.brushTransform.rotation.eulerAngles.z);
                    }
                    else
                    {
                        // Interpolate between the last position and the current position
                        float distance = Vector2.Distance(currentPosition, brush.lastPosition);
                        int steps = Mathf.CeilToInt(distance / minBrushDistance);
                        for (int i = 1; i <= steps; i++)
                        {
                            Vector2 interpolatedPosition = Vector2.Lerp(brush.lastPosition, currentPosition, i / (float)steps);
                           // DrawAtPosition(interpolatedPosition, brush.color, brush.sizeX, brush.sizeY, brush.brushTransform.rotation.eulerAngles.z);
                            CmdSyncDraw(interpolatedPosition, brush.color, brush.sizeX, brush.sizeY, brush.brushTransform.rotation.eulerAngles.z);
                        }
                    }

                    brush.lastPosition = currentPosition; // Update the last drawn position
                }
            }
            else
            {
                // Stop drawing when the brush is no longer in contact
                brush.isDrawing = false;
            }
        }

        private void DrawAtPosition(Vector2 position, Color color, float sizeX, float sizeY, float rotationAngle)        
        {          
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, renderTexture.width, renderTexture.height, 0);

            brushMaterial.SetPass(0);

            GL.Begin(GL.QUADS);
            // GL.Color(brush.color);
            GL.Color(color);

            // Convert rotation angle to radians
            float radians = rotationAngle * Mathf.Deg2Rad;

            // Calculate the rotation matrix components 
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            // Define the local offset vertices of the rectangle relative to the center
            Vector2[] vertices = new Vector2[4];
            vertices[0] = new Vector2(-sizeX, -sizeY); // Bottom-left
            vertices[1] = new Vector2(sizeX, -sizeY);  // Bottom-right
            vertices[2] = new Vector2(sizeX, sizeY);   // Top-right
            vertices[3] = new Vector2(-sizeX, sizeY);  // Top-left

            // Rotate  each vertex to match the brush rotation,
            // this is so you can have a brush that is wide and thin and the paint will match the rotation
            for (int i = 0; i < vertices.Length; i++)
            {
                float rotatedX = vertices[i].x * cos + vertices[i].y * sin; // Positive Y-axis rotation
                float rotatedY = -vertices[i].x * sin + vertices[i].y * cos; // Inverted sine for clockwise rotation

                // Add the position offset to align with the center
                GL.Vertex3(position.x + rotatedX, position.y + rotatedY, 0);
            }

            // populate the pixles 
            GL.End();
            GL.PopMatrix();
        }

        [Command(requiresAuthority = false)]
        public void CmdSyncDraw(Vector2 position, Color color, float sizeX, float sizeY, float rotationAngle)
        {
            RpcSyncDraw(position, color, sizeX, sizeY, rotationAngle);          
        }

        [ClientRpc]
        public void RpcSyncDraw(Vector2 position, Color color, float sizeX, float sizeY, float rotationAngle)
        {
            RenderTexture.active = renderTexture;
            DrawAtPosition(position, color, sizeX, sizeY, rotationAngle);
            RenderTexture.active = null;
        }

    }    
}
