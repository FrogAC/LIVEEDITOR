using System;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollItem : MonoBehaviour,IPointerClickHandler {


	public RectTransform rect {
		get{ return gameObject.GetComponent<RectTransform>(); }
	}

	public Scroll scroll;

	public int Index;

	public bool MoveToRight, MoveToLeft;

	public Text text;
	NoteType newNote;


	public float X {
		get{ return rect.anchoredPosition.x; }
		set{ rect.anchoredPosition = new Vector2(value, rect.anchoredPosition.y); }
	}

	public float Y {
		get{ return rect.anchoredPosition.y; }
		set{ rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, value); }
	}

	public Vector2 Size {
		get{ return rect.sizeDelta; }
		set{ rect.sizeDelta = value; }
	}

	void Awake () {
		scroll = gameObject.GetComponentInParent<Scroll>();
	}

	public void OnPointerClick (PointerEventData ped) {

		Debug.Log(scroll.isEditMode);
		if (scroll.isEditMode) {
			Debug.Log(2);
			newNote = new NoteType();
			newNote.starttime = (Index) * scroll.TimeCoefficient;
			newNote.endtime = (Index + 1) * scroll.TimeCoefficient;
			newNote.lane = (int)(Y / Size.y);
			scroll.notes.Add(newNote);
			scroll.OnScrollUpdate();
		}
	}
}
