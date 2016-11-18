//using System;
using System.Collections.Generic;
//using System.Linq;
using UnityEngine;
using EVATransfer.Framework;

namespace EVATransfer
{
	public class LFLOXTransferGroup : TransferGroup
	{
		private float ratio;
		private TransferableResource loxResource;
		private string loxName;
		private double loxVesselACurrent, loxVesselAMax;
		private double loxVesselBCurrent, loxVesselBMax;
		private double loxTransferAmount;
		private double loxTransferAmountStart;

		public LFLOXTransferGroup(TransferableResource lf, TransferableResource lox, float LOXLFRatio)
		{
			resource = lf;
			name = resource.Name;
			loxResource = lox;
			loxName = loxResource.Name;
			ratio = LOXLFRatio;
		}

		protected override void drawIcon(Rect r)
		{
			Color old = GUI.color;

			GUI.color = resource.IconColor;
			GUI.DrawTexture(r, resource.Icon);

			r.y += 36;

			GUI.color = loxResource.IconColor;
			GUI.DrawTexture(r, loxResource.Icon);

			GUI.color = old;
		}

		protected override void drawTransferAmount(Rect r, bool t)
		{
			r.y += 7;

			base.drawTransferAmount(r, t);

			r.y += 44;

			string loxLabel = "";
			if (transferScale > 1)
			{
				double amount = 0;
				if (t)
					amount = loxTransferAmount;
				else
				{
					amount = (transferScale / 100) * loxVesselACurrent;

					if (amount > loxVesselACurrent)
						amount = loxVesselACurrent;

					double limit = loxVesselBMax - loxVesselBCurrent;

					if (amount > limit)
						amount = limit;
				}

				loxLabel = getDisplayString(amount) + " >";
			}
			else if (transferScale < -1)
			{
				double amount = 0;
				if (t)
					amount = loxTransferAmount;
				else
				{
					amount = -1 * (transferScale / 100) * loxVesselBCurrent;

					if (amount > loxVesselBCurrent)
						amount = loxVesselBCurrent;

					double limit = loxVesselAMax - loxVesselACurrent;

					if (amount > limit)
						amount = limit;
				}

				loxLabel = "< " + getDisplayString(amount);
			}
			else
				return;

			GUI.Label(r, loxLabel);
		}

		protected override void drawSlider(Rect r, bool t, bool b)
		{
			r.y += 7;

			base.drawSlider(r, t, b);
		}

		protected override void drawValues(Rect r)
		{
			r.y -= 10;

			base.drawValues(r);

			r.y += 44;

			GUI.Label(r, getDisplayString(loxVesselACurrent) + " / " + getDisplayString(loxVesselAMax));

			r.x += 135;

			GUI.Label(r, getDisplayString(loxVesselBCurrent) + " / " + getDisplayString(loxVesselBMax));
		}

		protected override bool drawCloseButton(Rect r, bool t, bool b)
		{
			r.y -= 6;

			return base.drawCloseButton(r, t, b);
		}

		protected override void toggleResourceSetup()
		{
			if (transferScale > 1)
			{
				transferAmount = (transferScale / 100) * vesselACurrent;

				if (transferAmount > (vesselBMax - vesselBCurrent))
					transferAmount = vesselBMax - vesselBCurrent;

				loxTransferAmount = (transferScale / 100) * loxVesselACurrent;

				if (loxTransferAmount > (loxVesselBMax - loxVesselBCurrent))
					loxTransferAmount = loxVesselBMax - loxVesselBCurrent;
			}
			else if (transferScale < -1)
			{
				transferAmount = ((transferScale * -1) / 100) * vesselBCurrent;

				if (transferAmount > (vesselAMax - vesselACurrent))
					transferAmount = vesselAMax - vesselACurrent;

				loxTransferAmount = ((transferScale * -1) / 100) * loxVesselBCurrent;

				if (loxTransferAmount > (loxVesselAMax - loxVesselACurrent))
					loxTransferAmount = loxVesselAMax - loxVesselACurrent;
			}
			else
			{
				transferAmount = 0;
				loxTransferAmount = 0;
			}

			transferStartAmount = transferAmount;
			loxTransferAmountStart = loxTransferAmount;
		}

		protected override void transferFromTo(List<Part> fromParts, List<Part> toParts, float time)
		{
			double resourceSubtract = transferStartAmount * time;
			if (resourceSubtract > transferAmount)
				resourceSubtract = transferAmount;
			double partSubtract = resourceSubtract;

			double loxSubract = loxTransferAmountStart * time * ratio;
			if (loxSubract > loxTransferAmount)
				loxSubract = loxTransferAmount;
			double loxPartSubtract = loxSubract;

			int l = fromParts.Count;

			for (int i = 0; i < l; i++)
			{
				Part p = fromParts[i];

				if (p == null)
					continue;

				PartResource r = p.Resources[name];
				PartResource lox = p.Resources[loxName];

				if (r == null)
					continue;

				if (lox == null)
					continue;

				if (loxTransferAmount > 0)
				{
					if (lox.maxAmount > 0.1)
					{
						if (lox.amount > 0.001)
						{
							if (loxPartSubtract > lox.amount)
							{
								loxTransferAmount -= lox.amount;
								loxPartSubtract -= lox.amount;
								lox.amount -= lox.amount;
							}
							else
							{
								loxTransferAmount -= loxPartSubtract;
								lox.amount -= loxPartSubtract;
								loxPartSubtract = 0;
							}
						}
					}
				}

				if (transferAmount > 0)
				{
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
				}

				if (loxPartSubtract <= 0 && partSubtract <= 0)
					break;
			}

			double partAdd = resourceSubtract;
			double loxPartAdd = loxSubract;

			l = toParts.Count;

			for (int i = 0; i < l; i++)
			{
				Part p = toParts[i];

				if (p == null)
					continue;

				PartResource r = p.Resources[name];
				PartResource lox = p.Resources[loxName];

				if (r == null)
					continue;

				if (lox == null)
					continue;

				if (loxTransferAmount > 0 - loxPartAdd)
				{
					if (lox.maxAmount > 0.1)
					{
						if (lox.amount < lox.maxAmount - 0.001)
						{
							if (loxPartAdd > lox.maxAmount - lox.amount)
							{
								loxPartAdd -= (lox.maxAmount - lox.amount);
								lox.amount += (lox.maxAmount - lox.amount);
							}
							else
							{
								lox.amount += loxPartAdd;
								loxPartAdd = 0;
							}
						}
					}
				}

				if (transferAmount > 0 - partAdd)
				{
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
				}

				if (loxPartAdd <= 0 && partAdd <= 0)
					break;
			}
		}

		protected override bool checkPartForResources(Part p, bool ignore)
		{
			if (!p.Resources.Contains(loxName))
				return false;

			if (ignore && !p.Resources[loxName]._flowState)
				return false;

			if (p.Resources[loxName].maxAmount < 0.6)
				return false;

			return base.checkPartForResources(p, ignore);
		}

		protected override void resetValues()
		{
			loxVesselACurrent = 0;
			loxVesselAMax = 0;
			loxVesselBCurrent = 0;
			loxVesselBMax = 0;
			base.resetValues();
		}

		protected override void resetTransferValues()
		{
			loxTransferAmount = 0;
			loxTransferAmountStart = 0;

			base.resetTransferValues();
		}

		protected override void checkPartAmount(Part p, bool a)
		{
			if (!p.Resources.Contains(name))
				return;

			if (!p.Resources.Contains(loxName))
				return;

			PartResource pr = p.Resources[name];
			PartResource lox = p.Resources[loxName];

			if (a)
			{
				vesselACurrent += pr.amount;
				vesselAMax += pr.maxAmount;

				loxVesselACurrent += lox.amount;
				loxVesselAMax += lox.maxAmount;
			}
			else
			{
				vesselBCurrent += pr.amount;
				vesselBMax += pr.maxAmount;

				loxVesselBCurrent += lox.amount;
				loxVesselBMax += lox.maxAmount;
			}
		}

	}
}
