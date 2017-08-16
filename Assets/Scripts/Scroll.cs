using UnityEngine.UI;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.EventSystems;

[System.Serializable]
public class NoteType {
	public double starttime;
	public double endtime;
	///0:single; 1:long;
	public int type;
	//output only
	public bool parallel;
	public int lane;
	public bool isActive = false;
}



public class Scroll : MonoBehaviour {
	public GameObject ItemPrototype, NotePrototype;

	const int PanelOffset = 90;

	public float Height, AspectRatio, Width;
	public float sizeOffset = 1, timeOffset = 1, TimeCoefficient;
	public int BPM, InstanceCount, ItemCount, NoteInstanceCount, NotesCountMax;


	public ScrollRect scroll;
	public RectTransform Content, BlockPanel, NotePanel;

	public float ShortNoteX;
	private float contentShift, sizeCoefficient;
	public Vector2 itemSize;
	private int minItem, maxItem;
	private ScrollItem[,] instances;
	private ScrollNote[] noteInstances;
	public List<ScrollNote> activeNotes;
	public List<NoteType> notes, sortNotes;
	private EventTrigger eventTrigger;
	public bool isEditMode, isHolding = false;

	public StreamWriter writer;
	public string OutputFile, AudioFile;

	NoteType noteNew;


	public void Awake () { 
		isEditMode = true;
		TimeCoefficient = 30000 / BPM;
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

	void Start () {

		AspectRatio = (float)Screen.width / (float)Screen.height;
		Width = Height * AspectRatio;
		eventTrigger = GetComponent<EventTrigger>();
		//eventTrigger.AddEventTrigger(OnPointerClick, EventTriggerType.PointerClick);
	}

	#region editor


	void OnPointerClick (BaseEventData data) {
		//implement in ScrollItem

		if (!isEditMode)
			return;
		PointerEventData ped = (PointerEventData)data;
		float starttime = 0;
		int lane = (int)(((ped.position.y / Screen.height) * Height - 20) / itemSize.y);
		float startPos = ped.position.x - PanelOffset;
		foreach (ScrollItem item in instances) {
			if ((item.X >= startPos) && (item.X < startPos + itemSize.x)) {
				starttime = ((startPos - item.X) / itemSize.y * TimeCoefficient) + item.Index * TimeCoefficient;

				Debug.Log(item.Index);
				break;
			}
		}
		noteNew = new NoteType();
		noteNew.starttime = starttime;
		noteNew.endtime = starttime + TimeCoefficient;
		noteNew.lane = lane;
		notes.Add(noteNew);

		OnScrollUpdate();
	}


	void OnPointerUp (BaseEventData data) {
		if (!isEditMode)
			return;
		isHolding = false;
		PointerEventData ped = (PointerEventData)data;
	}

	#endregion

	# region rebuild

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
		for (int i = 0; i < InstanceCount; i++)
			for (int j = 0; j < 9; j++) {
				instances[j, i].gameObject.SetActive(true);
				SetItem(instances[j, i], i);
			}
	
		contentShift = Content.anchoredPosition.x;

	}

	#endregion

	#region resize

	public virtual void ChangeItemCount (int change) {
		ItemCount += change;
		Content.sizeDelta = new Vector2(itemSize.x * ItemCount, Height);
		BlockPanel.sizeDelta = Content.sizeDelta;
		NotePanel.sizeDelta = Content.sizeDelta;
	}

	public virtual void ResizeContent (float sizeDelta) {
		//resize content and activenotes
		if (sizeDelta == 0) {
			throw new ArgumentOutOfRangeException();
		}
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
			double startPosX = (note.starttime - scrollNote.Index * TimeCoefficient) / TimeCoefficient * itemSize.x + scrollNote.Index * itemSize.x;

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

	public virtual void ResizeTime (float sizeDelta) {
		if (sizeDelta == 0) {
			throw new ArgumentOutOfRangeException();
		}
		timeOffset *= sizeDelta;
		TimeCoefficient *= sizeDelta;
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
		SetBPM(BPM);
		sizeOffset = 1;
	}



	public virtual void SetBPM (int bpm) {
		TimeCoefficient = 60000 / bpm;
		timeOffset = 1;
		BPM = bpm;
		//change Note SIze
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

	protected virtual void SetItem (ScrollItem item, int index) {
		if (index < 0 || index >= ItemCount)
			return;

		item.Index = index;
		item.X = itemSize.x * index;
	}

	#endregion


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

		for (int j = 0; j < notes.Count; j++) {

			NoteType note = notes[j];
			if (!note.isActive) {
				int startIndex = (int)(note.starttime / TimeCoefficient);
				double noteSizeX;
				if (note.type == 0)
					noteSizeX = itemSize.x;
				else
					noteSizeX = (note.endtime - note.starttime) / TimeCoefficient * itemSize.x;
				int endIndex = startIndex + (int)(noteSizeX / itemSize.y);
				//pos
				for (int i = 0; i < InstanceCount; i++) {
					if ((instances[0, i].Index == startIndex) && (!note.isActive)) {
						double startPosX = (note.starttime - startIndex * TimeCoefficient) / TimeCoefficient * itemSize.x + instances[0, i].X;

						double startPosY = note.lane * itemSize.y;
						foreach (ScrollNote noteIns in noteInstances) {
							if (!noteIns.SetActive) {
								noteIns.X = (float)startPosX;
								noteIns.Y = (float)startPosY;
								noteIns.Size = new Vector2((float)noteSizeX, itemSize.y);
								noteIns.SetActive = true;
								noteIns.Index = startIndex;
								noteIns.EndIndex = endIndex;
								noteIns.count = j;
								activeNotes.Add(noteIns);
								break;
							}
						}

						note.isActive = true;
					} else if ((instances[0, i].Index == endIndex) && (!note.isActive)) {
						double startPosX = (note.endtime - endIndex * TimeCoefficient) / TimeCoefficient * itemSize.x + instances[0, i].X - noteSizeX;

						double startPosY = note.lane * itemSize.y;
						foreach (ScrollNote noteIns in noteInstances) {
							if (!noteIns.SetActive) {
								noteIns.X = (float)startPosX;
								noteIns.Y = (float)startPosY;
								noteIns.Size = new Vector2((float)noteSizeX, itemSize.y);
								noteIns.SetActive = true;
								noteIns.Index = startIndex;
								noteIns.EndIndex = endIndex;
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



	#region noteControl

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

	public virtual void RefreshNoteFromLeft (ScrollNote noteIns) {
		NoteType note = notes[noteIns.count];
		float time = (noteIns.Size.x / itemSize.x) * TimeCoefficient;
		note.starttime = note.endtime - time;
		noteIns.Index = (int)(note.starttime / TimeCoefficient);
		//format

		String minutes = Mathf.Floor(time / 1000 / 60).ToString("0");
		String seconds = Mathf.Floor(time / 1000 % 60).ToString("00");
		String microSeconds = Mathf.Floor(time % 1000).ToString("000");
		noteIns.text.text = String.Format("{0,0}:{1,0}:{2,0}", minutes, seconds, microSeconds);
	}

	public virtual void RefreshNoteFromRight (ScrollNote noteIns) {
		NoteType note = notes[noteIns.count];
		float time = (noteIns.Size.x / itemSize.x) * TimeCoefficient;
		note.endtime = note.starttime + time;
		noteIns.EndIndex = (int)(note.endtime / TimeCoefficient);

		String minutes = Mathf.Floor(time / 1000 / 60).ToString("0");
		String seconds = Mathf.Floor(time / 1000 % 60).ToString("00");
		String microSeconds = Mathf.Floor(time % 1000).ToString("000");
		noteIns.text.text = String.Format("{0,0}:{1,0}:{2,0}", minutes, seconds, microSeconds);
	}

	public virtual void RefreshNoteFromMid (ScrollNote noteIns, float deltaX) {
		NoteType note = notes[noteIns.count];
		note.starttime = note.starttime + (deltaX / itemSize.x) * TimeCoefficient;
		note.endtime = note.endtime + (deltaX / itemSize.x) * TimeCoefficient;
		//EndIndex may have no use at all

		float time = (float)note.starttime;
		String minutes = Mathf.Floor(time / 1000 / 60).ToString("0");
		String seconds = Mathf.Floor(time / 1000 % 60).ToString("00");
		String microSeconds = Mathf.Floor(time % 1000).ToString("000");
		noteIns.text.text = String.Format("{0,0}:{1,0}:{2,0}", minutes, seconds, microSeconds);
	}

	#endregion

	#region fileControl

	public virtual void SaveToJson () {
		//sort first
		sortNotes = notes;
		NoteType temp;
		for (int i = 0; i < sortNotes.Count - 1; i++)
			for (int j = i + 1; j < sortNotes.Count; j++) {
				if (sortNotes[i].starttime > sortNotes[j].starttime) {
					temp = sortNotes[i];
					sortNotes[i] = sortNotes[j];
					sortNotes[j] = temp;
				}
			}
			
		//parallel

		if ((sortNotes[0].starttime.Equals(sortNotes[1].starttime)))
			sortNotes[1].parallel = true;
		else
			sortNotes[1].parallel = false;

		if ((sortNotes[sortNotes.Count - 1].starttime.Equals(sortNotes[sortNotes.Count - 2].starttime)))
			sortNotes[sortNotes.Count - 1].parallel = true;
		else
			sortNotes[sortNotes.Count - 1].parallel = false;
		
		for (int i = 1; i < sortNotes.Count - 1; i++) {
			if ((sortNotes[i].starttime.Equals(sortNotes[i - 1].starttime)) ||
			    (sortNotes[i].starttime.Equals(sortNotes[i + 1].starttime)))
				sortNotes[i].parallel = true;
			else
				sortNotes[i].parallel = false;

		}
		/*
		 * Sample
		 * {
			"starttime": 1461.5889570552145,
			"endtime": 1461.5889570552145,
			"longnote": false,
			"parallel": true,
			"lane": 0,
			"hold": false
		},*/

		using (writer = new StreamWriter(OutputFile)) {
			writer.WriteLine("{");
			writer.WriteLine("\"speed\":" + 160 + ",");
			writer.WriteLine("\"audiofile\":" + AudioFile + ",");
			writer.WriteLine("\"lane\":[");
			for (int i = 0; i < 9; i++) {
				writer.WriteLine("[");
				bool lanestart = true;
				foreach (NoteType note in sortNotes) {
					if (note.lane == i) {
						if (lanestart)
							lanestart = false;
						else
							writer.WriteLine("},");
						writer.WriteLine("{");
						writer.WriteLine("\"starttime\":" + note.starttime + ",");
						writer.WriteLine("\"endtime\":" + note.endtime + ",");
						writer.WriteLine("\"longnote\":" + (!(note.endtime == note.starttime)).ToString().ToLower() + ",");
						writer.WriteLine("\"parallel\":" + note.parallel.ToString().ToLower() + ",");
						writer.WriteLine("\"hold\": false");
					}
				}
				if (!lanestart)
					writer.WriteLine("}");
				if (i < 8)
					writer.WriteLine("],");

			}
			writer.WriteLine("]]}");
		}
	}

	public virtual void ReadFromJson () {
		
	}

	#endregion
}
