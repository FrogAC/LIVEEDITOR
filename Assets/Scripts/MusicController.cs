using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MusicController : MonoBehaviour {
	AudioSource audioClip;
	int resolution = 60;

	public float[] waveForm;
	float[] samples;

	void Start () {
		audioClip = GetComponent<AudioSource>();

	}

	void Update () {
		for (int i = 0; i < waveForm.Length - 1; i++) {
			Vector3 sv = new Vector3(i * .01f, waveForm[i] * 100, 0);
			Vector3 ev = new Vector3(i * .01f, -waveForm[i] * 100, 0);

			Debug.DrawLine(sv, ev, Color.yellow);
		}

		int current = audioClip.timeSamples / resolution;
		current *= 2;

		Vector3 c = new Vector3(current * .01f, 0, 0);

		Debug.DrawLine(c, c + Vector3.up * 10, Color.white);
	}
}

