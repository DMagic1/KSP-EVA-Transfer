using System;
using System.Collections.Generic;
using System.Linq;
using EVATransfer.Framework;
using UnityEngine;

namespace EVATransfer
{
	public class TransferableResource
	{
		private PartResourceDefinition resource;
		private string name;
		private bool primary;
		private Texture2D icon;
		private Color color;
		private ResourceTransferMode mode;
		
		public TransferableResource(PartResourceDefinition r)
		{
			resource = r;
			name = resource.name;
			mode = resource.resourceTransferMode;

			switch (name)
			{
				case "LiquidFuel":
					icon = EVATransfer_Startup.lfIcon;
					color = XKCDColors.LightRed;
					break;
				case "Oxidizer":
					icon = EVATransfer_Startup.loxIcon;
					color = XKCDColors.OrangeyYellow;
					break;
				case "MonoPropellant":
					icon = EVATransfer_Startup.monoIcon;
					color = Color.white;
					break;
				case "XenonGas":
					icon = EVATransfer_Startup.xenonIcon;
					color = XKCDColors.AquaBlue;
					break;
				case "ElectricCharge":
					icon = EVATransfer_Startup.ecIcon;
					color = XKCDColors.SunnyYellow;
					break;
				case "Ore":
					icon = EVATransfer_Startup.oreIcon;
					color = XKCDColors.Purple_Pink;
					break;
				default:
					icon = null;
					color = Color.white;
					break;
			}

			if (icon != null)
				primary = true;
		}

		public string Name
		{
			get { return name; }
		}

		public bool Primary
		{
			get { return primary; }
		}

		public ResourceTransferMode Mode
		{
			get { return mode; }
		}

		public Texture2D Icon
		{
			get { return icon; }
		}

		public Color IconColor
		{
			get { return color; }
		}
	}
}
