using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

public class TerrainSystem : MonoBehaviour
{
	public Texture2D heightMap;
	public int pieceDimension;

	private int hmHeight;
	private int hmWidth;
	private GameObject[] pieces;

	[ComputeJobOptimization]
	struct SplitAndRemapJob : IJobParallelFor
	{
		public int hmHeight;
		public int hmWidth;
		public int pieceDimension;
		public int pixelCountInPiece;

		public NativeArray<Color> pixels;
		public NativeArray<NativeArray<float>> rawPieces;
		
		public void Execute(int pixelOffset)
		{
			int rowOffset = pixelOffset / hmWidth;
			int colOffset = pixelOffset - (rowOffset * hmWidth);
			
			int pieceRowIndx = rowOffset / pixelCountInPiece;
			int pieceColIndx = colOffset / pixelCountInPiece;
			int pieceRowOffset = rowOffset - (pixelCountInPiece * pieceRowIndx);
			int pieceColOffset = colOffset - (pixelCountInPiece * pieceColIndx);

			Color pixel = pixels[pixelOffset];
			NativeArray<float> rawPiece = rawPieces[pieceRowIndx * pieceDimension + pieceColIndx];
			rawPiece[pieceRowOffset * pixelCountInPiece + pieceColOffset] = pixel.grayscale;
		}
	}

	public void create()
	{
		hmWidth = heightMap.width;
		hmHeight = heightMap.height;
		NativeArray<Color> pixels = new NativeArray<Color>(heightMap.GetPixels(), Allocator.Persistent);
		
		int pixelCountInPiece = pixels.Length / (pieceDimension * pieceDimension);
		NativeArray<NativeArray<float>> rawPieces = new NativeArray<NativeArray<float>>(pieceDimension * pieceDimension, Allocator.Persistent);
		for (int i = 0; i < rawPieces.Length; ++i)
		{
			rawPieces[i] = new NativeArray<float>(pixelCountInPiece, Allocator.Persistent);
		}
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(TerrainSystem))]
public class TerrainSystemEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		TerrainSystem obj = (TerrainSystem) target;
		if (GUILayout.Button("Create"))
		{
			obj.create();
		}
	}
}
#endif