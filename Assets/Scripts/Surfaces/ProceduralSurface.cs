using System;
using ProceduralMeshes;
using ProceduralMeshes.Generators;
using ProceduralMeshes.Streams;
using UnityEngine;
using UnityEngine.Rendering;
using static Noise;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralSurface : MonoBehaviour
{
    private static AdvancedMeshJobScheduleDelegate[] meshJobs =
    {
        MeshJob<SquareGrid, SingleStream>.ScheduleParallel,
        MeshJob<SharedSquareGrid, SingleStream>.ScheduleParallel,
        MeshJob<SharedTriangleGrid, SingleStream>.ScheduleParallel,
        MeshJob<FlatHexagonGrid, SingleStream>.ScheduleParallel,
        MeshJob<PointyHexagonGrid, SingleStream>.ScheduleParallel,
        MeshJob<CubeSphere, SingleStream>.ScheduleParallel,
        MeshJob<SharedCubeSphere, SingleStream>.ScheduleParallel,
        MeshJob<Icosphere, SingleStream>.ScheduleParallel,
        MeshJob<GeoIcosphere, SingleStream>.ScheduleParallel,
        MeshJob<Octasphere, SingleStream>.ScheduleParallel,
        MeshJob<GeoOctasphere, SingleStream>.ScheduleParallel,
        MeshJob<UVSphere, SingleStream>.ScheduleParallel
    };

    private enum MeshType
    {
        SquareGrid,
        SharedSquareGrid,
        SharedTriangleGrid,
        FlatHexagonGrid,
        PointyHexagonGrid,
        CubeSphere,
        SharedCubeSphere,
        Icosphere,
        GeoIcosphere,
        Octasphere,
        GeoOctasphere,
        UVSphere
    };

    [SerializeField] private MeshType m_meshType;

    private static SurfaceJobScheduleDelegate[,] surfaceJobs =
    {
        {
            SurfaceJob<Lattice1D<LatticeNormal, Perlin>>.ScheduleParallel,
            SurfaceJob<Lattice2D<LatticeNormal, Perlin>>.ScheduleParallel,
            SurfaceJob<Lattice3D<LatticeNormal, Perlin>>.ScheduleParallel
        },
        {
            SurfaceJob<Lattice1D<LatticeNormal, Smoothstep<Turbulence<Perlin>>>>.ScheduleParallel,
            SurfaceJob<Lattice2D<LatticeNormal, Smoothstep<Turbulence<Perlin>>>>.ScheduleParallel,
            SurfaceJob<Lattice3D<LatticeNormal, Smoothstep<Turbulence<Perlin>>>>.ScheduleParallel
        },
        {
            SurfaceJob<Lattice1D<LatticeNormal, Value>>.ScheduleParallel,
            SurfaceJob<Lattice2D<LatticeNormal, Value>>.ScheduleParallel,
            SurfaceJob<Lattice3D<LatticeNormal, Value>>.ScheduleParallel
        },
        {
            SurfaceJob<Simplex1D<Simplex>>.ScheduleParallel,
            SurfaceJob<Simplex2D<Simplex>>.ScheduleParallel,
            SurfaceJob<Simplex3D<Simplex>>.ScheduleParallel
        },
        {
            SurfaceJob<Simplex1D<Smoothstep<Turbulence<Simplex>>>>.ScheduleParallel,
            SurfaceJob<Simplex2D<Smoothstep<Turbulence<Simplex>>>>.ScheduleParallel,
            SurfaceJob<Simplex3D<Smoothstep<Turbulence<Simplex>>>>.ScheduleParallel
        },
        {
            SurfaceJob<Simplex1D<Value>>.ScheduleParallel,
            SurfaceJob<Simplex2D<Value>>.ScheduleParallel,
            SurfaceJob<Simplex3D<Value>>.ScheduleParallel
        },
        {
            SurfaceJob<Voronoi1D<LatticeNormal, Worley, F1>>.ScheduleParallel,
            SurfaceJob<Voronoi2D<LatticeNormal, Worley, F1>>.ScheduleParallel,
            SurfaceJob<Voronoi3D<LatticeNormal, Worley, F1>>.ScheduleParallel
        },
        {
            SurfaceJob<Voronoi1D<LatticeNormal, Worley, F2>>.ScheduleParallel,
            SurfaceJob<Voronoi2D<LatticeNormal, Worley, F2>>.ScheduleParallel,
            SurfaceJob<Voronoi3D<LatticeNormal, Worley, F2>>.ScheduleParallel
        },
        {
            SurfaceJob<Voronoi1D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
            SurfaceJob<Voronoi2D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
            SurfaceJob<Voronoi3D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel
        },
        {
            SurfaceJob<Voronoi1D<LatticeNormal, SmoothWorley, F1>>.ScheduleParallel,
            SurfaceJob<Voronoi2D<LatticeNormal, SmoothWorley, F1>>.ScheduleParallel,
            SurfaceJob<Voronoi3D<LatticeNormal, SmoothWorley, F1>>.ScheduleParallel
        },
        {
            SurfaceJob<Voronoi1D<LatticeNormal, SmoothWorley, F2>>.ScheduleParallel,
            SurfaceJob<Voronoi2D<LatticeNormal, SmoothWorley, F2>>.ScheduleParallel,
            SurfaceJob<Voronoi3D<LatticeNormal, SmoothWorley, F2>>.ScheduleParallel
        },
        {
            SurfaceJob<Voronoi1D<LatticeNormal, Worley, F1>>.ScheduleParallel,
            SurfaceJob<Voronoi2D<LatticeNormal, Chebyshev, F1>>.ScheduleParallel,
            SurfaceJob<Voronoi3D<LatticeNormal, Chebyshev, F1>>.ScheduleParallel
        },
        {
            SurfaceJob<Voronoi1D<LatticeNormal, Worley, F2>>.ScheduleParallel,
            SurfaceJob<Voronoi2D<LatticeNormal, Chebyshev, F2>>.ScheduleParallel,
            SurfaceJob<Voronoi3D<LatticeNormal, Chebyshev, F2>>.ScheduleParallel
        },
        {
            SurfaceJob<Voronoi1D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
            SurfaceJob<Voronoi2D<LatticeNormal, Chebyshev, F2MinusF1>>.ScheduleParallel,
            SurfaceJob<Voronoi3D<LatticeNormal, Chebyshev, F2MinusF1>>.ScheduleParallel
        }
    };

    private static FlowJobScheduleDelegate[,] flowJobs =
    {
        {
            FlowJob<Lattice1D<LatticeNormal, Perlin>>.ScheduleParallel,
            FlowJob<Lattice2D<LatticeNormal, Perlin>>.ScheduleParallel,
            FlowJob<Lattice3D<LatticeNormal, Perlin>>.ScheduleParallel
        },
        {
            FlowJob<Lattice1D<LatticeNormal, Smoothstep<Turbulence<Perlin>>>>.ScheduleParallel,
            FlowJob<Lattice2D<LatticeNormal, Smoothstep<Turbulence<Perlin>>>>.ScheduleParallel,
            FlowJob<Lattice3D<LatticeNormal, Smoothstep<Turbulence<Perlin>>>>.ScheduleParallel
        },
        {
            FlowJob<Lattice1D<LatticeNormal, Value>>.ScheduleParallel,
            FlowJob<Lattice2D<LatticeNormal, Value>>.ScheduleParallel,
            FlowJob<Lattice3D<LatticeNormal, Value>>.ScheduleParallel
        },
        {
            FlowJob<Simplex1D<Simplex>>.ScheduleParallel,
            FlowJob<Simplex2D<Simplex>>.ScheduleParallel,
            FlowJob<Simplex3D<Simplex>>.ScheduleParallel
        },
        {
            FlowJob<Simplex1D<Smoothstep<Turbulence<Simplex>>>>.ScheduleParallel,
            FlowJob<Simplex2D<Smoothstep<Turbulence<Simplex>>>>.ScheduleParallel,
            FlowJob<Simplex3D<Smoothstep<Turbulence<Simplex>>>>.ScheduleParallel
        },
        {
            FlowJob<Simplex1D<Value>>.ScheduleParallel,
            FlowJob<Simplex2D<Value>>.ScheduleParallel,
            FlowJob<Simplex3D<Value>>.ScheduleParallel
        },
        {
            FlowJob<Voronoi1D<LatticeNormal, Worley, F1>>.ScheduleParallel,
            FlowJob<Voronoi2D<LatticeNormal, Worley, F1>>.ScheduleParallel,
            FlowJob<Voronoi3D<LatticeNormal, Worley, F1>>.ScheduleParallel
        },
        {
            FlowJob<Voronoi1D<LatticeNormal, Worley, F2>>.ScheduleParallel,
            FlowJob<Voronoi2D<LatticeNormal, Worley, F2>>.ScheduleParallel,
            FlowJob<Voronoi3D<LatticeNormal, Worley, F2>>.ScheduleParallel
        },
        {
            FlowJob<Voronoi1D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
            FlowJob<Voronoi2D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
            FlowJob<Voronoi3D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel
        },
        {
            FlowJob<Voronoi1D<LatticeNormal, SmoothWorley, F1>>.ScheduleParallel,
            FlowJob<Voronoi2D<LatticeNormal, SmoothWorley, F1>>.ScheduleParallel,
            FlowJob<Voronoi3D<LatticeNormal, SmoothWorley, F1>>.ScheduleParallel
        },
        {
            FlowJob<Voronoi1D<LatticeNormal, SmoothWorley, F2>>.ScheduleParallel,
            FlowJob<Voronoi2D<LatticeNormal, SmoothWorley, F2>>.ScheduleParallel,
            FlowJob<Voronoi3D<LatticeNormal, SmoothWorley, F2>>.ScheduleParallel
        },
        {
            FlowJob<Voronoi1D<LatticeNormal, Worley, F1>>.ScheduleParallel,
            FlowJob<Voronoi2D<LatticeNormal, Chebyshev, F1>>.ScheduleParallel,
            FlowJob<Voronoi3D<LatticeNormal, Chebyshev, F1>>.ScheduleParallel
        },
        {
            FlowJob<Voronoi1D<LatticeNormal, Worley, F2>>.ScheduleParallel,
            FlowJob<Voronoi2D<LatticeNormal, Chebyshev, F2>>.ScheduleParallel,
            FlowJob<Voronoi3D<LatticeNormal, Chebyshev, F2>>.ScheduleParallel
        },
        {
            FlowJob<Voronoi1D<LatticeNormal, Worley, F2MinusF1>>.ScheduleParallel,
            FlowJob<Voronoi2D<LatticeNormal, Chebyshev, F2MinusF1>>.ScheduleParallel,
            FlowJob<Voronoi3D<LatticeNormal, Chebyshev, F2MinusF1>>.ScheduleParallel
        }
    };

    private enum NoiseType
    {
        Perlin,
        PerlinSmoothTurbulence,
        PerlinValue,
        Simplex,
        SimplexSmoothTurbulence,
        SimplexValue,
        VoronoiWorleyF1,
        VoronoiWorleyF2,
        VoronoiWorleyF2MinusF1,
        VoronoiWorleySmoothLSE,
        VoronoiWorleySmoothPoly,
        VoronoiChebyshevF1,
        VoronoiChebyshevF2,
        VoronoiChebyshevF2MinusF1
    }

    [SerializeField] private NoiseType m_noiseType;

    [SerializeField] [Range(1, 3)] private int m_dimensions = 1;

    [SerializeField] private bool m_recalculateNormals;

    [SerializeField] private bool m_recalculateTangents;

    [Flags]
    private enum MeshOptimizationMode
    {
        Nothing = 0,
        ReorderIndices = 1,
        ReorderVertices = 0b10
    }

    [SerializeField] private MeshOptimizationMode m_meshOptimization;

    [SerializeField][Range(1, 50)] private int m_resolution = 1;

    [SerializeField] [Range(-1.0f, 1.0f)] private float m_displacement = 0.5f;

    [SerializeField] private Settings m_noiseSettings = Settings.Default;

    [SerializeField] private SpaceTRS m_domain = new SpaceTRS
    {
        scale = 1.0f
    };

    private enum FlowMode
    {
        Off,
        Curl,
        Downhill
    }

    [SerializeField] private FlowMode m_flowMode;

    [Flags]
    private enum GizmoMode
    {
        Nothing = 0,
        Vertices = 1,
        Normals = 0b10,
        Tangents = 0b100,
        Triangles = 0b1000
    }

    [SerializeField] private GizmoMode m_gizmos;

    private enum MaterialMode
    {
        Displacement,
        Flat,
        LatLonMap,
        CubeMap
    }

    [SerializeField] private MaterialMode m_material;

    [SerializeField] private Material[] m_materials;

    private Mesh m_mesh;

    [NonSerialized] private Vector3[] m_vertices;
    [NonSerialized] private Vector3[] m_normals;

    [NonSerialized] private Vector4[] m_tangents;

    [NonSerialized] private int[] m_triangles;

    private static int materialIsPlaneId = Shader.PropertyToID("_IsPlane");

    private ParticleSystem m_flowSystem;

    private bool IsPlane => m_meshType < MeshType.CubeSphere;

    private void Awake()
    {
        m_mesh = new Mesh
        {
            name = "Procedural Mesh"
        };

        GetComponent<MeshFilter>().mesh = m_mesh;

        m_materials[(int)m_displacement] = new Material(m_materials[(int)m_displacement]);

        m_flowSystem = GetComponent<ParticleSystem>();
    }

    private void OnDrawGizmos()
    {
        if (m_gizmos == GizmoMode.Nothing
            || m_mesh == null)
        {
            return;
        }

        bool drawVertices = (m_gizmos & GizmoMode.Vertices) != 0;
        bool drawNormals = (m_gizmos & GizmoMode.Normals) != 0;
        bool drawTangents = (m_gizmos & GizmoMode.Tangents) != 0;
        bool drawTriangles = (m_gizmos & GizmoMode.Triangles) != 0;

        if (m_vertices == null)
        {
            m_vertices = m_mesh.vertices;
        }

        if (drawNormals
            && m_normals == null)
        {
            drawNormals = m_mesh.HasVertexAttribute(VertexAttribute.Normal);

            if (drawNormals)
            {
                m_normals = m_mesh.normals;
            }
        }

        if (drawTangents
            && m_tangents == null)
        {
            drawTangents = m_mesh.HasVertexAttribute(VertexAttribute.Tangent);

            if (drawTangents)
            {
                m_tangents = m_mesh.tangents;
            }
        }

        if (drawTriangles
            && m_triangles == null)
        {
            m_triangles = m_mesh.triangles;
        }

        Transform t = transform;

        for (int i = 0; i < m_vertices.Length; i++)
        {
            Vector3 position = t.TransformPoint(m_vertices[i]);

            if (drawVertices)
            {
                Gizmos.color = Color.cyan;

                Gizmos.DrawSphere(position, 0.02f);
            }

            if (drawNormals)
            {
                Gizmos.color = Color.green;

                Gizmos.DrawRay(position, t.TransformDirection(m_normals[i]) * 0.2f);
            }

            if (drawTangents)
            {
                Gizmos.color = Color.red;

                Gizmos.DrawRay(position, t.TransformDirection(m_tangents[i]) * 0.2f);
            }
        }

        if (drawTriangles)
        {
            float colorStep = 1f / (m_triangles.Length - 3);

            for (int i = 0; i < m_triangles.Length; i += 3)
            {
                float c = i * colorStep;

                Gizmos.color = new Color(c, 0f, c);

                Gizmos.DrawSphere(t.TransformPoint((
                        m_vertices[m_triangles[i]] +
                        m_vertices[m_triangles[i + 1]] +
                        m_vertices[m_triangles[i + 2]]) * (1f / 3f)),
                    0.02f
                );
            }
        }
    }

    private void OnValidate() => enabled = true;

    private void Update()
    {
        GenerateMesh();
        
        enabled = false;

        m_vertices = null;
        m_normals = null;
        m_tangents = null;
        m_triangles = null;

        if (m_material == MaterialMode.Displacement)
        {
            m_materials[(int)MaterialMode.Displacement].SetFloat(materialIsPlaneId, IsPlane ? 1.0f : 0.0f);
        }

        GetComponent<MeshRenderer>().material = m_materials[(int)m_material];

        if (m_flowMode == FlowMode.Off)
        {
            m_flowSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        else
        {
            m_flowSystem.Play();
            
            ParticleSystem.ShapeModule shapeModule = m_flowSystem.shape;
            
            shapeModule.shapeType = IsPlane ? ParticleSystemShapeType.Rectangle : ParticleSystemShapeType.Sphere;
        }
    }

    private void GenerateMesh()
    {
        Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
        Mesh.MeshData meshData = meshDataArray[0];
        
        surfaceJobs[(int)m_noiseType, m_dimensions - 1](meshData, m_resolution,
            m_noiseSettings, m_domain, m_displacement, IsPlane,
            meshJobs[(int)m_meshType](m_mesh, meshData, m_resolution,
            default, Vector3.one * Mathf.Abs(m_displacement), true)).Complete();

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, m_mesh);

        if (m_recalculateNormals)
        {
            m_mesh.RecalculateNormals();
        }

        if (m_recalculateTangents)
        {
            m_mesh.RecalculateTangents();
        }

        if (m_meshOptimization == MeshOptimizationMode.ReorderIndices)
        {
            m_mesh.OptimizeIndexBuffers();
        }
        else if (m_meshOptimization == MeshOptimizationMode.ReorderVertices)
        {
            m_mesh.OptimizeReorderVertexBuffer();
        }
        else if (m_meshOptimization != MeshOptimizationMode.Nothing)
        {
            m_mesh.Optimize();
        }
    }

    private void OnParticleUpdateJobScheduled()
    {
        if (m_flowMode != FlowMode.Off)
        {
            flowJobs[(int)m_noiseType, m_dimensions - 1](m_flowSystem, m_noiseSettings, m_domain,
                m_displacement, IsPlane, m_flowMode == FlowMode.Curl);
        }
    }
}