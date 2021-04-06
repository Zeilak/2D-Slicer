using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Slicer2DMeshMeta
{
	static float EPS = Mathf.Epsilon;

	private List<Vector2> _inputPolyline;
	private List<Vector2> _newPolyline;
	private List<ushort> _triangles;
	private List<Vector2> _vertices;

	private List<Vector2> _newLineFragment;
	private List<ushort>[] _verData;
	private TriTree[] _triData;

	private int[] _polyLineIndex;
	private int[] _notPolyLineIndex;

	private List<ushort> verList;
	private List<ushort> triout;
	private List<Vector2> verout;

	public Slicer2DMeshMeta(List<Vector2> vert, List<ushort> tri, List<Vector2> slicePolyline)
	{
		_inputPolyline = new List<Vector2>(slicePolyline);
		_triangles = new List<ushort>(tri);
		_vertices = new List<Vector2>(vert);
	}

	public void Slice(out List<Vector2[]> vertices, out List<ushort[]> triangles)
	{
		// cut triangles along the line
		CreatingSeparationBoundaries();

		// divide the triangles into groups according to the line
		SplittingTrianglesIntoGroups();

		// converting the groups to the desired format
		TrianglesToGroups(out vertices, out triangles);
	}

	public void TrianglesToGroups(out List<Vector2[]> vertices, out List<ushort[]> triangles)
	{
		vertices = new List<Vector2[]>();
		triangles = new List<ushort[]>();
		for (int i = 0; i < _triangles.Count / 3; i++)
		{
			if (_triData[i].prev == null)
            {
				MakeTrianglesGroup(i);
				vertices.Add(verout.ToArray());
				triangles.Add(triout.ToArray());
            }
		}

	}

	private void MakeTrianglesGroup(int index)
	{
		verList = new List<ushort>();

		triout = new List<ushort>();
		verout = new List<Vector2>();

		MakeTrianglesGroupRec(_triData[index]);
	}

	private void MakeTrianglesGroupRec(TriTree tri)
    {
		for (int i = 0; i < 3; i++)
        {
			int ind = verList.IndexOf(_triangles[tri.index * 3 + i]);
			if (ind == -1)
			{
				verout.Add(_vertices[_triangles[tri.index * 3 + i]]);
				verList.Add(_triangles[tri.index * 3 + i]);
				triout.Add((ushort)(verout.Count - 1));
			}
			else
            {
				triout.Add((ushort)ind);
            }
        }
		for(int i = 0; i < tri.tris.Count; i++)
        {
			MakeTrianglesGroupRec(tri.tris[i]);
        }
    }
	private void SplittingTrianglesIntoGroups()
	{
		int k = 0, kk = 0;

		int verCount = _vertices.Count;
		int triCount = _triangles.Count;
		_verData = new List<ushort>[verCount];
		for(int i = 0; i < verCount; i++)
        {
			_verData[i] = new List<ushort>();
        }
		for(int i = 0; i < triCount; i++)
        {
			_verData[_triangles[i]].Add((ushort)(i / 3));
        }
		_triData = new TriTree[triCount / 3];
		for(int i = 0; i < triCount / 3; i++)
        {
			_triData[i] = new TriTree(i);
        }


		for(int i = 0; i < _notPolyLineIndex.Length; i++)
        {
			for (k = 0; k < _verData[_notPolyLineIndex[i]].Count; k++)
			{
				for (kk = k + 1; kk < _verData[_notPolyLineIndex[i]].Count; kk++)
				{
					_triData[_verData[_notPolyLineIndex[i]][k]].AddList(_triData[_verData[_notPolyLineIndex[i]][kk]]);
				}
			}
		}
		
		for(int i = 0; i < _polyLineIndex.Length; i++)
        {
			if (_polyLineIndex[i] == -1)
				continue;
			for (k = 0; k < _verData[_polyLineIndex[i]].Count; k++)
			{
				for (kk = k + 1; kk < _verData[_polyLineIndex[i]].Count; kk++)
				{
					if (CheckTriConnectionForLinePoints(_verData[_polyLineIndex[i]][k], _verData[_polyLineIndex[i]][kk]))
					{
						_triData[_verData[_polyLineIndex[i]][k]].AddList(_triData[_verData[_polyLineIndex[i]][kk]]);
					}

				}
			}
		}
	}

	private bool CheckTriConnectionForLinePoints(int i1, int i2)
    {
		List<ushort> points = new List<ushort>();
		for(int i = 0; i < 3; i++)
        {
			for(int j = 0; j < 3; j++)
            {
				if(_triangles[i1 * 3 + i] == _triangles[i2 * 3 + j])
                {
					points.Add(_triangles[i1 * 3 + i]);
                }
            }
        }
		if (points.Count > 1)
		{
			if (!ItsCuttingLine(points[0], points[1]))
			{
				return true;
			}
		}
		else
			return false;
		return false;
    }

	private bool ItsCuttingLine(int p1, int p2)
    {
		for(int i = 0; i < _polyLineIndex.Length; i++)
        {
			if(_polyLineIndex[i] == p1)
            {
				if (i < _polyLineIndex.Length - 1 && _polyLineIndex[i + 1] == p2)
				{
					return true;
				}
				else if(i > 0 && _polyLineIndex[i - 1] == p2)
                {
					return true;
                }
            }
        }
		return false;
    }
	private void CreatingSeparationBoundaries()
    {
		int iLine, iTri, triC;
		int t1, t2, t3;
		int pointsFound;
		Vector2 p1;
		Vector2[] newPoints = { Vector2.zero, Vector2.zero };
		int[] newPointsInTriLineNumber = { 0, 0 };
		_newPolyline = new List<Vector2>();

		for (iLine = 0; iLine < _inputPolyline.Count; iLine++)
		{
			for (iTri = 0; iTri < _triangles.Count; iTri += 3)
			{
				if (PointInTrinagle(iTri, _inputPolyline[iLine]))
				{
					SplitTriangles2(_inputPolyline[iLine], iTri);
				}
			}
		}

		for (iLine = 0; iLine < _inputPolyline.Count - 1; iLine++)
		{
			_newLineFragment = new List<Vector2>();
			_newLineFragment.Add(_inputPolyline[iLine]);
			triC = _triangles.Count;
			for (iTri = 0; iTri < triC; iTri += 3)
			{
				newPointsInTriLineNumber[0] = 0; newPointsInTriLineNumber[1] = 0;
				p1 = Vector2.zero;
				pointsFound = 0;
				t1 = _triangles[iTri];
				t2 = _triangles[iTri + 1];
				t3 = _triangles[iTri + 2];
				if (Intersect(_vertices[t1], _vertices[t2], _inputPolyline[iLine], _inputPolyline[iLine + 1], out p1))
				{
					if (!(p1 == _vertices[t1] || p1 == _vertices[t2]))
					{
						pointsFound = AddPointsNewLineFragment(p1, pointsFound, ref newPoints, t1, t2);
						if (pointsFound == 1)
							newPointsInTriLineNumber[0] = 1;
						else
						{
							newPointsInTriLineNumber[0] = 1;
							newPointsInTriLineNumber[1] = 1;
						}
						
					}
				}
				if (Intersect(_vertices[t2], _vertices[t3], _inputPolyline[iLine], _inputPolyline[iLine + 1], out p1))
				{
					if (!(p1 == _vertices[t2] || p1 == _vertices[t3]))
					{
					pointsFound = AddPointsNewLineFragment(p1, pointsFound, ref newPoints, t2, t3);
						if (pointsFound == 1)
							newPointsInTriLineNumber[0] = 2;
						else
						{
							if (newPointsInTriLineNumber[0] == 1)
								newPointsInTriLineNumber[1] = 2;
							else
							{
								newPointsInTriLineNumber[0] = 2;
								newPointsInTriLineNumber[1] = 2;
							}

						}
						
					}

				}
				if (Intersect(_vertices[t1], _vertices[t3], _inputPolyline[iLine], _inputPolyline[iLine + 1], out p1))
				{
					if (!(p1 == _vertices[t1] || p1 == _vertices[t3]))
					{
					pointsFound = AddPointsNewLineFragment(p1, pointsFound, ref newPoints, t1, t3);
						if (pointsFound == 1)
							newPointsInTriLineNumber[0] = 3;
						else
						{
							if (newPointsInTriLineNumber[0] == 1 || newPointsInTriLineNumber[0] == 2)
								newPointsInTriLineNumber[1] = 3;
							else
							{
								newPointsInTriLineNumber[0] = 3;
								newPointsInTriLineNumber[1] = 3;
							}

						}
						
					}
				}

				SplitTriangles1(pointsFound, newPoints, iTri, newPointsInTriLineNumber);

			}
			_newLineFragment = _newLineFragment.Distinct(new Vector2Comparer()).ToList<Vector2>();
			
			for (int i = 0; i < _newLineFragment.Count; i++)
				{
					_newPolyline.Add(_newLineFragment[i]);
				}
		}
		_newPolyline.Add(_inputPolyline[_inputPolyline.Count - 1]);

		_polyLineIndex = new int[_newPolyline.Count];
		for(int i = 0; i < _polyLineIndex.Length; i++)
        {
			_polyLineIndex[i] = -1;
        }
		List<int> nplil = new List<int>();
		bool setPolyLineIndex;
		for(int i = 0; i < _vertices.Count; i++)
        {
			setPolyLineIndex = false;
			for(int j = 0; j < _newPolyline.Count; j++)
            {
				if(_vertices[i] == _newPolyline[j])
                {
					_polyLineIndex[j] = i;
					setPolyLineIndex = true;
                }
            }
			if(!setPolyLineIndex)
            {
				nplil.Add(i);
			}
        }
		_notPolyLineIndex = nplil.ToArray();
	}

	private int AddPointsNewLineFragment(Vector2 p1, int pointFound, ref Vector2[] newPoints, int ver1, int ver2)
	{
		int i = 1;
		for (i = 1; i < _newLineFragment.Count; i++)
		{
			if (Vector2Dist(p1, _newLineFragment[0]) < Vector2Dist(_newLineFragment[i], _newLineFragment[0]))
			{
				_newLineFragment.Insert(i, p1);
				break;
			}
		}
		if (i == _newLineFragment.Count)
		{
			_newLineFragment.Add(p1);
		}
		int plc = _newPolyline.Count;
		if (plc > 1)
		{
			for (i = 0; i < plc; i++)
			{
				if (_newPolyline[i] == _vertices[ver1] && !(p1 == _newPolyline[i]))
				{
					if (i < plc - 1 && _newPolyline[i + 1] == _vertices[ver2] && !(p1 == _newPolyline[i + 1]))
					{
						_newPolyline.Insert(i + 1, p1);

						break;
					}
					else if (i > 0 && _newPolyline[i - 1] == _vertices[ver2] && !(p1 == _newPolyline[i - 1]))
					{
						_newPolyline.Insert(i, p1);
						break;
					}

				}
			}
		}

		if (pointFound == 0)
			newPoints[0] = p1;
		else
			newPoints[1] = p1;
		return ++pointFound;
	}

	private void SplitTriangles1(int pointsFound, Vector2[] newPs, int iTri, int[] pOnLineNumber)
    {
		if (pointsFound == 0)
			return;
		if(pointsFound == 2)
        {
			for (int i = 0; i < 3; i++)
			{
				if (_vertices[_triangles[iTri + i]] == newPs[0])
				{
					newPs[0] = newPs[1];
					pointsFound = 1;
				}
				if (_vertices[_triangles[iTri + i]] == newPs[1])
				{
					pointsFound--;
					if (pointsFound == 0)
					{
						return;
					}
				}
			}
		}
		if (pointsFound == 1)
        {
			int newVert;
			for (int i = 0; i < 3; i++)
			{
				if (_vertices[_triangles[iTri + i]] == newPs[0])
					return;
			}
			for (newVert = 0; newVert < _vertices.Count && !(newPs[0] == _vertices[newVert]); newVert++) ;
			if(newVert == _vertices.Count)
            {
				_vertices.Add(newPs[0]);
            }
			int iTriNew = _triangles.Count;
			_triangles.Add(0);
			_triangles.Add(0);
			_triangles.Add(0);
			if (pOnLineNumber[0] == 1)
            {
				_triangles[iTriNew] = _triangles[iTri + 1];
				_triangles[iTriNew + 1] = (ushort)newVert;
				_triangles[iTriNew + 2] = _triangles[iTri + 2];

				_triangles[iTri + 1] = (ushort)newVert;

			}
			else if(pOnLineNumber[0] == 2)
            {
				_triangles[iTriNew] = _triangles[iTri];
				_triangles[iTriNew + 1] = (ushort)newVert;
				_triangles[iTriNew + 2] = _triangles[iTri + 1];

				_triangles[iTri + 1] = (ushort)newVert;
			}
			else if (pOnLineNumber[0] == 3)
            {
				_triangles[iTriNew] = _triangles[iTri + 1];
				_triangles[iTriNew + 1] = (ushort)newVert;
				_triangles[iTriNew + 2] = _triangles[iTri];

				_triangles[iTri] = (ushort)newVert;
			}
		}
		else if(pointsFound == 2)
        {
			int newVert1, newVert2;
			for (newVert1 = 0; newVert1 < _vertices.Count && !(newPs[0] == _vertices[newVert1]); newVert1++) ;
			if (newVert1 == _vertices.Count)
			{
				_vertices.Add(newPs[0]);
			}
			for (newVert2 = 0; newVert2 < _vertices.Count && !(newPs[1] == _vertices[newVert2]); newVert2++) ;
			if (newVert2 == _vertices.Count)
			{
				_vertices.Add(newPs[1]);
			}
			int iTriNew = _triangles.Count;
			_triangles.Add(0);
			_triangles.Add(0);
			_triangles.Add(0);
			_triangles.Add(0);
			_triangles.Add(0);
			_triangles.Add(0);
			if (pOnLineNumber[0] == pOnLineNumber[1])
            {
				if (pOnLineNumber[0] == 1)
                {
					if(Vector2Dist(newPs[1], _vertices[(int)_triangles[iTri]]) < Vector2Dist(newPs[0], _vertices[(int)_triangles[iTri]]))
                    {
						int buff = newVert1;
						newVert1 = newVert2;
						newVert2 = buff;
                    }

					_triangles[iTriNew] = _triangles[iTri];
					_triangles[iTriNew + 1] = (ushort)newVert1;
					_triangles[iTriNew + 2] = _triangles[iTri + 2];

					_triangles[iTriNew + 3] = (ushort)newVert1;
					_triangles[iTriNew + 4] = (ushort)newVert2;
					_triangles[iTriNew + 5] = _triangles[iTri + 2];

					_triangles[iTri] = (ushort)newVert2;
				}
				else if (pOnLineNumber[0] == 2)
				{
					if (Vector2Dist(newPs[1], _vertices[(int)_triangles[iTri + 1]]) > Vector2Dist(newPs[0], _vertices[(int)_triangles[iTri + 1]]))
					{
						int buff = newVert1;
						newVert1 = newVert2;
						newVert2 = buff;
					}

					_triangles[iTriNew] = _triangles[iTri];
					_triangles[iTriNew + 1] = (ushort)newVert1;
					_triangles[iTriNew + 2] = _triangles[iTri + 2];

					_triangles[iTriNew + 3] = _triangles[iTri];
					_triangles[iTriNew + 4] = (ushort)newVert1;
					_triangles[iTriNew + 5] = (ushort)newVert2;

					_triangles[iTri + 2] = (ushort)newVert2;
				}
				else if (pOnLineNumber[0] == 3)
				{
					if (Vector2Dist(newPs[1], _vertices[(int)_triangles[iTri + 2]]) < Vector2Dist(newPs[0], _vertices[(int)_triangles[iTri + 2]]))
					{
						int buff = newVert1;
						newVert1 = newVert2;
						newVert2 = buff;
					}

					_triangles[iTriNew] = _triangles[iTri];
					_triangles[iTriNew + 1] = (ushort)newVert2;
					_triangles[iTriNew + 2] = _triangles[iTri + 1];

					_triangles[iTriNew + 3] = _triangles[iTri + 1];
					_triangles[iTriNew + 4] = (ushort)newVert1;
					_triangles[iTriNew + 5] = (ushort)newVert2;

					_triangles[iTri] = (ushort)newVert1;
				}
			}
			else
            {
				if((pOnLineNumber[0] == 3) && (pOnLineNumber[1] == 1) || (pOnLineNumber[0] == 3) && (pOnLineNumber[1] == 2) || (pOnLineNumber[0] == 2) && (pOnLineNumber[1] == 1))
                {
					int buff = newVert1;
					newVert1 = newVert2;
					newVert2 = buff;
					buff = pOnLineNumber[0];
					pOnLineNumber[0] = pOnLineNumber[1];
					pOnLineNumber[1] = buff;
				}
				if((pOnLineNumber[1] == 3) && (pOnLineNumber[0] == 1))
                {
					_triangles[iTriNew] = _triangles[iTri];
					_triangles[iTriNew + 1] = (ushort)newVert2;
					_triangles[iTriNew + 2] = (ushort)newVert1;

					_triangles[iTriNew + 3] = _triangles[iTri + 2];
					_triangles[iTriNew + 4] = (ushort)newVert1;
					_triangles[iTriNew + 5] = (ushort)newVert2;

					_triangles[iTri] = (ushort)newVert1;
				}
				else if((pOnLineNumber[1] == 3) && (pOnLineNumber[0] == 2))
                {
					_triangles[iTriNew] = _triangles[iTri];
					_triangles[iTriNew + 1] = (ushort)newVert2;
					_triangles[iTriNew + 2] = (ushort)newVert1;

					_triangles[iTriNew + 3] = _triangles[iTri + 2];
					_triangles[iTriNew + 4] = (ushort)newVert1;
					_triangles[iTriNew + 5] = (ushort)newVert2;

					_triangles[iTri + 2] = (ushort)newVert1;
				}
				else if((pOnLineNumber[1] == 2) && (pOnLineNumber[0] == 1))
                {
					_triangles[iTriNew] = _triangles[iTri];
					_triangles[iTriNew + 1] = (ushort)newVert2;
					_triangles[iTriNew + 2] = (ushort)newVert1;

					_triangles[iTriNew + 3] = _triangles[iTri + 1];
					_triangles[iTriNew + 4] = (ushort)newVert1;
					_triangles[iTriNew + 5] = (ushort)newVert2;

					_triangles[iTri + 1] = (ushort)newVert2;
				}
            }
			
		}
	}

	private void SplitTriangles2(Vector2 p, int iTri)
    {
		int iTriNew = _triangles.Count;
		_triangles.Add(0);
		_triangles.Add(0);
		_triangles.Add(0);
		_triangles.Add(0);
		_triangles.Add(0);
		_triangles.Add(0);
		int newVert = _vertices.Count;
		_vertices.Add(p);

		_triangles[iTriNew] = _triangles[iTri];
		_triangles[iTriNew + 1] = (ushort)newVert;
		_triangles[iTriNew + 2] = _triangles[iTri + 2];

		_triangles[iTriNew + 3] = _triangles[iTri];
		_triangles[iTriNew + 4] = (ushort)newVert;
		_triangles[iTriNew + 5] = _triangles[iTri + 1];

		_triangles[iTri] = (ushort)newVert;
	}

	private bool PointInTrinagle(int iTri, Vector2 point)
    {
		return PointInTrinagle(_vertices[(int)_triangles[iTri]], _vertices[(int)_triangles[iTri + 1]], _vertices[(int)_triangles[iTri + 2]], point);
    }
	public static bool PointInTrinagle(Vector2 t1, Vector2 t2, Vector2 t3, Vector2 p)
	{
		if ((t1.x - p.x) * (t2.y - t1.y) - (t2.x - t1.x) * (t1.y - p.y) > 0)
		{
			if (((t2.x - p.x) * (t3.y - t2.y) - (t3.x - t2.x) * (t2.y - p.y)) > 0 && ((t3.x - p.x) * (t1.y - t3.y) - (t1.x - t3.x) * (t3.y - p.y)) > 0)
				return true;
			else
				return false;
		}
		else
		{
			if (((t2.x - p.x) * (t3.y - t2.y) - (t3.x - t2.x) * (t2.y - p.y)) < 0 && ((t3.x - p.x) * (t1.y - t3.y) - (t1.x - t3.x) * (t3.y - p.y)) < 0)
				return true;
			else
				return false;
		}
	}

	private static bool Vector2Less(Vector2 p1, Vector2 p2)
	{
		return p1.x < p2.x - EPS || Mathf.Abs(p1.x - p2.x) < EPS && p1.y < p2.y - EPS;
	}
	static float Vector2Dist(Vector2 p1, Vector2 p2)
    {
		return Mathf.Sqrt(Mathf.Pow((p2.x - p1.x), 2) + Mathf.Pow((p2.y - p1.y), 2));
	}

	private static bool Intersect(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2, out Vector2 out_intersection)
	{
		Vector2 dir1 = end1 - start1;
		Vector2 dir2 = end2 - start2;

		//считаем уравнения прямых проходящих через отрезки
		float a1 = -dir1.y;
		float b1 = +dir1.x;
		float d1 = -(a1 * start1.x + b1 * start1.y);

		float a2 = -dir2.y;
		float b2 = +dir2.x;
		float d2 = -(a2 * start2.x + b2 * start2.y);

		//подставляем концы отрезков, для выяснения в каких полуплоскотях они
		float seg1_line2_start = a2 * start1.x + b2 * start1.y + d2;
		float seg1_line2_end = a2 * end1.x + b2 * end1.y + d2;

		float seg2_line1_start = a1 * start2.x + b1 * start2.y + d1;
		float seg2_line1_end = a1 * end2.x + b1 * end2.y + d1;

		out_intersection = Vector2.zero;
		//если концы одного отрезка имеют один знак, значит он в одной полуплоскости и пересечения нет.
		if (seg1_line2_start * seg1_line2_end >= 0 || seg2_line1_start * seg2_line1_end >= 0)
			return false;

		float u = seg1_line2_start / (seg1_line2_start - seg1_line2_end);
		out_intersection = start1 + u * dir1;
		return true;
	}

	private class TriTree
    {
		public List<TriTree> tris = new List<TriTree>();
		public TriTree prev = null;
		public int index;

		public TriTree(int i)
        {
			index = i;
        }

		public TriTree(){}

		public void AddList(TriTree added)
        {
			while (added.prev != null)
            {
				added = added.prev;
            }
			TriTree thisStart = this;
			while (thisStart.prev != null)
			{
				thisStart = thisStart.prev;
			}

			if (added.index != thisStart.index)
            {
				thisStart.tris.Add(added);
				added.prev = thisStart;
			}
				
        }

		public static string DebugTree(TriTree tritree)
        {
			string s = "";
			for(int i = 0; i < tritree.tris.Count; i++)
            {
				s += DebugTree(tritree.tris[i]);
            }
			s += tritree.index.ToString();
			return s;
		}

    }
	class Vector2Comparer : IEqualityComparer<Vector2>
	{
		public bool Equals(Vector2 x, Vector2 y)
		{
			return x == y;
		}

		public int GetHashCode(Vector2 product)
		{
			int hashProductName = product.x == null ? 0 : product.x.GetHashCode();

			int hashProductCode = product.x.GetHashCode();

			return hashProductName ^ hashProductCode;
		}
	}
}
