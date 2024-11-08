
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(BiomesGeneration))]
public class BiomesGenerationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Simple Voronoi"))
        {
            BiomesGeneration generationManager = serializedObject.targetObject as BiomesGeneration;
            generationManager.GenerateSimpleVoronoiTexture();
        }

        if (GUILayout.Button("Perlin Noise"))
        {
            BiomesGeneration generationManager = serializedObject.targetObject as BiomesGeneration;
            generationManager.GeneratePerlinNoiseTexture();
        }


        if (GUILayout.Button("Mesh"))
        {
            BiomesGeneration generationManager = serializedObject.targetObject as BiomesGeneration;
            generationManager.UseVoronoiLib();
        }

    }
}
