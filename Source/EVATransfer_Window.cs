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
		private bool windowActive;
		private bool LFLOXTransferLink;
		private bool transferActive;
		private bool dropDown;
		private Rect ddRect;
		private Vector2 dropDownScroll;
		private string version;

		private Vessel sourceVessel;
		private Vessel targetVessel;

		private ModuleEVATransfer evaModule;

		private float transferComplete;

		private DictionaryValueList<string, TransferGroup> allowedOtherResources = new DictionaryValueList<string, TransferGroup>();
		private DictionaryValueList<string, TransferGroup> allowedStockResources = new DictionaryValueList<string, TransferGroup>();

		private List<TransferGroup> selectedResources = new List<TransferGroup>();

		private LFLOXTransferGroup lfloxGroup;

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
			base.Start();

			WindowRect = sessionRect;
			GameEvents.onVesselChange.Add(vesselChange);
			GameEvents.onVesselWasModified.Add(vesselChange);
		}

		public bool TransferActive
		{
			get { return transferActive; }
		}

		public void setup(bool a, bool b, bool c, bool d, bool e, bool f, bool g, ModuleEVATransfer mod)
		{
			evaModule = mod;

			if (evaModule == null)
				return;

			TooltipsEnabled = evaModule.tooltips;

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

			if (a && b)
			{
				if (!EVATransfer_Startup.loadedResources.ContainsKey("LiquidFuel") || !EVATransfer_Startup.loadedResources.ContainsKey("Oxidizer"))
					return;

				lfloxGroup = new LFLOXTransferGroup(EVATransfer_Startup.loadedResources["LiquidFuel"], EVATransfer_Startup.loadedResources["Oxidizer"], evaModule.loxlfTransferRatio);

				transferLFLOX = true;
			}
		}

		public void activateVessels(Vessel a, Vessel b)
		{
			sourceVessel = a;
			targetVessel = b;

			vesselChange(null);

			updateResources(true, true);

			windowActive = true;
		}

		public void severConnection()
		{
			windowActive = false;
		}

		private void addResource(string name)
		{
			if (allowedStockResources.Contains(name))
				return;

			if (!EVATransfer_Startup.loadedResources.ContainsKey(name))
				return;

			TransferableResource r = EVATransfer_Startup.loadedResources[name];

			if (r == null)
				return;

			TransferGroup t = new TransferGroup(r);

			if (t == null)
				return;

			allowedStockResources.Add(name, t);
		}

		private void addAllResources()
		{
			foreach (TransferableResource r in EVATransfer_Startup.loadedResources.Values)
			{
				switch (r.Name)
				{
					case "LiquidFuel":
					case "Oxidizer":
					case "MonoPropellant":
					case "XenonGas":
					case "ElectricCharge":
					case "Ore":
						continue;
				}

				if (r.Mode == ResourceTransferMode.NONE)
					continue;

				if (allowedOtherResources.Contains(r.Name))
					continue;

				if (r == null)
					continue;

				TransferGroup t = new TransferGroup(r);

				if (t == null)
					return;

				allowedOtherResources.Add(r.Name, t);
			}
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();

			GameEvents.onVesselChange.Remove(vesselChange);
			GameEvents.onVesselWasModified.Remove(vesselChange);
		}

		protected override void FixedUpdate()
		{
			if (!windowActive)
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

			updateResources(true, false);
		}

		private void updateResources(bool values, bool parts)
		{
			int l = selectedResources.Count;

			for (int i = 0; i < l; i++)
			{
				TransferGroup t = selectedResources[i];

				if (t == null)
					continue;

				t.updateValues(values, parts, evaModule.fillMode, evaModule.ignoreInactiveTanks);
			}
		}

		protected override void DrawWindow(int id)
		{
			versionLabel(id);
			closeBox(id);

			if (!windowActive)
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
			if (dropDown && Event.current.type == EventType.MouseDown && !ddRect.Contains(Event.current.mousePosition))
			{
				WindowOptions = new GUILayoutOption[3] { GUILayout.Width(300), GUILayout.Height(120), GUILayout.MaxHeight(120) };
				dropDown = false;
			}

			sessionRect = WindowRect;
		}

		private void versionLabel(int id)
		{
			Rect r = new Rect(4, 0, 50, 18);
			GUI.Label(r, version, EVATransfer_Startup.labelSmall);
		}

		private void closeBox(int id)
		{
			Rect r = new Rect(WindowRect.width - 30, 1, 22, 22);
			if (GUI.Button(r, "X"))
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

			Color old = GUI.color;

			int l = allowedStockResources.Count;

			for (int i = 0; i < l; i++)
			{
				TransferGroup t = allowedStockResources.At(i);

				if (t == null)
					continue;

				GUIStyle s;
				if (selectedResources.Contains(t))
					s = EVATransfer_Startup.activeButton;
				else if (!t.OnBoard)
					s = EVATransfer_Startup.button;
				else
					s = EVATransfer_Startup.button;

				if (GUILayout.Button(new GUIContent("", t.Resource.DisplayName), s, GUILayout.Width(36), GUILayout.Height(36)))
				{
					if (!transferActive)
						toggleSelectedResource(t);
				}

				Rect r = GUILayoutUtility.GetLastRect();
				GUI.color = t.Resource.IconColor;
				GUI.DrawTexture(r, t.Resource.Icon);
				GUI.color = old;
			}

			if (allowedOtherResources.Count > 0)
			{
				bool flag = false;

				for (int i = allowedOtherResources.Count - 1; i >= 0; i--)
				{
					TransferGroup t = allowedOtherResources.At(i);

					if (t == null)
						continue;

					if (t.OnBoard)
					{
						flag = true;
						break;
					}
				}

				if (flag)
				{
					GUILayout.FlexibleSpace();

					if (GUILayout.Button(EVATransfer_Startup.dropDownIcon, GUILayout.Width(36), GUILayout.Height(36)))
					{
						if (!transferActive)
						{
							dropDown = !dropDown;
							if (!dropDown)
								WindowOptions = new GUILayoutOption[3] { GUILayout.Width(300), GUILayout.Height(120), GUILayout.MaxHeight(120) };
						}
					}
				}
			}

			if (transferLFLOX)
			{
				Rect r = new Rect(36, 9, 20, 20);
				if (GUI.Button(r, new GUIContent(EVATransfer_Startup.linkIcon, "Link LF and LOX"), EVATransfer_Startup.textureButton))
				{
					if (!transferActive)
						toggleLFLOXTransfer();
				}

				if (LFLOXTransferLink)
				{
					r.x -= 6;
					r.y += 8;
					r.width = 32;
					GUI.DrawTexture(r, EVATransfer_Startup.bracketIcon);
				}
			}

			GUILayout.EndHorizontal();
		}

		private void resourceLabels(int id)
		{
			int l = selectedResources.Count;

			if (l <= 0)
				return;

			GUILayout.Space(30);

			Rect r = new Rect(40, 78, 80, 20);

			GUI.Label(r, sourceVessel.vesselName, EVATransfer_Startup.labelBig);

			r.x += 130;

			GUI.Label(r, targetVessel.vesselName, EVATransfer_Startup.labelBig);

			r.y += 17;

			for (int i = 0; i < l; i++)
			{
				TransferGroup t = selectedResources[i];

				t.drawResourceGroup(r.y, transferActive, dropDown);

				if (t.drawCloseGroup(WindowRect.width - 21, r.y + 12, transferActive, dropDown))
				{
					toggleSelectedResource(t);
				}

				if (t == lfloxGroup)
				{
					GUILayout.Space(80);
					r.y += 78;
				}
				else
				{
					GUILayout.Space(52);
					r.y += 50;
				}
			}

			if (selectedResources.Any(a => Math.Abs(a.TransferScale) > 0.1))
			{
				GUILayout.Space(25);

				r.x = 10;
				r.y += 10;
				r.width = WindowRect.width - 20;
				r.height = 20;

				if (dropDown)
					GUI.Label(r, transferActive ? "Stop Transfer" : "Begin Transfer", EVATransfer_Startup.button);
				else
				{
					if (GUI.Button(r, transferActive ? "Stop Transfer" : "Begin Transfer"))
						toggleTransfer();
				}
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

			int l = allowedOtherResources.Count;

			for (int i = 0; i < l; i++)
			{
				TransferGroup t = allowedOtherResources.At(i);

				if (!t.OnBoard)
					continue;

				if (r.yMin >= (dropDownScroll.y - 25) && r.yMax <= (dropDownScroll.y + 285))
				{
					if (GUI.Button(r, t.Resource.DisplayName, selectedResources.Contains(t) ? EVATransfer_Startup.activeButton : EVATransfer_Startup.button))
					{
						toggleSelectedResource(t);
						WindowOptions = new GUILayoutOption[3] { GUILayout.Width(300), GUILayout.Height(120), GUILayout.MaxHeight(120) };
						dropDown = false;
					}
				}
				r.y += 25;
			}

			GUI.EndScrollView();
		}

		public void toggleTransfer()
		{
			transferActive = !transferActive;
			dropDown = false;

			int l = selectedResources.Count;

			if (transferActive)
			{
				for (int i = 0; i < l; i++)
				{
					TransferGroup t = selectedResources[i];

					if (t == null)
						continue;

					t.toggleTransfer();
				}

				transferComplete = evaModule.transferSpeed * (float)(selectedResources.Select(s => s.TransferScale).Max(a => Math.Abs(a)) / 100);
			}
			else
			{
				for (int i = 0; i < l; i++)
				{
					TransferGroup t = selectedResources[i];

					if (t == null)
						continue;

					t.finishTransfer();
				}

				transferComplete = 0;
			}
		}

		private void toggleSelectedResource(TransferGroup t)
		{
			if (LFLOXTransferLink)
			{
				if (t.Resource.Name == "LiquidFuel" || t.Resource.Name == "Oxidizer")
					return;
			}

			if (allowedStockResources.Contains(t.Resource.Name))
			{
				if (selectedResources.Contains(t))
					selectedResources.Remove(t);
				else if (selectedResources.Count < evaModule.MaxTransfers)
					selectedResources.Add(t);
			}
			else if (allowedOtherResources.Contains(t.Resource.Name))
			{
				if (selectedResources.Contains(t))
					selectedResources.Remove(t);
				else if (selectedResources.Count < evaModule.MaxTransfers)
					selectedResources.Add(t);
			}

			updateResources(true, true);
		}

		private void toggleLFLOXTransfer()
		{
			if (lfloxGroup == null)
				return;

			LFLOXTransferLink = !LFLOXTransferLink;

			if (LFLOXTransferLink)
			{
				TransferGroup LF = allowedStockResources["LiquidFuel"];
				TransferGroup LOX = allowedStockResources["Oxidizer"];

				if (LF == null || LOX == null)
					return;

				if (selectedResources.Contains(LF))
					selectedResources.Remove(LF);
				if (selectedResources.Contains(LOX))
					selectedResources.Remove(LOX);

				if (selectedResources.Count < evaModule.MaxTransfers)
				{
					lfloxGroup.updateVessels(sourceVessel, targetVessel);
					selectedResources.Insert(0, lfloxGroup);
				}
				else
					LFLOXTransferLink = false;
			}
			else
			{
				if (selectedResources.Contains(lfloxGroup))
					selectedResources.Remove(lfloxGroup);
			}

			updateResources(true, true);
		}

		private void vesselChange(Vessel v)
		{
			int l = allowedStockResources.Count;

			for (int i = 0; i < l; i++)
			{
				TransferGroup t = allowedStockResources.At(i);

				if (t == null)
					continue;

				t.updateVessels(sourceVessel, targetVessel);
			}

			l = allowedOtherResources.Count;

			for (int i = 0; i < l; i++)
			{
				TransferGroup t = allowedOtherResources.At(i);

				if (t == null)
					continue;

				t.updateVessels(sourceVessel, targetVessel);
			}

			updateResources(true, true);
		}

		protected override void RepeatingWorker()
		{
			if (transferActive)
				return;

			if (sourceVessel == null || targetVessel == null)
				return;

			updateResources(true, false);
		}

		private void transferStep()
		{
			float time = Time.fixedDeltaTime;

			int l = selectedResources.Count;

			if (transferComplete <= 0)
			{
				transferActive = false;
				for (int i = 0; i < l; i++)
				{
					TransferGroup t = selectedResources[i];

					if (t == null)
						continue;

					t.finishTransfer();
				}
				return;
			}

			transferComplete -= time;

			for (int i = 0; i < l; i++)
			{
				TransferGroup t = selectedResources[i];

				if (t == null)
					continue;

				t.transferResources(time, evaModule.transferSpeed);
			}
		}

	}
}
