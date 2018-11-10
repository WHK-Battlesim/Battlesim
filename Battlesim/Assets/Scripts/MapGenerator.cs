using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TriangleNet;
using TriangleNet.Geometry;
using TriangleNet.Meshing;

namespace Assets.Scripts
{
    public class MapGenerator : MonoBehaviour
    {
        public Texture2D DefaultHeightMap;
        public Material Material;

        public Vector3 Offset = Vector3.zero;
        public Vector3 Scale = new Vector3(10, 1, 10);
        public int NumberOfVertices = 10000;

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
            
            var polygon = new Polygon();
            var random = new System.Random();
            for (var i = 0; i < NumberOfVertices; i++)
            {
                polygon.Add(new Vertex(random.NextDouble(), random.NextDouble()));
            }

            var options = new ConstraintOptions {ConformingDelaunay = true};
            var triangulatedMesh = polygon.Triangulate(options);

            mesh.SetVertices(triangulatedMesh.Vertices.Select(v => Vector3.Scale(new Vector3((float) v.X, heightMap.GetPixelBilinear((float) v.X, (float) v.Y).r, (float) v.Y), Scale) + Offset).ToList());
            mesh.SetTriangles(triangulatedMesh.Triangles.SelectMany(t => t.vertices.Reverse(), (t, v) => v.ID).ToArray(), 0);
            mesh.RecalculateNormals();

            meshRenderer.material = Material;
        }
    }
}
