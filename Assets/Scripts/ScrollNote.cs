﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollNote : MonoBehaviour,IPointerUpHandler,IDragHandler {
	#region Pointer

	bool isDraging = false;
	public Scroll scroll;
	public Text text;
	public NoteLeft left;
	public NoteRight right;
	/// <summary>
	/// 0:normal 1:highlight
	/// </summary>
	public int mode = 0;

	public void OnPointerUp (PointerEventData eventData) {
		if (isDraging) {
			isDraging = false;
		} else {
			if (mode == 0) {
				mode = 1;
				Debug.Log("HighLight!");
				text.gameObject.SetActive(true);
			} else {
				mode = 0;
				Debug.Log("Normal");
				text.gameObject.SetActive(false);
			}
		}
	}

	public void OnDrag (PointerEventData eventData) {
		if (mode == 0)
			return;
		if (!isDraging)
			isDraging = true;
		X = X + eventData.delta.x;
		scroll.RefreshNoteFromMid(this, eventData.delta.x);
	}


	#endregion


	public RectTransform rect {
		get{ return gameObject.GetComponent<RectTransform>(); }
	}

	public int Index;
	public int EndIndex;
	public int count;

	private bool isActive;

	public bool SetActive {
		get{ return isActive; }
		set {
			isActive = value;
			gameObject.SetActive(value);
		}
	}

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
}
