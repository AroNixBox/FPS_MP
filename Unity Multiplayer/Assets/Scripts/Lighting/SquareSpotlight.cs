using UnityEngine;

namespace Lighting
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Light))]
    public class SquareSpotlight : MonoBehaviour 
    { 
        [SerializeField] private int textureSize = 128; 
        [SerializeField] private float widthSize = 0.5f;  // Breite des Rechtecks
        [SerializeField] private float heightSize = 0.5f; // Höhe des Rechtecks

        private void OnValidate()
        {
            Light spotLight = GetComponent<Light>();
            spotLight.type = LightType.Spot;
            spotLight.cookie = GenerateRectangleCookie();
        }

        Texture2D GenerateRectangleCookie()
        {
            Texture2D cookie = new Texture2D(textureSize, textureSize);

            Color[] colors = new Color[textureSize * textureSize];

            int widthStart = (int)(textureSize * (0.5f - widthSize / 2));
            int widthEnd = (int)(textureSize * (0.5f + widthSize / 2));

            int heightStart = (int)(textureSize * (0.5f - heightSize / 2));
            int heightEnd = (int)(textureSize * (0.5f + heightSize / 2));

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    if (x >= widthStart && x <= widthEnd && y >= heightStart && y <= heightEnd)
                        colors[y * textureSize + x] = Color.white;
                    else
                        colors[y * textureSize + x] = Color.black;
                }
            }

            cookie.SetPixels(colors);
            cookie.Apply();
            return cookie;
        }
    }
}