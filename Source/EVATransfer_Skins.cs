#region license
/*The MIT License (MIT)
EVATransfer_Skins - Initialize GUI styles and textures

Copyright (c) 2015 DMagic

KSP Plugin Framework by TriggerAu, 2014: http://forum.kerbalspaceprogram.com/threads/66503-KSP-Plugin-Framework

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion

using EVATransfer.Framework;
using UnityEngine;

namespace EVATransfer
{

	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class EVATransfer_Skins : ET_MBE
	{
		internal static GUISkin kspSkin;
		internal static GUIStyle button;
		internal static GUIStyle activeButton;
		internal static GUIStyle closeButton;
		internal static GUIStyle textureButton;
		internal static GUIStyle box;
		internal static GUIStyle slider;
		internal static GUIStyle label;
		internal static GUIStyle labelBig;
		internal static GUIStyle labelLeft;
		internal static GUIStyle labelSmall;
		internal static GUIStyle labelSlider;

		internal static Texture2D dropDownIcon;
		internal static Texture2D linkIcon;
		internal static Texture2D bracketIcon;
		internal static Texture2D lfIcon;
		internal static Texture2D loxIcon;
		internal static Texture2D monoIcon;
		internal static Texture2D xenonIcon;
		internal static Texture2D ecIcon;
		internal static Texture2D oreIcon;

		protected override void OnGUIOnceOnly()
		{
			initializeTextures();
			initializeSkins();
		}

		internal static void initializeTextures()
		{
			dropDownIcon = GameDatabase.Instance.GetTexture("EVATransfer/Icons/DropDownIcon", false);
			linkIcon = GameDatabase.Instance.GetTexture("EVATransfer/Icons/LinkIcon", false);
			bracketIcon = GameDatabase.Instance.GetTexture("EVATransfer/Icons/BracketIcon", false);
			lfIcon = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/R&D_node_icon_fuelsystems", false);
			loxIcon = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/fuels_oxidizer", false);
			monoIcon = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/fuels_monopropellant", false);
			xenonIcon = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/fuels_xenongas", false);
			ecIcon = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/R&D_node_icon_advelectrics", false);
			oreIcon = GameDatabase.Instance.GetTexture("Squad/PartList/SimpleIcons/fuels_ore", false);
		}

		internal static void initializeSkins()
		{
			kspSkin = ET_SkinsLibrary.CopySkin(ET_SkinsLibrary.DefSkinType.KSP);
			ET_SkinsLibrary.AddSkin("EVA_KSPSkin", kspSkin);

			button = new GUIStyle(ET_SkinsLibrary.DefKSPSkin.button);
			button.fontStyle = FontStyle.Bold;
			button.fontSize = 13;
			button.alignment = TextAnchor.LowerCenter;
			button.padding = new RectOffset(2, 2, 2, 2);

			activeButton = new GUIStyle(button);
			activeButton.normal.background = activeButton.hover.background;
			activeButton.hover.background = button.normal.background;

			closeButton = new GUIStyle(button);
			closeButton.fontSize = 10;

			box = new GUIStyle(ET_SkinsLibrary.DefKSPSkin.box);

			slider = new GUIStyle(ET_SkinsLibrary.DefKSPSkin.horizontalSlider);

			label = new GUIStyle(ET_SkinsLibrary.DefKSPSkin.label);
			label.fontStyle = FontStyle.Bold;
			label.fontSize = 13;
			label.wordWrap = false;
			label.alignment = TextAnchor.MiddleCenter;

			labelBig = new GUIStyle(label);
			labelBig.fontSize = 15;

			labelLeft = new GUIStyle(label);
			labelLeft.alignment = TextAnchor.MiddleLeft;

			labelSmall = new GUIStyle(ET_SkinsLibrary.DefKSPSkin.label);
			labelSmall.fontStyle = FontStyle.Bold;
			labelSmall.fontSize = 11;

			labelSlider = new GUIStyle(ET_SkinsLibrary.DefKSPSkin.label);
			labelSlider.fontSize = 9;
			labelSlider.normal.textColor = Color.white;

			textureButton = new GUIStyle(ET_SkinsLibrary.DefKSPSkin.button);
			textureButton.padding = new RectOffset(1, 1, 1, 1);
			textureButton.normal.background = label.normal.background;

			ET_SkinsLibrary.List["EVA_KSPSkin"].button = new GUIStyle(button);
			ET_SkinsLibrary.List["EVA_KSPSkin"].horizontalSlider = new GUIStyle(slider);
			ET_SkinsLibrary.List["EVA_KSPSkin"].label = new GUIStyle(label);
			ET_SkinsLibrary.List["EVA_KSPSkin"].box = new GUIStyle(box);

			ET_SkinsLibrary.AddStyle("EVA_KSPSkin", button);
			ET_SkinsLibrary.AddStyle("EVA_KSPSkin", slider);
			ET_SkinsLibrary.AddStyle("EVA_KSPSkin", label);
			ET_SkinsLibrary.AddStyle("EVA_KSPSkin", box);
		}
	}
}
