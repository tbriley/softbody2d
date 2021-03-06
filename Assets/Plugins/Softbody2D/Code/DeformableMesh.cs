﻿namespace FinerGames {

	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System;

	[ExecuteInEditMode][RequireComponent (typeof (MeshFilter), typeof (MeshRenderer))]
	public class DeformableMesh : MonoBehaviour {
		
		public bool CalculateNormals = false;
		public bool CalculateTangents = false;

		public bool CanModifyPivotPoint = true;

		public Softbody2D Softbody2D;
		public Vector2 pivotOffset = Vector2.zero;
		public Vector2 offset = Vector2.zero;

		public Vector2 scale = Vector2.one;
		public float angle =  0f;

		[SerializeField]
		public List<Transform> Vertices = new List<Transform> ();

		[SerializeField]
		protected MeshFilter meshFilter;

		public MeshFilter MeshFilter
		{
			get
			{
				if (meshFilter == null) 
				{
					meshFilter = GetComponent<MeshFilter>();

					if (meshFilter == null) 
						meshFilter = gameObject.AddComponent<MeshFilter>();
				}

				return meshFilter;
			}
			set { meshFilter = value; }
		}

		[SerializeField]
		protected MeshRenderer meshRenderer;
		public MeshRenderer MeshRenderer
		{
			get
			{
				if (meshRenderer == null) 
				{
					meshRenderer = GetComponent<MeshRenderer>();

					if (meshRenderer == null) 
						meshRenderer = gameObject.AddComponent<MeshRenderer>();
				}

				return meshRenderer;
			}
			set { meshRenderer = value; }
		}

		void Awake()
		{
			MeshFilter.sharedMesh = new Mesh();;

			//TODO do what this says!!!
			//make sure to handle the case of LinkedMeshRenderer.sharedMaterial being null derived classes. (in the initilize method)
			if(MeshRenderer.sharedMaterial != null)
				MeshRenderer.sharedMaterial = new Material(MeshRenderer.sharedMaterial);

			Initialize(true);
		}

		public virtual void Initialize(bool forceUpdate = false)
		{
			Softbody2D = GetComponent<Softbody2D>();

			if (MeshFilter.sharedMesh == null)
				MeshFilter.sharedMesh = new Mesh();
		}

		public virtual void UpdateMesh(Vector2[] points){}

		public virtual bool UpdatePivotPoint (Vector2 change, out MonoBehaviour monoBehavior){ monoBehavior = null;  return false;}


		void OnDestroy() {
			#if UNITY_EDITOR
			DestroyImmediate(MeshRenderer.sharedMaterial);
			DestroyImmediate(MeshFilter.sharedMesh);
			#else
			Destroy(MeshRenderer.sharedMaterial);
			Destroy(MeshFilter.sharedMesh);
			#endif
		}

		public virtual void ApplyNewOffset(Vector2 newOffset) {
			Vector2 oldOffset = offset;
			offset = newOffset;

			if(MeshFilter.sharedMesh != null)
			{
				Vector2[] uvPts = MeshFilter.sharedMesh.uv;

				for(int i= 0; i < uvPts.Length; i++)
					uvPts[i] += oldOffset - offset;

				MeshFilter.sharedMesh.uv = uvPts;
			}
		}

		protected void CalculateMeshTangents () {
			//speed up math by copying the mesh arrays
			int[] triangles = MeshFilter.sharedMesh.triangles;
			Vector3[] vertices = MeshFilter.sharedMesh.vertices;
			Vector2[] uv = MeshFilter.sharedMesh.uv;
			Vector3[] normals = MeshFilter.sharedMesh.normals;

			//variable definitions
			int triangleCount = triangles.Length;
			int vertexCount = vertices.Length;

			Vector3[] tan1 = new Vector3[vertexCount];
			Vector3[] tan2 = new Vector3[vertexCount];

			Vector4[] tangents = new Vector4[vertexCount];

			for (long a = 0; a < triangleCount; a += 3)
			{
				long i1 = triangles[a + 0];
				long i2 = triangles[a + 1];
				long i3 = triangles[a + 2];

				Vector3 v1 = vertices[i1];
				Vector3 v2 = vertices[i2];
				Vector3 v3 = vertices[i3];

				Vector2 w1 = uv[i1];
				Vector2 w2 = uv[i2];
				Vector2 w3 = uv[i3];

				float x1 = v2.x - v1.x;
				float x2 = v3.x - v1.x;
				float y1 = v2.y - v1.y;
				float y2 = v3.y - v1.y;
				float z1 = v2.z - v1.z;
				float z2 = v3.z - v1.z;

				float s1 = w2.x - w1.x;
				float s2 = w3.x - w1.x;
				float t1 = w2.y - w1.y;
				float t2 = w3.y - w1.y;

				float r = 1.0f / (s1 * t2 - s2 * t1);

				Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
				Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

				tan1[i1] += sdir;
				tan1[i2] += sdir;
				tan1[i3] += sdir;

				tan2[i1] += tdir;
				tan2[i2] += tdir;
				tan2[i3] += tdir;
			}


			for (long a = 0; a < vertexCount; ++a)
			{
				Vector3 n = normals[a];
				Vector3 t = tan1[a];

				//Vector3 tmp = (t - n * Vector3.Dot(n, t)).normalized;
				//tangents[a] = new Vector4(tmp.x, tmp.y, tmp.z);
				Vector3.OrthoNormalize(ref n, ref t);
				tangents[a].x = t.x;
				tangents[a].y = t.y;
				tangents[a].z = t.z;

				tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
			}

			MeshFilter.sharedMesh.tangents = tangents;
		}
	}
}
