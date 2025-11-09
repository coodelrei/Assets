#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Unit))]
public class UnitEditor : Editor
{
    const int CellPx = 24;

    public override void OnInspectorGUI()
    {
        var u = (Unit)target;
        serializedObject.Update();

        // Panel ayarlarý
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useGridAuthoring"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("authorCols"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("authorRows"));
        EditorGUILayout.Space(6);

        if (GetBool(serializedObject, "useGridAuthoring"))
        {
            DrawGrid(u);
            if (GUILayout.Button("Bake ShapeOffsets From Grid"))
            {
                Undo.RecordObject(u, "Bake Shape");
                u.BakeShapeOffsetsFromAuthorGrid();
                EditorUtility.SetDirty(u);
            }
            EditorGUILayout.Space(6);
        }

        // Diðer alanlar (ShapeOffsets ve authorGrid'i gizle)
        DrawPropertiesExcluding(serializedObject,
            "m_Script",
            "useGridAuthoring", "authorCols", "authorRows",
            "authorGrid",
            "ShapeOffsets");

        serializedObject.ApplyModifiedProperties();
    }

    void DrawGrid(Unit u)
    {
        var grid = u.GetAuthorGrid();
        int cols = Mathf.Max(1, u.authorCols);
        int rows = Mathf.Max(1, u.authorRows);

        var bgStyle = new GUIStyle("Button");

        // Inspector yukarýdan aþaðý çiziliyor; görselde üst satýr y = rows-1
        for (int y = rows - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < cols; x++)
            {
                int idx = y * cols + x;
                bool val = (idx < grid.Count) ? grid[idx] : false;

                var rect = GUILayoutUtility.GetRect(CellPx, CellPx);
                if (GUI.Button(rect, GUIContent.none, bgStyle))
                {
                    Undo.RecordObject(u, "Toggle Cell");
                    u.SetAuthorCell(x, y, !val);
                    EditorUtility.SetDirty(u);
                }

                // Dolu hücreyi renklendir
                if (val) EditorGUI.DrawRect(rect, new Color(0.9f, 0.9f, 0.2f, 0.9f));

                // Kenarlýklar (Handles yerine 1px rect)
                var k = new Color(0f, 0f, 0f, 0.8f);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), k);
                EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), k);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), k);
                EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), k);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.HelpBox(
            "Kutulara týkla. Bake ile ShapeOffsets üretilir. Sol-alt köþe normalize edilir (0,0).",
            MessageType.Info);
    }

    static bool GetBool(SerializedObject so, string propName)
        => so.FindProperty(propName)?.boolValue ?? false;
}
#endif
