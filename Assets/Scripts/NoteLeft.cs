using System;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;



[RequireComponent(typeof(EventTrigger))]
public class NoteLeft : MonoBehaviour {

	public Scroll scroll;
	public ScrollNote note;

	private EventTrigger eventTrigger;

	void Start () {
		eventTrigger = GetComponent<EventTrigger>();
		eventTrigger.AddEventTrigger(OnDrag, EventTriggerType.Drag);
		scroll = GetComponentInParent<Scroll>();
	}

	void OnDrag (BaseEventData data) {
		PointerEventData ped = (PointerEventData)data;
		if ((note.mode == 1) &&
		    ((note.Size.x > scroll.itemSize.x) || (ped.delta.x < 0)) &&
		    ((note.X > 0) || (ped.delta.x > 0))) {
			note.Size -= new Vector2(ped.delta.x, 0);
			note.X = note.X + ped.delta.x;


			scroll.RefreshNoteFromLeft(note);
		} 
	}
}