using UnityEngine;

namespace Assets.Scripts
{
    public class ReplaceShader : MonoBehaviour
    {
        private void Start ()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            var shader = Shader.Find("Standard (Flat Lighting)");
            foreach (var material in meshRenderer.materials)
            {
                material.shader = shader;
            }
        }
    }
}
