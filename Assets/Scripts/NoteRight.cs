using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class NoteRight: MonoBehaviour,IPointerClickHandler {
	public ScrollNote note;

	public void OnPointerClick (PointerEventData eventData) {
		if (note.mode == 1) {
			Debug.Log("Right");
		}
	}

}
