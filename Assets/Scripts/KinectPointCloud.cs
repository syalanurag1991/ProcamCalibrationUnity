using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KinectPointCloud : MonoBehaviour
{
	// Supporting components for displaying point-cloud
	public GameObject MeshObjectPrefab;
	public float rotationSpeed;

	// For holding reference to active kinect manager
	private static KinectManager kinectManagerInstance;

	// For holding imported processed kinect data
	private static KinectData processedKinectData;

	// Display point-cloud on UI
	private static GameObject[] MeshObjects;
	public bool useShader;
	public ushort[] gradeLevels;

	private void Start()
	{
		gradeLevels = new ushort[6];
		gradeLevels[0] = 1000;
		gradeLevels[1] = 1500;
		gradeLevels[2] = 2000;
		gradeLevels[3] = 2500;
		gradeLevels[4] = 3000;
		gradeLevels[5] = 3500;
	}

	private void Update()
    {
		if (kinectManagerInstance == null)
		{
			SetKinectManagerInstance();
			return;
		} else
		{
			if (kinectManagerInstance.displayPointCloud)
			{
				float yVal = Input.GetAxis("Horizontal");                                                                               // move point cloud in 3D space
				float xVal = -Input.GetAxis("Vertical");
				transform.Rotate(
					(xVal * Time.deltaTime * rotationSpeed),
					(yVal * Time.deltaTime * rotationSpeed),
					0,
					Space.Self);
			}
		}

		if (processedKinectData == null)
		{
			kinectManagerInstance.GetProcessedKinectData(ref processedKinectData);
			//if (kinectManagerInstance.displayStreams)
			if (kinectManagerInstance.displayPointCloud)
				SetPointCloudMeshes();
			return;
		}
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

	private void SetPointCloudMeshes()
	{
		MeshObjects = new GameObject[processedKinectData.NumberOfMeshes];																// create mesh objects and arrange in grid
		int meshIndex = 0;
		for (int meshY = processedKinectData.DownsampleSize / 2; meshY > -processedKinectData.DownsampleSize / 2; meshY--)
		{
			for (int meshX = -processedKinectData.DownsampleSize / 2; meshX < processedKinectData.DownsampleSize / 2; meshX++)
			{
				MeshObjects[meshIndex] = GameObject.Instantiate(MeshObjectPrefab, gameObject.transform);
				MeshObjects[meshIndex++].transform.localPosition = new Vector3((float)meshX * (processedKinectData.MeshWidth - 1), (float)meshY * (processedKinectData.MeshHeight - 1), 0f);
			}
		}

		for (int i = 0; i < processedKinectData.NumberOfMeshes; i++)																	// assign mesh components to mesh objects
			MeshObjects[i].GetComponent<MeshFilter>().mesh = processedKinectData.Meshes[i];
	}
}
