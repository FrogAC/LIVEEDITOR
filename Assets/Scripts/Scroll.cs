using UnityEngine.UI;
using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class NoteType {
	public double starttime;
	public double endtime;
	///0:single; 1:long;
	public int type;
	public int lane;
	public bool isActive = false;
}

public class Scroll : MonoBehaviour {
	public GameObject ItemPrototype, NotePrototype;

	public float Height;
	float sizeOffset = 1;
	public int BPM, InstanceCount, ItemCount, TimeCoefficient, NoteInstanceCount, NotesCountMax;


	public ScrollRect scroll;
	public RectTransform Content, BlockPanel, NotePanel;

	public float ShortNoteX;
	private float contentShift, sizeCoefficient;
	private Vector2 itemSize;
	private int minItem, maxItem;
	private ScrollItem[,] instances;
	private ScrollNote[] noteInstances;
	public List<ScrollNote> activeNotes;
	public List<NoteType> notes;

	protected virtual void RebuildInstances () {
		instances = new ScrollItem[9, InstanceCount];
		for (int i = 0; i < 9; i++)
			for (int j = 0; j < InstanceCount; j++) {
				var copy = Instantiate(ItemPrototype, BlockPanel, false);

				instances[i, j] = copy.GetComponent<ScrollItem>();
				instances[i, j].Index = j;
				instances[i, j].Size = itemSize;
				instances[i, j].Y = Height / 9 * i;
			}
	}

	protected virtual void RebuildNotes () {
		activeNotes = new List<ScrollNote>();
		noteInstances = new ScrollNote[NoteInstanceCount];
		notes = new List<NoteType>();
		for (int i = 0; i < NoteInstanceCount; i++) {
			var copy = Instantiate(NotePrototype, NotePanel, false);
			noteInstances[i] = copy.GetComponent<ScrollNote>();
			noteInstances[i].SetActive = false;
		}
	}

	protected virtual void RebuildContent () {
		Content.anchoredPosition = new Vector2(0, 0);
		BlockPanel.anchoredPosition = new Vector2(0, 0);
		NotePanel.anchoredPosition = new Vector2(0, 0);
		Content.sizeDelta = new Vector2(itemSize.x * ItemCount, Height);
		BlockPanel.sizeDelta = Content.sizeDelta;
		NotePanel.sizeDelta = Content.sizeDelta;
		/*	if (ItemCount <= InstanceCount) {
			for (int i = 0; i < ItemCount; i++)
				for (int j = 0; j < 9; j++) {
					instances[j, i].gameObject.SetActive(true);
					SetItem(instances[j, i], i);
				}
			
			for (int i = ItemCount; i < InstanceCount; i++)
				for (int j = 0; j < 9; j++) {
					instances[j, i].gameObject.SetActive(false);
				}

			disabled = true;
		} */
		for (int i = 0; i < InstanceCount; i++)
			for (int j = 0; j < 9; j++) {
				instances[j, i].gameObject.SetActive(true);
				SetItem(instances[j, i], i);
			}
	
		contentShift = Content.anchoredPosition.x;

	}

	public virtual void ChangeItemCount (int change) {
		ItemCount += change;
		Content.sizeDelta = new Vector2(itemSize.x * ItemCount, Height);
		BlockPanel.sizeDelta = Content.sizeDelta;
		NotePanel.sizeDelta = Content.sizeDelta;
	}

	public virtual void ResizeContent (float sizeDelta) {
		//resize content and activenotes
		sizeOffset *= sizeDelta;
		itemSize.x = itemSize.x * sizeDelta;
		for (int i = 0; i < 9; i++)
			for (int j = 0; j < InstanceCount; j++) {
				instances[i, j].Size = itemSize;

				SetItem(instances[i, j], instances[i, j].Index);
			}

		Content.sizeDelta = new Vector2(itemSize.x * ItemCount, Height);
		BlockPanel.sizeDelta = Content.sizeDelta;
		NotePanel.sizeDelta = Content.sizeDelta;

		foreach (ScrollNote scrollNote in activeNotes) {
			NoteType note = notes[scrollNote.count];
			double startPosX = (note.starttime - scrollNote.Index * TimeCoefficient) / TimeCoefficient * itemSize.x + instances[0, scrollNote.Index].X;

			double noteSizeX;
			if (note.type == 0)
				noteSizeX = ShortNoteX;
			else
				noteSizeX = (note.endtime - note.starttime) / TimeCoefficient * itemSize.x;
			scrollNote.X = (float)startPosX;
			scrollNote.Size = new Vector2((float)noteSizeX, itemSize.y);

		}

		OnScrollUpdate();
	}

	public virtual void ResizeToNormal () {
		ResizeContent(1 / sizeOffset);
		sizeOffset = 1;
	}

	public virtual void SetBPM (int BPM) {
		TimeCoefficient = 60000 / BPM;
	}

	protected virtual void SetItem (ScrollItem item, int index) {
		if (index < 0 || index >= ItemCount)
			return;

		item.Index = index;
		item.X = itemSize.x * index;
	}

	public virtual void OnScrollUpdate () {
		var shift = contentShift - Content.anchoredPosition.x;
		int minIndex = Mathf.FloorToInt((itemSize.x + shift) / itemSize.x);
		int maxIndex = minIndex + InstanceCount - 1;
		minItem = 999999999;
		maxItem = 0;


		bool dirty = false;
		for (int i = 0; i < InstanceCount; i++) {
			var item = instances[0, i];
			if (item.Index > maxItem)
				maxItem = item.Index;
			if (item.Index < minItem)
				minItem = item.Index;
			if (item.Index < minIndex - 2) {
				dirty = true;
				item.MoveToRight = true;
			} else if (item.Index > maxIndex - 2) { 
				dirty = true;
				item.MoveToLeft = true;
			}
		}
		if (dirty) {
			for (int i = 0; i < InstanceCount; i++) {
				var item = instances[0, i];
				if (item.MoveToRight) {
					item.MoveToRight = false;
					maxItem++;
					minItem++;
					for (int j = 0; j < 9; j++) {
						
						SetItem(instances[j, i], maxItem);
					}
				} else if (item.MoveToLeft) {
					item.MoveToLeft = false;
					minItem--;
					maxItem--;
					for (int j = 0; j < 9; j++) {
						SetItem(instances[j, i], minItem);
					}
				}
			}
		}

		for (int i = 0; i < InstanceCount; i++) {
			var item = instances[8, i];
			String minutes = Mathf.Floor(item.Index * TimeCoefficient / 1000 / 60).ToString("0");
			String seconds = Mathf.Floor(item.Index * TimeCoefficient / 1000 % 60).ToString("00");
			String microSeconds = Mathf.Floor(item.Index * TimeCoefficient % 1000).ToString("000");
			item.text.text = String.Format("{0,0}:{1,0}:{2,0}", minutes, seconds, microSeconds);
		}

		//Note
		foreach (var note in activeNotes) {
			if (note.mode == 0)
			if ((note.Index > maxIndex + 1) || (note.EndIndex < minIndex - 1)) {
				notes[note.count].isActive = false;

				note.SetActive = false;
				activeNotes.Remove(note);
			}
		}

		var minTime = minIndex * TimeCoefficient;
		for (int j = 0; j < notes.Count; j++) {

			NoteType note = notes[j];
			if (!note.isActive) {
				int startIndex = (int)(note.starttime / TimeCoefficient);
				//pos
				for (int i = 0; i < InstanceCount; i++) {
					if (instances[0, i].Index == startIndex) {
						double startPosX = (note.starttime - startIndex * TimeCoefficient) / TimeCoefficient * itemSize.x + instances[0, i].X;

						double startPosY = note.lane * itemSize.y;
						double noteSizeX;
						if (note.type == 0)
							noteSizeX = ShortNoteX;
						else
							noteSizeX = (note.endtime - note.starttime) / TimeCoefficient * itemSize.x;
						foreach (ScrollNote noteIns in noteInstances) {
							if (!noteIns.SetActive) {
								noteIns.X = (float)startPosX;
								noteIns.Y = (float)startPosY;
								noteIns.Size = new Vector2((float)noteSizeX, itemSize.y);
								noteIns.SetActive = true;
								noteIns.Index = startIndex;
								noteIns.EndIndex = startIndex + (int)(noteSizeX / itemSize.y);
								noteIns.count = j;
								activeNotes.Add(noteIns);
								break;
							}
						}

						note.isActive = true;
					}
				}

			}
		}

	}

	public void Awake () {
		itemSize = new Vector2(100, Height / 9);
		RebuildInstances();
		RebuildContent();
		RebuildNotes();

		AddNote(200, 2000, 2);

		AddNote(200, 200, 3);

		AddNote(2000, 3000, 8);
		minItem = instances[0, 0].Index;
		maxItem = minItem + InstanceCount - 1;

		OnScrollUpdate();
	}

	/// <summary>
	/// type 0 = Single
	/// type 1 = Long
	/// </summary>
	public virtual void AddNote (float start, float end, int laneN) {
		var newNote = new NoteType();
		newNote.starttime = start;
		newNote.endtime = end;
		newNote.lane = laneN;
		if (start == end)
			newNote.type = 0;
		else
			newNote.type = 1;

		notes.Add(newNote);
	}

	public virtual void DeleteNote (int count) {
		notes.Remove(notes[count]);
		ScrollNote delNote = null;
		foreach (var note in activeNotes) {
			if (note.count == count) {
				delNote = note;
			}
			if (note.count > count) {
				note.count--;
			}
		}

		delNote.SetActive = false;
		activeNotes.Remove(delNote);

	}

	public virtual void Refresh () {
		
	}
}
