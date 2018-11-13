using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;

namespace Assets.Scripts
{
    public class MapGenerator : MonoBehaviour
    {
        public Texture2D DefaultHeightMap;
        public Texture2D DefaultFeatureMap;
        public TextAsset DefaultExtents;
        public Material Material;

        public Vector3 Offset = Vector3.zero;
        public float Scale = 10;
        public int NumberOfVertices = 10000;

        [Serializable]
        public struct TerrainFeature
        {
            [HideInInspector]
            public string Name;
            public Color FeatureMapColor;
            public Color MeshColor;

        }

        public List<TerrainFeature> TerrainFeatures = new List<TerrainFeature>()
        {
            new TerrainFeature()
            {
                Name = "Land",
                FeatureMapColor = Color.white,
                MeshColor = new Color(0.516f, 0.867f, 0.279f)
            },
            new TerrainFeature()
            {
                Name = "River",
                FeatureMapColor = Color.green,
                MeshColor = new Color(0.2f, 0.2f, 0.8f)
            },
            new TerrainFeature()
            {
                Name = "Lake",
                FeatureMapColor = Color.red,
                MeshColor = new Color(0.2f, 0.2f, 0.8f)
            },
            new TerrainFeature()
            {
                Name = "Fallback",
                FeatureMapColor = Color.magenta,
                MeshColor = Color.magenta
            },
        };

        private void Start()
        {
            BuildTerrain(DefaultHeightMap, DefaultFeatureMap);
        }

        private int GetFeatureId(Texture2D featureMap, Triangle triangle)
        {
            var avgX = (triangle.vertices[0].X + triangle.vertices[1].X + triangle.vertices[2].X) / 3;
            var avgY = (triangle.vertices[0].Y + triangle.vertices[1].Y + triangle.vertices[2].Y) / 3;
            var color = featureMap.GetPixel((int)Math.Round(featureMap.width * avgX),
                                            (int)Math.Round(featureMap.height * avgY));
            var index = TerrainFeatures.FindIndex(terrainFeature => terrainFeature.FeatureMapColor == color) % TerrainFeatures.Count;
            return index;
        }

        public void BuildTerrain(Texture2D heightMap, Texture2D featureMap)
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

            var options = new ConstraintOptions { ConformingDelaunay = true };
            var triangulatedMesh = polygon.Triangulate(options);

            mesh.SetVertices(
                triangulatedMesh
                    .Vertices
                    .Select(
                        vertex => new Vector3(
                                 (float)vertex.X,
                                 heightMap.GetPixelBilinear((float)vertex.X, (float)vertex.Y).r,
                                 (float)vertex.Y)
                             * Scale + Offset)
                    .ToList());

            var triangleGroupVertices =
                triangulatedMesh
                    .Triangles
                    .GroupBy(
                        triangle => GetFeatureId(featureMap, triangle))
                    .Select(
                        grouping => new Tuple<int, IEnumerable<int>>(
                            grouping.Key,
                            grouping.SelectMany(
                                triangle => triangle.vertices.Reverse(),
                                (triangle, vertex) => vertex.ID)))
                    .ToList();

            var triangleGroupCount = triangleGroupVertices.Count();
            mesh.subMeshCount = triangleGroupCount;

            var materials = new Material[triangleGroupCount];
            foreach (var vertexGroup in triangleGroupVertices)
            {
                mesh.SetTriangles(vertexGroup.Item2.ToArray(), vertexGroup.Item1);

                materials[vertexGroup.Item1] = new UnityEngine.Material(Material)
                {
                    color = TerrainFeatures[vertexGroup.Item1].MeshColor
                };
            }

            meshRenderer.materials = materials;
            mesh.RecalculateNormals();
        }
    }
}
