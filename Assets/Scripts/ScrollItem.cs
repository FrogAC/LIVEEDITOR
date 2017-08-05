using UnityEngine;
using UnityEngine.UI;

public class ScrollItem : MonoBehaviour {
	public RectTransform rect {
		get{ return gameObject.GetComponent<RectTransform>(); }
	}

	public int Index;

	public bool MoveToRight, MoveToLeft;

	public Text text;



	public float X {
		get{ return rect.anchoredPosition.x; }
		set{ rect.anchoredPosition = new Vector2(value, rect.anchoredPosition.y); }
	}

	public float Y {
		get{ return rect.anchoredPosition.y; }
		set{ rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, value); }
	}

	public Vector2 Size {
		set{ rect.sizeDelta = value; }
	}
}
