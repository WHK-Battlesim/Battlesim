using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class MapGenerator : MonoBehaviour
    {
        public Texture2D DefaultHeightMap;
        public Material Material;

        public Vector3 Offset = Vector3.zero;
        public Vector3 Scale = new Vector3(10, 10, 10);
        public int Resolution = 100;

        private void Start ()
        {
            BuildTerrain(DefaultHeightMap);
        }

        public void BuildTerrain(Texture2D heightMap)
        {
            var terrain = new GameObject("Terrain");
            terrain.transform.SetParent(transform);

            var meshFilter = terrain.AddComponent<MeshFilter>();
            var meshRenderer = terrain.AddComponent<MeshRenderer>();

            var mesh = meshFilter.mesh;
            mesh.Clear();

            // TODO: generate some random Delauney triangles and build mesh based on that

            mesh.SetVertices(GetVertices(heightMap).ToList());
            mesh.SetTriangles(GetTriangles(), 0);
            mesh.RecalculateNormals();

            meshRenderer.material = Material;
        }

        private IEnumerable<Vector3> GetVertices(Texture2D heightMap)
        {
            var vertices = new Vector3[Resolution * Resolution];
            var floatResolution = (float) Resolution;

            for (var z = 0; z < Resolution; z++)
            {
                for (var x = 0; x < Resolution; x++)
                {
                    var y = heightMap.GetPixelBilinear(x / floatResolution, z / floatResolution).r;

                    vertices[x + z * Resolution] = Offset + Vector3.Scale(new Vector3(x / floatResolution, y, z / floatResolution), Scale);
                }
            }

            return vertices;
        }

        private int[] GetTriangles()
        {
            var quadResolution = Resolution - 1;
            var triangles = new int[quadResolution * quadResolution * 2 * 3];

            Func<int, int, int> triangleId = (x, z) => (x + z * quadResolution) * 2 * 3;
            Func<int, int, int> vertexId = (x, z) => x + z * Resolution;

            for (var z = 0; z < Resolution - 1; z++)
            {
                for (var x = 0; x < Resolution - 1; x++)
                {
                    var triangleOffset = triangleId(x, z);
                    var index = 0;
                    
                    triangles[triangleOffset + index++] = vertexId(x, z);
                    triangles[triangleOffset + index++] = vertexId(x, z + 1);
                    triangles[triangleOffset + index++] = vertexId(x + 1, z);

                    triangles[triangleOffset + index++] = vertexId(x + 1, z);
                    triangles[triangleOffset + index++] = vertexId(x, z + 1);
                    triangles[triangleOffset + index] = vertexId(x + 1, z + 1);
                }
            }

            return triangles;
        }
    }
}
