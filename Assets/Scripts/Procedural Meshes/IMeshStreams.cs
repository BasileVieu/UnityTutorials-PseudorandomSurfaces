using Unity.Mathematics;
using UnityEngine;

namespace ProceduralMeshes
{
    public interface IMeshStreams
    {
        void Setup(Mesh.MeshData _meshData, Bounds _bounds, int _vertexCount, int _indexCount);

        void SetVertex(int _index, Vertex _vertex);

        void SetTriangle(int _index, int3 _triangle);
    }
}