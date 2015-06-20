#region license
/*The MIT License (MIT)
EVATransfer_Window - Handles the window UI and resource transfer logic

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

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using EVATransfer.Framework;
using UnityEngine;

namespace EVATransfer
{

	public class EVATransfer_Window : ET_MBW
	{
		private bool transferLFLOX;
		private bool active;
		private bool LFLOXTransferLink;
		private bool transferActive;
		private bool dropDown;
		private Rect ddRect;
		private Vector2 dropDownScroll;
		private string version;

		private Texture2D icon;

		private Vessel sourceVessel;
		private Vessel targetVessel;

		private ModuleEVATransfer evaModule;

		private float transferComplete;

		private double[] sourceValues;
		private double[] sourceValuesMax;
		private double[] targetValues;
		private double[] targetValuesMax;
		private double[] transferScale;
		private double[] transferAmount;
		private double[] transferStartAmount;
		private double[] oxidizerAmount = new double[4];
		private double oxidizerTransferAmount = 0;
		private double oxidizerStartAmount = 0;

		private Dictionary<string, PartResourceDefinition> allowedOtherResources = new Dictionary<string, PartResourceDefinition>();
		private Dictionary<string, PartResourceDefinition> allowedStockResources = new Dictionary<string,PartResourceDefinition>();

		private List<PartResourceDefinition> selectedResources = new List<PartResourceDefinition>();

		private Dictionary<string, List<Part>> sourceVesselParts = new Dictionary<string, List<Part>>();
		private Dictionary<string, List<Part>> targetVesselParts = new Dictionary<string, List<Part>>();

		private static Rect sessionRect = new Rect(100, 100, 300, 120);

		protected override void Awake()
		{
			WindowRect = new Rect(100, 100, 300, 120);
			WindowOptions = new GUILayoutOption[3] { GUILayout.Width(300), GUILayout.Height(120), GUILayout.MaxHeight(120) };
			WindowCaption = "EVA Transfer";

			Visible = false;
			DragEnabled = true;
			ClampToScreen = true;
			ClampToScreenOffset = new RectOffset(-100, -100, -100, -100);
			TooltipMouseOffset = new Vector2d(-10, -25);
			TooltipsEnabled = true;

			ET_SkinsLibrary.SetCurrent("EVA_KSPSkin");

			Assembly assembly = AssemblyLoader.loadedAssemblies.GetByAssembly(Assembly.GetExecutingAssembly()).assembly;
			var ainfoV = Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
			switch (ainfoV == null)
			{
				case true: version = ""; break;
				default: version = ainfoV.InformationalVersion; break;
			}
		}

		protected override void Start()
		{
			WindowRect = sessionRect;
			GameEvents.onVesselChange.Add(vesselChange);
		}

		public void setup(bool a, bool b, bool c, bool d, bool e, bool f, bool g, ModuleEVATransfer mod)
		{
			evaModule = mod;

			if (evaModule == null)
				return;

			sourceValues = new double[evaModule.MaxTransfers];
			sourceValuesMax = new double[evaModule.MaxTransfers];
			targetValues = new double[evaModule.MaxTransfers];
			targetValuesMax = new double[evaModule.MaxTransfers];
			transferScale = new double[evaModule.MaxTransfers];
			transferAmount = new double[evaModule.MaxTransfers];
			transferStartAmount = new double[evaModule.maxTransfers];

			TooltipsEnabled = evaModule.tooltips;

			transferLFLOX = a && b;

			if (a)
				addResource("LiquidFuel");
			if (b)
				addResource("Oxidizer");
			if (c)
				addResource("MonoPropellant");
			if (d)
				addResource("XenonGas");
			if (e)
				addResource("ElectricCharge");
			if (f)
				addResource("Ore");
			if (g)
				addAllResources();
		}

		public void activateVessels(Vessel a, Vessel b)
		{
			sourceVessel = a;
			targetVessel = b;

			refreshPartDatabase();

			updateResources();

			active = true;
		}

		public void severConnection()
		{
			active = false;
		}

		private void addResource(string name)
		{
			if (allowedStockResources.ContainsKey(name))
				return;

			if (PartResourceLibrary.Instance == null)
				return;

			PartResourceDefinition r = PartResourceLibrary.Instance.GetDefinition(name);

			if (r == null)
				return;

			allowedStockResources.Add(name, r);
		}

		private void addAllResources()
		{
			if (PartResourceLibrary.Instance == null)
				return;

			foreach (PartResourceDefinition p in PartResourceLibrary.Instance.resourceDefinitions)
			{
				switch (p.name)
				{
					case "LiquidFuel":
					case "Oxidizer":
					case "MonoPropellant":
					case "XenonGas":
					case "ElectricCharge":
					case "Ore":
						continue;
				}

				if (p.resourceTransferMode == ResourceTransferMode.NONE)
					continue;

				if (allowedOtherResources.ContainsKey(p.name))
					continue;

				if (p == null)
					continue;

				allowedOtherResources.Add(p.name, p);
			}
		}

		protected override void OnDestroy()
		{
			GameEvents.onVesselChange.Remove(vesselChange);
		}

		protected override void FixedUpdate()
		{
			if (!active)
				return;

			if (!Visible)
				return;

			if (!transferActive)
				return;

			if (selectedResources.Count <= 0)
				return;

			if (sourceVessel == null || targetVessel == null)
				return;

			transferStep();

			updateResources();
		}

		protected override void DrawWindow(int id)
		{
			versionLabel(id);
			closeBox(id);

			if (!active)
			{
				GUILayout.Label("Connection Severed; Transfer Terminated");
				dropDown = false;
				return;
			}

			transferButtons(id);
			resourceLabels(id);

			drawDropDown(id);
		}

		protected override void DrawWindowPost(int id)
		{
			if (dropDown && Event.current.type == EventType.mouseDown && !ddRect.Contains(Event.current.mousePosition))
			{
				WindowOptions = new GUILayoutOption[3] { GUILayout.Width(300), GUILayout.Height(120), GUILayout.MaxHeight(120) };
				dropDown = false;
			}

			sessionRect = WindowRect;
		}

		private void versionLabel(int id)
		{
			Rect r = new Rect(4, 0, 50, 18);
			GUI.Label(r, version, EVATransfer_Skins.labelSmall);
		}

		private void closeBox(int id)
		{
			Rect r = new Rect(WindowRect.width - 30, 1, 22, 22);
			if (GUI.Button(r, "✖"))
			{
				evaModule.Events["openEVAFuelTransfer"].active = true;
				evaModule.Events["closeEVAFuelTransfer"].active = false;
				StopRepeatingWorker();
				Visible = false;
			}
		}

		private void transferButtons(int id)
		{
			GUILayout.BeginHorizontal();

			foreach (PartResourceDefinition p in allowedStockResources.Values)
			{
				drawIcon(p, true, new Rect());
			}

			if (allowedOtherResources.Count > 0)
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button(EVATransfer_Skins.dropDownIcon, GUILayout.Width(36), GUILayout.Height(36)))
				{
					if (!transferActive)
					{
						dropDown = !dropDown;
						if (!dropDown)
							WindowOptions = new GUILayoutOption[3] { GUILayout.Width(300), GUILayout.Height(120), GUILayout.MaxHeight(120) };
					}
				}
			}

			if (transferLFLOX)
			{
				Rect r = new Rect(36, 9, 20, 20);
				if (GUI.Button(r, new GUIContent(EVATransfer_Skins.linkIcon, "Link LF and LOX"), EVATransfer_Skins.textureButton))
				{
					if (!transferActive)
						toggleLFLOXTransfer();
				}

				if (LFLOXTransferLink)
				{
					r.x -= 6;
					r.y += 8;
					r.width = 32;
					GUI.DrawTexture(r, EVATransfer_Skins.bracketIcon);
				}
			}

			GUILayout.EndHorizontal();
		}

		private void resourceLabels(int id)
		{
			if (selectedResources.Count <= 0)
			{
				return;
			}

			GUILayout.Space(30);

			Rect r = new Rect(40, 78, 80, 20);

			GUI.Label(r, sourceVessel.vesselName, EVATransfer_Skins.labelBig);

			r.x += 130;

			GUI.Label(r, targetVessel.vesselName, EVATransfer_Skins.labelBig);

			r.y += 17;

			for (int i = 0; i < selectedResources.Count; i++)
			{
				if (LFLOXTransferLink && i == 0)
				{
					GUILayout.Space(80);

					drawTransferAmount(transferScale[i], new Rect(115, r.y + 10, 80, 20), i);

					drawTransferAmount(transferScale[i], new Rect(115, r.y + 54, 80, 20), i, true);

					drawLabel(new Rect(35, r.y + 10, 90, 20), getDisplayString(sourceValues[i]) + " / " + getDisplayString(sourceValuesMax[i]));

					drawLabel(new Rect(180, r.y + 10, 90, 20), getDisplayString(targetValues[i]) + " / " + getDisplayString(targetValuesMax[i]));

					drawLabel(new Rect(35, r.y + 54, 90, 20), getDisplayString(oxidizerAmount[0]) + " / " + getDisplayString(oxidizerAmount[1]));

					drawLabel(new Rect(180, r.y + 54, 90, 20), getDisplayString(oxidizerAmount[2]) + " / " + getDisplayString(oxidizerAmount[3]));

					drawIcon(selectedResources[i], false, new Rect(-2, r.y, 36, 36));

					drawIcon(null, false, new Rect(-2, r.y + 36, 36, 36), true);

					transferScale[i] = drawSlider(ref transferScale[i], new Rect(32, r.y + 29, 246, 30), i);

					if (dropDown)
						GUI.Label(new Rect(WindowRect.width - 21, r.y + 18, 18, 18), "✖", EVATransfer_Skins.closeButton);
					else
					{
						if (GUI.Button(new Rect(WindowRect.width - 21, r.y + 18, 18, 18), "✖", EVATransfer_Skins.closeButton))
						{
							if (!transferActive)
								toggleLFLOXTransfer();
						}
					}

					r.y += 28;
				}
				else
				{
					GUILayout.Space(52);

					drawTransferAmount(transferScale[i], new Rect(115, r.y + 3, 80, 20), i);

					drawIcon(selectedResources[i], false, new Rect(-2, r.y, 36, 36));

					transferScale[i] = drawSlider(ref transferScale[i], new Rect(32, r.y + 22, 246, 30), i);

					drawLabel(new Rect(40, r.y + 20, 100, 20), getDisplayString(sourceValues[i]) + " / " + getDisplayString(sourceValuesMax[i]));

					drawLabel(new Rect(170, r.y + 20, 100, 20), getDisplayString(targetValues[i]) + " / " + getDisplayString(targetValuesMax[i]));

					if (dropDown)
						GUI.Label(new Rect(WindowRect.width - 21, r.y + 12, 18, 18), "✖", EVATransfer_Skins.closeButton);
					else
					{
						if (GUI.Button(new Rect(WindowRect.width - 21, r.y + 12, 18, 18), "✖", EVATransfer_Skins.closeButton))
						{
							if (!transferActive)
								toggleSelectedResource(selectedResources[i]);
						}
					}
				}

				r.y += 50;
			}

			if (transferScale.Any(a => Math.Abs(a) > 0.1))
			{
				GUILayout.Space(25);

				r.x = 10;
				r.y += 10;
				r.width = WindowRect.width - 20;
				r.height = 20;

				if (GUI.Button(r, transferActive ? "Stop Transfer" : "Begin Transfer"))
					toggleTransfer();
			}
		}

		private void drawDropDown(int id)
		{
			if (!dropDown)
				return;

			if (WindowRect.height < 350)
			{
				WindowOptions = new GUILayoutOption[3] { GUILayout.Width(300), GUILayout.Height(120), GUILayout.MaxHeight(350) };
			}

			WindowRect.height = 350;

			ddRect = new Rect(20, 65, 240, 260);
			GUI.Box(ddRect, "");

			Rect view = new Rect(0, 0, 220, allowedOtherResources.Count * 25);

			dropDownScroll = GUI.BeginScrollView(ddRect, dropDownScroll, view);

			Rect r = new Rect(2, 2, 220, 23);

			for (int i = 0; i < allowedOtherResources.Count; i++)
			{
				if (r.yMin >= (dropDownScroll.y - 25) && r.yMax <= (dropDownScroll.y + 285))
				{
					PartResourceDefinition p = allowedOtherResources.ElementAt(i).Value;
					if (GUI.Button(r, p.name, selectedResources.Contains(p) ? EVATransfer_Skins.activeButton : EVATransfer_Skins.button))
					{
						toggleSelectedResource(p);
						WindowOptions = new GUILayoutOption[3] { GUILayout.Width(300), GUILayout.Height(120), GUILayout.MaxHeight(120) };
						dropDown = false;
					}
				}
				r.y += 25;
			}

			GUI.EndScrollView();
		}

		private void drawTransferAmount(double scale, Rect pos, int index, bool oxi = false)
		{
			string label = "";
			if (scale > 1)
			{
				double amount = 0;
				if (transferActive)
				{
					amount = oxi? oxidizerTransferAmount : transferAmount[index];
				}
				else
				{
					amount = (scale / 100) * sourceValues[index];

					if (oxi)
						amount *= (11d / 9d);

					if (amount > (oxi ? oxidizerAmount[0] : sourceValues[index]))
						amount = (oxi ? oxidizerAmount[0] : sourceValues[index]);

					double limit = (oxi ? (oxidizerAmount[3] - oxidizerAmount[2]) : (targetValuesMax[index] - targetValues[index]));

					if (amount > limit)
						amount = limit;
				}

				label = getDisplayString(amount) + " >";
			}
			else if (scale < -1)
			{
				double amount = 0;
				if (transferActive)
				{
					amount = oxi ? oxidizerTransferAmount : transferAmount[index];
				}
				else
				{
					amount = -1 * (scale / 100) * targetValues[index];

					if (oxi)
						amount *= (11d / 9d);

					if (amount > (oxi ? oxidizerAmount[2] : targetValues[index]))
						amount = (oxi ? oxidizerAmount[2] : targetValues[index]);

					double limit = (oxi ? (oxidizerAmount[1] - oxidizerAmount[0]) : (sourceValuesMax[index] - sourceValues[index]));

					if (amount > limit)
						amount = limit;
				}

				label = "< " + getDisplayString(amount);
			}
			else
				return;

			drawLabel(pos, label);
		}

		private void drawLabel(Rect pos, string text)
		{
			GUI.Label(pos, text);
		}

		private void drawIcon(PartResourceDefinition resource, bool button, Rect pos, bool oxi = false)
		{
			Color c = Color.white;
			Color old = GUI.color;

			if (oxi)
			{
				icon = EVATransfer_Skins.loxIcon;
				c = XKCDColors.YellowishOrange;
			}
			else
			{
				switch (resource.name)
				{
					case "LiquidFuel":
						icon = EVATransfer_Skins.lfIcon;
						c = XKCDColors.LightRed;
						break;
					case "Oxidizer":
						icon = EVATransfer_Skins.loxIcon;
						c = XKCDColors.OrangeyYellow;
						break;
					case "MonoPropellant":
						icon = EVATransfer_Skins.monoIcon;
						c = Color.white;
						break;
					case "XenonGas":
						icon = EVATransfer_Skins.xenonIcon;
						c = XKCDColors.AquaBlue;
						break;
					case "ElectricCharge":
						icon = EVATransfer_Skins.ecIcon;
						c = XKCDColors.SunnyYellow;
						break;
					case "Ore":
						icon = EVATransfer_Skins.oreIcon;
						c = XKCDColors.Purple_Pink;
						break;
					default:
						icon = null;
						break;
				}
			}

			if (button)
			{
				if (icon == null)
				{
					if (GUILayout.Button(resource.name, selectedResources.Contains(resource) ? EVATransfer_Skins.activeButton : EVATransfer_Skins.button, GUILayout.Width(50)))
					{
						if (!transferActive)
							toggleSelectedResource(resource);
					}
				}
				else
				{
					if (GUILayout.Button(new GUIContent("", resource.name), (selectedResources.Contains(resource) || (LFLOXTransferLink && (resource.name == "Oxidizer" || resource.name == "LiquidFuel"))) ? EVATransfer_Skins.activeButton : EVATransfer_Skins.button, GUILayout.Width(36), GUILayout.Height(36)))
					{
						if (!transferActive)
							toggleSelectedResource(resource);
					}

					Rect r = GUILayoutUtility.GetLastRect();
					GUI.color = c;
					GUI.DrawTexture(r, icon);
					GUI.color = old;
				}
			}
			else
			{
				if (icon == null)
				{
					pos.x = 4;
					pos.y += 2;
					pos.width = 100;
					pos.height = 18;
					GUI.Label(pos, resource.name, EVATransfer_Skins.labelLeft);
				}
				else
				{
					GUI.color = c;
					GUI.Label(pos, icon);
					GUI.color = old;
				}
			}
		}

		private double drawSlider(ref double value, Rect pos, int index)
		{
			if (dropDown)
				GUI.Label(pos, "", EVATransfer_Skins.slider);
			else if (transferActive)
			{
				float slider = 0;
				if (transferStartAmount[index] > 0)
					slider = transferScale[index] == 0 ? 0 : (float)(transferScale[index] * (transferAmount[index] / transferStartAmount[index]));
				GUI.HorizontalSlider(pos, slider, -100, 100);
			}
			else
			{
				value = GUI.HorizontalSlider(pos, (float)value, -100, 100);
				value = Math.Round(value / 5) * 5;
			}

			drawSliderLabel(pos);

			return value;
		}

		private void drawSliderLabel(Rect pos)
		{
			pos.x = 38;
			pos.y += 5;
			pos.width = 18;
			pos.height = 10;
			GUI.Label(pos, "|", EVATransfer_Skins.labelSlider);
			pos.x += 61;
			GUI.Label(pos, "|", EVATransfer_Skins.labelSlider);
			pos.x += 54;
			GUI.Label(pos, "|", EVATransfer_Skins.labelSlider);
			pos.x += 56;
			GUI.Label(pos, "|", EVATransfer_Skins.labelSlider);
			pos.x += 58;
			GUI.Label(pos, "|", EVATransfer_Skins.labelSlider);

			pos.x = 33;
			pos.y += 8;
			pos.width = 30;
			GUI.Label(pos, "100%", EVATransfer_Skins.labelSlider);
			pos.x += 63;
			GUI.Label(pos, "50%", EVATransfer_Skins.labelSlider);
			pos.x += 56;
			GUI.Label(pos, "0%", EVATransfer_Skins.labelSlider);
			pos.x += 54;
			GUI.Label(pos, "50%", EVATransfer_Skins.labelSlider);
			pos.x += 56;
			GUI.Label(pos, "100%", EVATransfer_Skins.labelSlider);
		}

		private string getDisplayString(double value)
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

		private void toggleTransfer()
		{
			transferActive = !transferActive;
			dropDown = false;

			if (transferActive)
			{
				refreshPartDatabase();
				updateResources();

				for (int i = 0; i < selectedResources.Count; i++)
				{
					double scale = transferScale[i];
					if (scale > 1)
					{
						transferAmount[i] = (scale / 100) * sourceValues[i];

						if (transferAmount[i] > (targetValuesMax[i] - targetValues[i]))
							transferAmount[i] = (targetValuesMax[i] - targetValues[i]);
					}
					else if (scale < -1)
					{
						transferAmount[i] = ((scale * -1) / 100) * targetValues[i];

						if (transferAmount[i] > (sourceValuesMax[i] - sourceValues[i]))
							transferAmount[i] = (sourceValuesMax[i] - sourceValues[i]);
					}
					else
						transferAmount[i] = 0;

					if (LFLOXTransferLink && i == 0)
					{
						if (scale > 1)
						{
							oxidizerTransferAmount = (scale / 100) * sourceValues[i] * (11d / 9d);

							if (oxidizerTransferAmount > oxidizerAmount[0])
								oxidizerTransferAmount = oxidizerAmount[0];

							if (oxidizerTransferAmount > (oxidizerAmount[3] - oxidizerAmount[2]))
								oxidizerTransferAmount = (oxidizerAmount[3] - oxidizerAmount[2]);
						}
						else if (scale < -1)
						{
							oxidizerTransferAmount = ((scale * -1) / 100) * targetValues[i] * (11d / 9d);

							if (oxidizerTransferAmount > oxidizerAmount[2])
								oxidizerTransferAmount = oxidizerAmount[2];

							if (oxidizerTransferAmount > (oxidizerAmount[1] - oxidizerAmount[0]))
								oxidizerTransferAmount = (oxidizerAmount[1] - oxidizerAmount[0]);
						}
						else
							oxidizerTransferAmount = 0;

						oxidizerStartAmount = oxidizerTransferAmount;
					}

					transferStartAmount[i] = transferAmount[i];
				}

				transferComplete = evaModule.transferSpeed * (float)(transferScale.Max(a => Math.Abs(a)) / 100);
			}
			else
			{
				transferAmount = new double[evaModule.MaxTransfers];
				transferScale = new double[evaModule.MaxTransfers];
				transferStartAmount = new double[evaModule.maxTransfers];
				transferComplete = 0;
			}
		}

		private void toggleSelectedResource(PartResourceDefinition p)
		{
			if (LFLOXTransferLink)
			{
				if (p.name == "LiquidFuel" || p.name == "Oxidizer")
					return;
			}

			if (allowedStockResources.ContainsKey(p.name))
			{
				if (selectedResources.Contains(p))
					selectedResources.Remove(p);
				else if (selectedResources.Count < evaModule.MaxTransfers)
					selectedResources.Add(p);
			}
			else if (allowedOtherResources.ContainsKey(p.name))
			{
				if (selectedResources.Contains(p))
					selectedResources.Remove(p);
				else if (selectedResources.Count < evaModule.MaxTransfers)
					selectedResources.Add(p);
			}

			refreshPartDatabase();
			updateResources();
		}

		private void toggleLFLOXTransfer()
		{
			LFLOXTransferLink = !LFLOXTransferLink;

			if (LFLOXTransferLink)
			{
				PartResourceDefinition LF = allowedStockResources["LiquidFuel"];
				PartResourceDefinition LOX = allowedStockResources["Oxidizer"];

				if (LF == null || LOX == null)
					return;

				if (selectedResources.Contains(LF))
					selectedResources.Remove(LF);
				if (selectedResources.Contains(LOX))
					selectedResources.Remove(LOX);

				if (selectedResources.Count < evaModule.MaxTransfers)
					selectedResources.Insert(0, LF);
				else
					LFLOXTransferLink = false;
			}
			else
			{
				PartResourceDefinition LF = allowedStockResources["LiquidFuel"];

				if (LF == null)
					return;

				if (selectedResources.Contains(LF))
					selectedResources.Remove(LF);
			}

			refreshPartDatabase();
			updateResources();
		}

		private void vesselChange(Vessel v)
		{
			refreshPartDatabase();
			updateResources();
		}

		private void refreshPartDatabase()
		{
			sourceVesselParts = new Dictionary<string, List<Part>>();
			targetVesselParts = new Dictionary<string, List<Part>>();
			transferScale = new double[evaModule.MaxTransfers];

			for (int i = 0; i < selectedResources.Count; i++)
			{
				PartResourceDefinition r = selectedResources[i];

				if (r == null)
					continue;

				PartResourceDefinition rLOX = null;

				if (i == 0 && LFLOXTransferLink)
				{
					rLOX = allowedStockResources["Oxidizer"];

					if (rLOX == null)
						continue;
				}

				List<Part> sourceList = new List<Part>();

				foreach (Part p in sourceVessel.Parts)
				{
					if (!p.Resources.Contains(r.name))
						continue;

					if (p.Resources[r.name].maxAmount < 0.6)
						continue;

					if (i ==0 && LFLOXTransferLink)
					{
						if (!p.Resources.Contains(rLOX.name))
							continue;

						if (p.Resources[rLOX.name].maxAmount < 0.6)
							continue;
					}

					sourceList.Add(p);
				}

				sortParts(sourceList, r.name);

				if (!sourceVesselParts.ContainsKey(r.name))
					sourceVesselParts.Add(r.name, sourceList);

				List<Part> targetList = new List<Part>();

				foreach (Part p in targetVessel.Parts)
				{
					if (!p.Resources.Contains(r.name))
						continue;

					if (p.Resources[r.name].maxAmount < 0.6)
						continue;

					if (i == 0 && LFLOXTransferLink)
					{
						if (!p.Resources.Contains(rLOX.name))
							continue;

						if (p.Resources[rLOX.name].maxAmount < 0.6)
							continue;
					}

					targetList.Add(p);
				}

				sortParts(targetList, r.name);

				if (!targetVesselParts.ContainsKey(r.name))
					targetVesselParts.Add(r.name, targetList);
			}
		}

		protected override void RepeatingWorker()
		{
			if (transferActive)
				return;

			if (sourceVessel == null || targetVessel == null)
				return;

			updateResources();
		}

		private void transferStep()
		{
			float time = Time.fixedDeltaTime;

			if (transferComplete <= 0)
			{
				transferActive = false;
				transferAmount = new double[evaModule.MaxTransfers];
				transferScale = new double[evaModule.MaxTransfers];
				transferStartAmount = new double[evaModule.maxTransfers];
				return;
			}

			transferComplete -= time;

			for (int i = 0; i < selectedResources.Count; i++)
			{
				PartResourceDefinition r = selectedResources[i];

				if (!sourceVesselParts.ContainsKey(r.name) || !targetVesselParts.ContainsKey(r.name))
					continue;

				if (transferScale[i] <= 0.1 && transferScale[i] >= -0.1)
					continue;

				if (transferAmount[i] <= 0.001)
					continue;

				float timeSlice = time / (evaModule.transferSpeed * (float)(Math.Abs(transferScale[i]) / 100));

				if (transferScale[i] < -0.1)
				{
					if (targetValues[i] <= 0.001)
						continue;

					transferFromTo(targetVesselParts[r.name], sourceVesselParts[r.name], r, timeSlice, i);
				}
				else
				{
					if (sourceValues[i] <= 0.001)
						continue;

					transferFromTo(sourceVesselParts[r.name], targetVesselParts[r.name], r, timeSlice, i);
				}
			}
		}

		private void transferFromTo(List<Part> fromParts, List<Part> toParts, PartResourceDefinition resource, float time, int index)
		{
			bool lfLOX = index == 0 && LFLOXTransferLink;

			double resourceSubtract = transferStartAmount[index] * time;
			double partSubtract = resourceSubtract;

			double loxSubract = 0;
			double loxPartSubtract = 0;

			if (lfLOX)
			{
				loxSubract = oxidizerStartAmount * time * (11d / 9d);
				loxPartSubtract = loxSubract;
			}

			for (int i = 0; i < fromParts.Count; i++)
			{
				Part p = fromParts[i];

				if (p == null)
					continue;

				PartResource r = p.Resources[resource.name];
				PartResource lox = null;

				if (r == null)
					continue;

				if (lfLOX)
				{
					lox = p.Resources["Oxidizer"];

					if (lox == null)
						continue;
				}

				if (lfLOX)
				{
					if (oxidizerTransferAmount > 0)
					{
						if (lox.maxAmount > 0.1)
						{
							if (lox.amount > 0.001)
							{
								if (loxPartSubtract > lox.amount)
								{
									oxidizerTransferAmount -= lox.amount;
									loxPartSubtract -= lox.amount;
									lox.amount -= lox.amount;
								}
								else
								{
									oxidizerTransferAmount -= loxPartSubtract;
									lox.amount -= loxPartSubtract;
									loxPartSubtract = 0;
								}
							}
						}
					}
				}

				if (r.maxAmount > 0.1)
				{
					if (r.amount > 0.001)
					{
						if (partSubtract > r.amount)
						{
							transferAmount[index] -= r.amount;
							partSubtract -= r.amount;
							r.amount -= r.amount;
						}
						else
						{
							r.amount -= partSubtract;
							transferAmount[index] -= partSubtract;
							partSubtract = 0;
						}
					}
				}

				if (lfLOX && loxPartSubtract <= 0 && partSubtract <= 0)
					break;
				else if (partSubtract <= 0)
					break;
			}

			double partAdd = resourceSubtract;
			double loxPartAdd = 0;

			if (lfLOX)
			{
				loxPartAdd = loxSubract;
			}

			for (int i = 0; i < toParts.Count; i++)
			{
				Part p = toParts[i];

				if (p == null)
					continue;

				PartResource r = p.Resources[resource.name];
				PartResource lox = null;

				if (r == null)
					continue;

				if (lfLOX)
				{
					lox = p.Resources["Oxidizer"];

					if (lox == null)
						continue;

					if (oxidizerTransferAmount > 0)
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
				}

				if (r.maxAmount > 0.1)
				{
					if (r.amount < r.maxAmount - 0.001)
					{
						if (partAdd > r.maxAmount - r.amount)
						{
							LogFormatted_DebugOnly("Not Enough Space For {0:F2}; Adding {1:F2} of {2}", partAdd, r.maxAmount - r.amount, r.resourceName);
							partAdd -= (r.maxAmount - r.amount);
							r.amount += (r.maxAmount - r.amount);
						}
						else
						{
							r.amount += partAdd;
							partAdd = 0;
						}
					}
					else
						LogFormatted_DebugOnly("No Space");
				}
				else
					LogFormatted_DebugOnly("Max Too Low");

				if (lfLOX && loxPartAdd <= 0 && partAdd <= 0)
					break;
				else if (partAdd <= 0)
					break;
			}
		}

		private void updateResources()
		{
			Array.Clear(oxidizerAmount, 0, 4);
			Array.Clear(sourceValues, 0, sourceValues.Length);
			Array.Clear(sourceValuesMax, 0, sourceValuesMax.Length);
			Array.Clear(targetValues, 0, targetValues.Length);
			Array.Clear(targetValuesMax, 0, targetValuesMax.Length);

			for (int i = 0; i < selectedResources.Count; i++)
			{
				bool lfLOX = LFLOXTransferLink && i == 0;

				PartResourceDefinition r = selectedResources[i];

				if (sourceVesselParts.ContainsKey(r.name))
				{
					foreach (Part p in sourceVesselParts[r.name])
					{
						if (!p.Resources.Contains(r.name))
							continue;

						PartResource pr = p.Resources[r.name];

						if (lfLOX)
						{
							if (!p.Resources.Contains("Oxidizer"))
								continue;

							PartResource lox = p.Resources["Oxidizer"];

							oxidizerAmount[0] += lox.amount;

							oxidizerAmount[1] += lox.maxAmount;
						}

						sourceValues[i] += pr.amount;

						sourceValuesMax[i] += pr.maxAmount;
					}
				}

				if (targetVesselParts.ContainsKey(r.name))
				{
					foreach (Part p in targetVesselParts[r.name])
					{
						if (!p.Resources.Contains(r.name))
							continue;

						PartResource pr = p.Resources[r.name];

						if (lfLOX)
						{
							if (!p.Resources.Contains("Oxidizer"))
								continue;

							PartResource lox = p.Resources["Oxidizer"];

							oxidizerAmount[2] += lox.amount;

							oxidizerAmount[3] += lox.maxAmount;
						}

						targetValues[i] += pr.amount;

						targetValuesMax[i] += pr.maxAmount;
					}
				}
			}
		}

		private void sortParts (List<Part> list, string resourceName)
		{
			switch (evaModule.fillMode)
			{
				case 0:
					{
						list.Sort((a, b) => a.Resources[resourceName].maxAmount.CompareTo(b.Resources[resourceName].maxAmount));
						break;
					}
				case 1:
					{
						list.Sort((a, b) => a.Resources[resourceName].maxAmount.CompareTo(b.Resources[resourceName].maxAmount));
						list.Reverse();
						break;
					}
				default:
					{
						break;
					}
			}
		}
	}
}
