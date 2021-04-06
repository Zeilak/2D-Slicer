using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debug1 : MonoBehaviour
{

    public Transform[] _tripoints;
    public Transform[] _polyLine;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Slice();
    }

    void Slice()
    {
        Vector2[] v = new Vector2[_tripoints.Length];
        ushort[] t = new ushort[]{ 0, 1, 2, 2, 3, 1, 6, 2, 3, 6, 7, 3, 7, 5, 3, 4, 3, 1, 5, 3, 4};
        Vector2[] l = new Vector2[_polyLine.Length];
        for (int i = 0; i < _tripoints.Length; i++)
        {
            v[i] = _tripoints[i].position;
        }
        for (int i = 0; i < _polyLine.Length; i++)
        {
            l[i] = _polyLine[i].position;
        }
        List<Vector2[]> svl = new List<Vector2[]>();
        List<ushort[]> trl = new List<ushort[]>(); Vector2[] polyline;

        Slicer2DMeshMeta slicer = new Slicer2DMeshMeta(new List<Vector2>(v), new List<ushort>(t), new List<Vector2>(l));
        slicer.Slice(out svl, out trl);

        int a, b, c;
        for (int i = 0; i < svl.Count; i++)
        {
            Color color = new Color(0, 0, 0);

            int i1 = i % 7;
            switch(i1)
            {
                case 0:
                    color = Color.green;
                    break;
                case 1:
                    color = Color.yellow;
                    break;
                case 2:
                    color = Color.black;
                    break;
                case 3:
                    color = Color.white;
                    break;
                case 4:
                    color = Color.red;
                    break;
                case 5:
                    color = Color.magenta;
                    break;
                case 6:
                    color = Color.grey;
                    break;
            }
            for (int j = 0; j < trl[i].Length; j = j + 3)
            {
                a = trl[i][j];
                b = trl[i][j + 1];
                c = trl[i][j + 2];
                Debug.DrawLine(svl[i][a], svl[i][b], color, 0f);
                Debug.DrawLine(svl[i][b], svl[i][c], color, 0f);
                Debug.DrawLine(svl[i][c], svl[i][a], color, 0f);
            }
        }

        for (int i = 1; i < l.Length; i++)
        {
            Debug.DrawLine(l[i - 1], l[i], Color.blue, 0f);
        }

    }
}
