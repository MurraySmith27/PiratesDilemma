using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.UIElements;
using Vertex = UnityEngine.UIElements.Vertex;

public class CircleElement : VisualElement
{
    
    public new class UxmlFactory : UxmlFactory<CircleElement, UxmlTraits> { }
    
    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlFloatAttributeDescription m_currentOuterRadius =
            new UxmlFloatAttributeDescription { name = "current-inner-radius", defaultValue = 1f };
        UxmlFloatAttributeDescription m_width =
            new UxmlFloatAttributeDescription { name = "width", defaultValue = 2f };
        UxmlColorAttributeDescription m_tint =
            new UxmlColorAttributeDescription { name = "circle-tint", defaultValue = Color.white };
        UxmlIntAttributeDescription m_resolution =
            new UxmlIntAttributeDescription { name = "resolution", defaultValue = 12 };

        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);
            var ate = ve as CircleElement;

            ate.m_currentOuterRadius = m_currentOuterRadius.GetValueFromBag(bag, cc);
            ate.m_width = m_width.GetValueFromBag(bag, cc);
            ate.m_tint = m_tint.GetValueFromBag(bag, cc);
            ate.m_resolution = m_resolution.GetValueFromBag(bag, cc);
        }
    }
    
    
    public float m_currentOuterRadius = 1f;
    
    public float m_width = 1f;

    public Color m_tint = Color.white;

    public int m_resolution = 24;
 
    private List<Vertex> k_Vertices = new List<Vertex>();

    private List<ushort> k_faceIndices = new List<ushort>();

    private Texture2D m_texture;

    public CircleElement()
    {
        generateVisualContent += OnGenerateVisualContent;
        m_texture = Resources.Load<Texture2D>("circle_tex");
    }

    private void UpdateMeshData(MeshGenerationContext mgc)
    {
        
        MeshWriteData mwd = mgc.Allocate(2 * m_resolution, 6 * m_resolution, m_texture);
        
        Rect uvRegion = mwd.uvRegion;
        
        float outerRadius = m_currentOuterRadius;

        k_Vertices.Clear();
        k_faceIndices.Clear();

        float angleIncrement = 360f / m_resolution;
        
        for (ushort i = 0; i < m_resolution; i++)
        {
            float angle = i * (angleIncrement * Mathf.PI / 180f);
            
            Vertex newOuterVertex = new Vertex();
            newOuterVertex.position = new Vector3( outerRadius * Mathf.Sin(angle), outerRadius * Mathf.Cos(angle), Vertex.nearZ);
            
            newOuterVertex.uv = 0.4f * new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * uvRegion.size + uvRegion.min;

            newOuterVertex.tint = m_tint;

            k_Vertices.Add(newOuterVertex);
            
            k_faceIndices.Add(i);
            k_faceIndices.Add((ushort)(m_resolution+i));

            if (i == m_resolution-1)
            {
                k_faceIndices.Add(0);   
            }
            else
            {
                k_faceIndices.Add((ushort)(i+1));
            }
            
            k_faceIndices.Add((ushort)(m_resolution + i));

            if (i == m_resolution-1)
            {
                k_faceIndices.Add((ushort)m_resolution);
                k_faceIndices.Add(0);
            }
            else
            {
                k_faceIndices.Add((ushort)(m_resolution+i+1));
                k_faceIndices.Add((ushort)(i+1));
            }
        } 
        
        //now add all inner vertices

        float innerRadius = Mathf.Max(outerRadius - m_width, 0f);
        for (int i = 0; i < m_resolution; i++)
        {
            float angle = i * (angleIncrement * Mathf.PI / 180f);
            
            Vertex newInnerVertex = new Vertex();
            newInnerVertex.position = new Vector3( innerRadius * Mathf.Sin(angle), innerRadius * Mathf.Cos(angle), Vertex.nearZ);
            
            newInnerVertex.tint = m_tint;
            
            k_Vertices.Add(newInnerVertex);
        }
        
        mwd.SetAllVertices(k_Vertices.ToArray());
        
        mwd.SetAllIndices(k_faceIndices.ToArray());
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        UpdateMeshData(mgc);
    }
}
