using System;
using System.Collections.Generic;
using System.Linq;
using EVATransfer.Framework;
using UnityEngine;

namespace EVATransfer
{
	public class TransferGroup
	{
		private bool onBoard;
		private Vessel vesselA, vesselB;

		protected TransferableResource resource;
		protected string name;
		protected double vesselACurrent, vesselBCurrent;
		protected double vesselAMax, vesselBMax;
		protected double transferScale;
		protected double transferAmount;
		protected double transferStartAmount;

		private Dictionary<string, List<Part>> vesselAParts = new Dictionary<string, List<Part>>();
		private Dictionary<string, List<Part>> vesselBParts = new Dictionary<string, List<Part>>();

		public TransferGroup(TransferableResource r)
		{
			resource = r;
			name = resource.Name;
		}

		public TransferGroup()
		{

		}

		public TransferableResource Resource
		{
			get { return resource; }
		}

		public double TransferScale
		{
			get { return transferScale; }
		}

		public bool OnBoard
		{
			get { return onBoard; }
		}

		#region Public methods called by the transfer window
		public void updateVessels(Vessel a, Vessel b)
		{
			vesselA = a;
			vesselB = b;

			if (vesselA == null || vesselB == null)
				onBoard = false;

			if (vesselA.Parts.Any(p => p.Resources.Contains(name)) && vesselB.Parts.Any(p => p.Resources.Contains(name)))
				onBoard = true;
			else
				onBoard = false;
		}

		public void drawResourceGroup(float y, bool transfering, bool behind)
		{
			drawIcon(new Rect(-2, y, 36, 36));

			drawTransferAmount(new Rect(115, y + 3, 80, 20), transfering);

			drawSlider(new Rect(32, y + 22, 246, 30), transfering, behind);

			drawValues(new Rect(40, y + 20, 100, 20));
		}

		public bool drawCloseGroup(float x, float y, bool transfering, bool behind)
		{
			return drawCloseButton(new Rect(x, y, 18, 18), transfering, behind);
		}

		public void transferResources(float time, float speed)
		{
			transferStep(time, speed);
		}

		public void toggleTransfer()
		{
			toggleResourceSetup();
		}

		public void updateValues(bool values, bool parts, int mode)
		{
			if (parts)
				updatePartList(mode);

			if (values)
				updateResources();
		}

		public void finishTransfer()
		{
			resetTransferValues();
		}
		#endregion

		#region Updating part lists and resource amounts
		private void updateResources()
		{
			resetValues();

			if (vesselAParts.ContainsKey(name))
			{
				foreach (Part p in vesselAParts[name])
				{
					checkPartAmount(p, true);
				}
			}

			if (vesselBParts.ContainsKey(name))
			{
				foreach (Part p in vesselBParts[name])
				{
					checkPartAmount(p, false);
				}
			}
		}

		private void updatePartList(int fillMode)
		{
			if (vesselA == null || vesselB == null)
				return;

			vesselAParts = new Dictionary<string, List<Part>>();
			vesselBParts = new Dictionary<string, List<Part>>();

			List<Part> sourceList = new List<Part>();

			foreach (Part p in vesselA.Parts)
			{
				if (!checkPartForResources(p))
					continue;

				sourceList.Add(p);
			}

			sortParts(sourceList, fillMode);

			if (!vesselAParts.ContainsKey(name))
				vesselAParts.Add(name, sourceList);

			List<Part> targetList = new List<Part>();

			foreach (Part p in vesselB.Parts)
			{
				if (!checkPartForResources(p))
					continue;

				targetList.Add(p);
			}

			sortParts(targetList, fillMode);

			if (!vesselBParts.ContainsKey(name))
				vesselBParts.Add(name, targetList);
		}

		protected virtual bool checkPartForResources(Part p)
		{
			if (!p.Resources.Contains(name))
				return false;

			if (p.Resources[name].maxAmount < 0.6)
				return false;

			return true;
		}

		protected virtual void resetValues()
		{
			vesselACurrent = 0;
			vesselAMax = 0;
			vesselBCurrent = 0;
			vesselBMax = 0;
		}

		protected virtual void resetTransferValues()
		{
			transferScale = 0;
			transferAmount = 0;
			transferStartAmount = 0;
		}

		protected virtual void checkPartAmount(Part p, bool a)
		{
			if (!p.Resources.Contains(name))
				return;

			PartResource pr = p.Resources[name];

			if (a)
			{
				vesselACurrent += pr.amount;
				vesselAMax += pr.maxAmount;
			}
			else
			{
				vesselBCurrent += pr.amount;
				vesselBMax += pr.maxAmount;
			}
		}

		private void sortParts(List<Part> list, int i)
		{
			switch (i)
			{
				case 0:
					{
						list.Sort((a, b) => a.Resources[name].maxAmount.CompareTo(b.Resources[name].maxAmount));
						break;
					}
				case 1:
					{
						list.Sort((a, b) => a.Resources[name].maxAmount.CompareTo(b.Resources[name].maxAmount));
						list.Reverse();
						break;
					}
				default:
					{
						break;
					}
			}
		}
		#endregion

		#region Resource Transfer Methods
		protected virtual void toggleResourceSetup()
		{
			if (transferScale > 1)
			{
				transferAmount = (transferScale / 100) * vesselACurrent;

				if (transferAmount > (vesselBMax - vesselBCurrent))
					transferAmount = vesselBMax - vesselBCurrent;
			}
			else if (transferScale < -1)
			{
				transferAmount = ((transferScale * -1) / 100) * vesselBCurrent;

				if (transferAmount > (vesselAMax - vesselACurrent))
					transferAmount = vesselAMax - vesselACurrent;
			}
			else
				transferAmount = 0;

			transferStartAmount = transferAmount;
		}

		private void transferStep(float time, float speed)
		{
			if (vesselA == null || vesselB == null)
				return;

			if (!vesselAParts.ContainsKey(name) || !vesselBParts.ContainsKey(name))
				return;

			if (transferScale <= 0.1 && transferScale >= -0.1)
				return;

			if (transferAmount <= 0.001)
				return;

			float timeSlice = time / (speed * (float)(Math.Abs(transferScale) / 100));

			if (transferScale < -0.1)
			{
				if (vesselBCurrent <= 0.001)
					return;

				transferFromTo(vesselBParts[name], vesselAParts[name], timeSlice);
			}
			else
			{
				if (vesselACurrent <= 0.001)
					return;

				transferFromTo(vesselAParts[name], vesselBParts[name], timeSlice);
			}
		}

		protected virtual void transferFromTo(List<Part> fromParts, List<Part> toParts, float time)
		{
			double resourceSubtract = transferStartAmount * time;
			double partSubtract = resourceSubtract;

			for (int i = 0; i < fromParts.Count; i++)
			{
				Part p = fromParts[i];

				if (p == null)
					continue;

				PartResource r = p.Resources[name];

				if (r == null)
					continue;

				if (r.maxAmount > 0.1)
				{
					if (r.amount > 0.001)
					{
						if (partSubtract > r.amount)
						{
							transferAmount -= r.amount;
							partSubtract -= r.amount;
							r.amount -= r.amount;
						}
						else
						{
							r.amount -= partSubtract;
							transferAmount -= partSubtract;
							partSubtract = 0;
						}
					}
				}

				if (partSubtract <= 0)
					break;
			}

			double partAdd = resourceSubtract;

			for (int i = 0; i < toParts.Count; i++)
			{
				Part p = toParts[i];

				if (p == null)
					continue;

				PartResource r = p.Resources[name];

				if (r == null)
					continue;

				if (r.maxAmount > 0.1)
				{
					if (r.amount < r.maxAmount - 0.001)
					{
						if (partAdd > r.maxAmount - r.amount)
						{
							partAdd -= (r.maxAmount - r.amount);
							r.amount += (r.maxAmount - r.amount);
						}
						else
						{
							r.amount += partAdd;
							partAdd = 0;
						}
					}
				}

				if (partAdd <= 0)
					break;
			}
		}
		#endregion

		#region GUI Drawing Methods
		protected virtual void drawIcon(Rect r)
		{
			Color old = GUI.color;

			if (resource.Primary)
			{
				GUI.color = resource.IconColor;
				GUI.DrawTexture(r, resource.Icon);
				GUI.color = old;
			}
			else
			{
				r.x = 4;
				r.y += 2;
				r.width = 100;
				r.height = 18;
				GUI.Label(r, name, EVATransfer_Startup.labelLeft);
			}
		}

		protected virtual void drawTransferAmount(Rect r, bool t)
		{
			string label = "";
			if (transferScale > 1)
			{
				double amount = 0;
				if (t)
					amount = transferAmount;
				else
				{
					amount = (transferScale / 100) * vesselACurrent;

					if (amount > vesselACurrent)
						amount = vesselACurrent;

					double limit = vesselBMax - vesselBCurrent;

					if (amount > limit)
						amount = limit;
				}

				label = getDisplayString(amount) + " >";
			}
			else if (transferScale < -1)
			{
				double amount = 0;
				if (t)
					amount = transferAmount;
				else
				{
					amount = -1 * (transferScale / 100) * vesselBCurrent;

					if (amount > vesselBCurrent)
						amount = vesselBCurrent;

					double limit = vesselAMax - vesselACurrent;

					if (amount > limit)
						amount = limit;
				}

				label = "< " + getDisplayString(amount);
			}
			else
				return;

			GUI.Label(r, label);
		}

		protected virtual void drawSlider(Rect r, bool t, bool b)
		{
			if (b)
				GUI.Label(r, "", EVATransfer_Startup.slider);
			else if (t)
			{
				float slider = 0;
				if (transferStartAmount > 0)
					slider = transferScale == 0 ? 0 : (float)(transferScale * (transferAmount / transferStartAmount));
				GUI.HorizontalSlider(r, slider, -100, 100);
			}
			else
			{
				transferScale = GUI.HorizontalSlider(r, (float)transferScale, -100, 100);
				transferScale = Math.Round(transferScale / 5) * 5;
			}

			drawSliderLabel(r);
		}

		private void drawSliderLabel(Rect r)
		{
			r.x = 38;
			r.y += 5;
			r.width = 18;
			r.height = 10;
			GUI.Label(r, "|", EVATransfer_Startup.labelSlider);
			r.x += 61;
			GUI.Label(r, "|", EVATransfer_Startup.labelSlider);
			r.x += 54;
			GUI.Label(r, "|", EVATransfer_Startup.labelSlider);
			r.x += 56;
			GUI.Label(r, "|", EVATransfer_Startup.labelSlider);
			r.x += 58;
			GUI.Label(r, "|", EVATransfer_Startup.labelSlider);

			r.x = 33;
			r.y += 8;
			r.width = 30;
			GUI.Label(r, "100%", EVATransfer_Startup.labelSlider);
			r.x += 63;
			GUI.Label(r, "50%", EVATransfer_Startup.labelSlider);
			r.x += 56;
			GUI.Label(r, "0%", EVATransfer_Startup.labelSlider);
			r.x += 54;
			GUI.Label(r, "50%", EVATransfer_Startup.labelSlider);
			r.x += 56;
			GUI.Label(r, "100%", EVATransfer_Startup.labelSlider);
		}

		protected virtual void drawValues(Rect r)
		{
			GUI.Label(r, getDisplayString(vesselACurrent) + " / " + getDisplayString(vesselAMax));

			r.x += 135;

			GUI.Label(r, getDisplayString(vesselBCurrent) + " / " + getDisplayString(vesselBMax));
		}

		protected virtual bool drawCloseButton(Rect r, bool t, bool b)
		{
			if (b)
				GUI.Label(r, "✖", EVATransfer_Startup.closeButton);
			else
			{
				if (GUI.Button(r, "✖", EVATransfer_Startup.closeButton))
				{
					if (!t)
						return true;
				}
			}

			return false;
		}

		protected string getDisplayString(double value)
		{
			string s = "";

			if (value <= 0)
				s = "0";
			else if (value > 0 && value < 100)
				s = value.ToString("F2");
			else if (value >= 100 && value < 1000)
				s = value.ToString("F1");
			else if (value >= 1000 && value < 10000)
				s = (value /= 1000).ToString("F1") + "k";
			else
				s = (value /= 1000).ToString("F0") + "k";

			return s;
		}
		#endregion

	}
}
