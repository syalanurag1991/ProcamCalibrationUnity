using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UndistortDisplay : MonoBehaviour
{
	// Check in inspector to show point cloud
	public bool displayPointCloud;
	
	// File that contains homography and inverse homography matrices
	public TextAsset calibrationFile;

	// For holding reference to active kinect manager
	private static KinectManager kinectManagerInstance;

	// For holding imported processed kinect data
	private static KinectData processedKinectData;

	// Supporting textures for displaying streams
	private Texture2D undistortedColorStreamTexture;
	private Color32[] undistortedColorStreamColors;
	private Texture2D undistortedColorStreamTextureForProjector;
	private Color32[] undistortedColorStreamColorsForProjector;

	// Display kinect streams on UI
	public RawImage undistortedColorStreamDisplay;
	public RawImage undistortedColorStreamDisplayForProjector;

	private double inv_m11, inv_m12, inv_m13, inv_m21, inv_m22, inv_m23, inv_m31, inv_m32, inv_m33;

	// Supporting components for displaying point-cloud
	public GameObject MeshObjectPrefab;
	public float rotationSpeed;
	public int projectorWidth = 1024;
	public int projectorHeight = 768;

	public int downsampleSize = 4;
	public int NumberOfMeshes = 16;
	public int MeshWidth = 256;
	public int MeshHeight = 192;

	public float depthScale = 0.05f;

	private GameObject[] MeshObjects;
	private Mesh[] Meshes;
	private Color32[][] MeshColors;
	private Vector3[][] MeshVertices;

	public ushort[] gradedLevels;
	public Color32[] gradedLevelColors;

	void Start()
	{
		Debug.Log(calibrationFile.text);
		int index = calibrationFile.text.IndexOf('I');
		string remainingText = calibrationFile.text.Substring(index + ("Inverse Homography").Length);
		string[] values = remainingText.Split(" "[0]);

		//inv_m11 = 0.230403;
		//inv_m12 = -0.0180729;
		//inv_m13 = 121.62;
		//inv_m21 = 0.0015293;
		//inv_m22 = 0.216588;
		//inv_m23 = 77.6476;
		//inv_m31 = -2.92417e-06;
		//inv_m32 = -6.4123e-05;
		//inv_m33 = 0.975638;

		inv_m11 = double.Parse(values[0]);
		inv_m12 = double.Parse(values[1]);
		inv_m13 = double.Parse(values[2]);
		inv_m21 = double.Parse(values[3]);
		inv_m22 = double.Parse(values[4]);
		inv_m23 = double.Parse(values[5]);
		inv_m31 = double.Parse(values[6]);
		inv_m32 = double.Parse(values[7]);
		inv_m33 = double.Parse(values[8]);

		MeshWidth = projectorWidth / downsampleSize;
		MeshHeight = projectorHeight / downsampleSize;
		
		gradedLevels = new ushort[6];
		gradedLevels[0] = 1000;
		gradedLevels[1] = 1500;
		gradedLevels[2] = 2000;
		gradedLevels[3] = 2500;
		gradedLevels[4] = 3000;
		gradedLevels[5] = 3500;

		//gradedLevelColors = new Color32[5];
		
		if (displayPointCloud)
		{
			CreateMeshesForPointCloud();
			undistortedColorStreamDisplayForProjector.enabled = false;
		}
		else
			undistortedColorStreamColorsForProjector = new Color32[projectorWidth * projectorHeight];

		undistortedColorStreamColors = new Color32[projectorWidth * projectorHeight];
		
	}

	void Update()
	{
		// move point cloud in 3D space // rotate point cloud
		float xVal = -Input.GetAxis("Horizontal");
		float yVal = Input.GetAxis("Vertical");
		transform.Translate(
			xVal * Time.deltaTime * rotationSpeed,
			yVal * Time.deltaTime * rotationSpeed,
			0.0f,
			Space.Self);
		//transform.Rotate(
		//	(xVal * Time.deltaTime * rotationSpeed),
		//	(yVal * Time.deltaTime * rotationSpeed),
		//	0,
		//	Space.Self);

		if (kinectManagerInstance == null)
		{
			SetKinectManagerInstance();
			return;
		}

		if (processedKinectData == null)
		{
			kinectManagerInstance.GetProcessedKinectData(ref processedKinectData);
			SetStreamTextures();
			if(displayPointCloud)
				SetPointCloudMeshes();
			return;
		}

		RefreshStreamsAndPointCloud();
		UpdateStreamTextures();
	}

	private void SetKinectManagerInstance()
	{
		KinectManager kinectManagerObject = FindObjectOfType<KinectV1Manager>();
		if (kinectManagerObject != null)
		{
			Debug.Log("Found Kinect V1");
			kinectManagerInstance = KinectManager.Instance;

			return;
		}

		kinectManagerObject = FindObjectOfType<KinectV2Manager>();
		if (kinectManagerObject != null)
		{
			Debug.Log("Found Kinect V2");
			kinectManagerInstance = KinectManager.Instance;
			return;
		}

		Debug.Log("No active kinect manager instance found!");
		return;
	}

	void SetStreamTextures()
	{
		// Initialize texture
		undistortedColorStreamTexture = new Texture2D(projectorWidth, projectorHeight, TextureFormat.RGBA32, false);

		if(!displayPointCloud)
			undistortedColorStreamTextureForProjector = new Texture2D(projectorWidth, projectorHeight, TextureFormat.RGBA32, false);

		// Undistorted color stream
		if (undistortedColorStreamDisplay != null)
			undistortedColorStreamDisplay.texture = undistortedColorStreamTexture;

		// Undistorted color stream for projector
		if (undistortedColorStreamDisplayForProjector != null && (!displayPointCloud))
			undistortedColorStreamDisplayForProjector.texture = undistortedColorStreamTextureForProjector;
	}

	void UpdateStreamTextures()
	{
		// Update registered color stream
		undistortedColorStreamTexture.SetPixels32(undistortedColorStreamColors);
		if (!displayPointCloud)
			undistortedColorStreamTextureForProjector.SetPixels32(undistortedColorStreamColorsForProjector);

		// Apply all texture changes 
		undistortedColorStreamTexture.Apply(false, false);
		if (!displayPointCloud)
			undistortedColorStreamTextureForProjector.Apply(false, false);
	}

	private void CreateMeshesForPointCloud()
	{
		Debug.Log("Meshes created");
		Meshes = new Mesh[NumberOfMeshes];                                                                                              // create mesh components for mesh objects
		MeshVertices = new Vector3[NumberOfMeshes][];
		MeshColors = new Color32[NumberOfMeshes][];
		int[][] meshTriangles = new int[NumberOfMeshes][];

		for (int i = 0; i < NumberOfMeshes; i++)                                                                                        // assign mesh components to mesh objects
		{
			Meshes[i] = new Mesh();
			MeshVertices[i] = new Vector3[MeshWidth * MeshHeight];
			MeshColors[i] = new Color32[MeshWidth * MeshHeight];
			meshTriangles[i] = new int[6 * ((MeshWidth - 1) * (MeshHeight - 1))];

			int triangleIndex = 0;
			for (int y = 0; y < MeshHeight; y++)
			{
				for (int x = 0; x < MeshWidth; x++)
				{
					int index = (y * MeshWidth) + x;
					MeshVertices[i][index] = new Vector3(x, -y, 0);
					MeshColors[i][index] = new Color32(0, 0, 255, 255);
					if (x != (MeshWidth - 1) && y != (MeshHeight - 1))                                                                  // skip the last row/col
					{
						int topLeft = index;
						int topRight = topLeft + 1;
						int bottomLeft = topLeft + MeshWidth;
						int bottomRight = bottomLeft + 1;
						meshTriangles[i][triangleIndex++] = topLeft;
						meshTriangles[i][triangleIndex++] = topRight;
						meshTriangles[i][triangleIndex++] = bottomLeft;
						meshTriangles[i][triangleIndex++] = bottomLeft;
						meshTriangles[i][triangleIndex++] = topRight;
						meshTriangles[i][triangleIndex++] = bottomRight;
					}
				}
			}

			Meshes[i].vertices = MeshVertices[i];
			Meshes[i].colors32 = MeshColors[i];
			Meshes[i].triangles = meshTriangles[i];
			Meshes[i].RecalculateNormals();
		}
	}

	private void SetPointCloudMeshes()
	{
		MeshObjects = new GameObject[NumberOfMeshes];                                                               // create mesh objects and arrange in grid
		int meshIndex = 0;
		for (int meshY = downsampleSize / 2; meshY > -downsampleSize / 2; meshY--)
		{
			for (int meshX = -downsampleSize / 2; meshX < downsampleSize / 2; meshX++)
			{
				MeshObjects[meshIndex] = GameObject.Instantiate(MeshObjectPrefab, gameObject.transform);
				MeshObjects[meshIndex++].transform.localPosition = new Vector3((float)meshX * (MeshWidth - 1), (float)meshY * (MeshHeight - 1), 0f);
			}
		}

		for (int i = 0; i < NumberOfMeshes; i++)                                                                    // assign mesh components to mesh objects
			MeshObjects[i].GetComponent<MeshFilter>().mesh = Meshes[i];
	}

	private void RefreshStreamsAndPointCloud()
	{
		if (processedKinectData == null || processedKinectData.DepthWidth == 0 || processedKinectData.DepthHeight == 0 || processedKinectData.ColorWidth == 0 || processedKinectData.ColorHeight == 0)
		{
			Debug.Log("Kinect data not initialized");
			return;
		}

		int meshIndex = 0;
		//int projectorPixelIndex = 0;
		double denominator = 1;
		int newX = 0, newY = 0;
		for (int meshY = 0; meshY < downsampleSize; meshY++)
		{
			for (int meshX = 0; meshX < downsampleSize; meshX++)
			{
				int smallIndex = 0;
				for (int y = meshY * MeshHeight; y < (meshY + 1) * MeshHeight; y++)
				{
					for (int x = meshX * MeshWidth; x < (meshX + 1) * MeshWidth; x++)
					{
						int projectorPixelIndex = y * projectorWidth + x;

						denominator = inv_m31 * x + inv_m32 * y + inv_m33;
						newX = (int)((inv_m11 * x + inv_m12 * y + inv_m13) / denominator);
						newY = (int)((inv_m21 * x + inv_m22 * y + inv_m23) / denominator);
						int depthSpaceIndex = newY * KinectV2Wrapper.Constants.DepthImageWidth + newX;

						if ((newX >= 0 && newX < processedKinectData.DepthWidth && newY >= 0 && newY < processedKinectData.DepthHeight) &&
							processedKinectData.CorrectedDepths[depthSpaceIndex] >= gradedLevels[0] &&
							processedKinectData.CorrectedDepths[depthSpaceIndex] < gradedLevels[5])
						{
							undistortedColorStreamColors[projectorPixelIndex] = processedKinectData.RegisteredColorStreamColors[depthSpaceIndex];
							if (processedKinectData.CorrectedDepths[depthSpaceIndex] > gradedLevels[0])
							{
								if(displayPointCloud)
									MeshColors[meshIndex][smallIndex] = new Color(0, 0, 0, 1);
								else
									undistortedColorStreamColorsForProjector[projectorPixelIndex] = gradedLevelColors[0];
							}
								
							if (processedKinectData.CorrectedDepths[depthSpaceIndex] > gradedLevels[1])
							{
								if (displayPointCloud)
									MeshColors[meshIndex][smallIndex] = new Color(0, 1, 0, 0);
								else
									undistortedColorStreamColorsForProjector[projectorPixelIndex] = gradedLevelColors[1];
							}
								
							if (processedKinectData.CorrectedDepths[depthSpaceIndex] > gradedLevels[2])
							{
								if (displayPointCloud)
									MeshColors[meshIndex][smallIndex] = new Color(0, 0, 1, 0);
								else
									undistortedColorStreamColorsForProjector[projectorPixelIndex] = gradedLevelColors[2];
							}
								
							if (processedKinectData.CorrectedDepths[depthSpaceIndex] > gradedLevels[3])
							{
								if (displayPointCloud)
									MeshColors[meshIndex][smallIndex] = new Color(1, 0, 0, 0);
								else
									undistortedColorStreamColorsForProjector[projectorPixelIndex] = gradedLevelColors[3];
							}
								
							if (processedKinectData.CorrectedDepths[depthSpaceIndex] > gradedLevels[4])
							{
								if (displayPointCloud)
									MeshColors[meshIndex][smallIndex] = new Color(0, 0, 0, 0);
								else
									undistortedColorStreamColorsForProjector[projectorPixelIndex] = gradedLevelColors[4];
							}

							if (displayPointCloud)
								MeshVertices[meshIndex][smallIndex++].z = processedKinectData.CorrectedDepths[depthSpaceIndex] * depthScale;       // update mesh geometry
						}
						else
						{
							if(displayPointCloud)
							{
								MeshVertices[meshIndex][smallIndex].z = float.NegativeInfinity;                     // set 'z' to -Infinity
								MeshColors[meshIndex][smallIndex++].a = 0;                                          // set alpha values to 0, update voxel index						}
							} else
								undistortedColorStreamColorsForProjector[projectorPixelIndex] = new Color32(0, 0, 0, 255);
							undistortedColorStreamColors[projectorPixelIndex] = new Color32(0, 0, 0, 255);
						}

						//projectorPixelIndex++;
					}
				}

				if (displayPointCloud)
				{
					Meshes[meshIndex].vertices = MeshVertices[meshIndex];
					Meshes[meshIndex].colors32 = MeshColors[meshIndex];
					Meshes[meshIndex++].RecalculateNormals();
				}
			}
		}

		//Debug.Log("projectorPixelIndex" + projectorPixelIndex.ToString());
	}
}
