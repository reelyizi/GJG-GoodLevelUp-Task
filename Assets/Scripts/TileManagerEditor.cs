using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileManager))]
public class TileManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TileManager tileManager = (TileManager)target;

        if (GUILayout.Button("Generate Grid"))
        {
            tileManager.InitializeTileLookup();
            tileManager.GenerateGrid();
            tileManager.UpdateTileGroups();
        }
    }
}
