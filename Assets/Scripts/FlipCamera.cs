using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipCamera : MonoBehaviour
{
	private Camera cameraRenderingForProjector;

	private void Start()
	{
		cameraRenderingForProjector = GetComponent<Camera>();
	}

	void OnPreCull()
	{
		cameraRenderingForProjector.ResetWorldToCameraMatrix();
		cameraRenderingForProjector.ResetProjectionMatrix();
		cameraRenderingForProjector.projectionMatrix = cameraRenderingForProjector.projectionMatrix * Matrix4x4.Scale(new Vector3(-1, 1, 1));
	}

	void OnPreRender()
	{
		GL.SetRevertBackfacing(true);
	}

	void OnPostRender()
	{
		GL.SetRevertBackfacing(false);
	}
}
