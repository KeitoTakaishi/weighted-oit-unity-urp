using UnityEngine;

[ExecuteAlways]
public class Gizmo : MonoBehaviour
{
    public Color color = Color.green;
    public Vector3 size = Vector3.one;

    private void OnRenderObject()
    {
        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);

        Material mat = new Material(Shader.Find("Hidden/Internal-Colored"));
        mat.SetPass(0);

        GL.Begin(GL.LINES);
        GL.Color(color);

        Vector3 half = size * 0.5f;

        Vector3[] v = new Vector3[8]
        {
            new Vector3(-half.x,-half.y,-half.z),
            new Vector3( half.x,-half.y,-half.z),
            new Vector3( half.x, half.y,-half.z),
            new Vector3(-half.x, half.y,-half.z),
            new Vector3(-half.x,-half.y, half.z),
            new Vector3( half.x,-half.y, half.z),
            new Vector3( half.x, half.y, half.z),
            new Vector3(-half.x, half.y, half.z)
        };

        int[,] edges = new int[,]
        {
            {0,1},{1,2},{2,3},{3,0},
            {4,5},{5,6},{6,7},{7,4},
            {0,4},{1,5},{2,6},{3,7}
        };

        for (int i = 0; i < edges.GetLength(0); i++)
        {
            GL.Vertex(v[edges[i, 0]]);
            GL.Vertex(v[edges[i, 1]]);
        }

        GL.End();
        GL.PopMatrix();
    }
}
