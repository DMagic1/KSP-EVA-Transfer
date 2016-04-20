#region license
/*The MIT License (MIT)
ModuleEVATransfer - Part module to control the EVA fuel line

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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CompoundParts;
using Experience;
using UnityEngine;

namespace EVATransfer
{
	public class ModuleEVATransfer : CModuleLinkedMesh
	{
		[KSPField]
		public string useProfession = "Engineer";
		[KSPField]
		public int minLevel = 0;
		[KSPField]
		public float maxDistance = 200;
		[KSPField]
		public float maxSlack = 10;
		[KSPField]
		public int maxTransfers = 5;
		[KSPField]
		public float transferSpeed = 8;
		[KSPField]
		public int fillMode = 2;
		[KSPField]
		public bool tooltips = true;
		[KSPField]
		public float loxlfTransferRatio = 1.2222f;
		[KSPField]
		public bool transferLF = true;
		[KSPField]
		public bool transferLOX = true;
		[KSPField]
		public bool transferMono = true;
		[KSPField]
		public bool transferXen= true;
		[KSPField]
		public bool transferEC = true;
		[KSPField]
		public bool transferOre = true;
		[KSPField]
		public bool transferAll = true;

		private Vessel EVA;
		private Transform EVAJetpack;
		private EVATransfer_Window evaWindow;

		private CompoundPart.AttachState EVAAttachState;

		private float connectionDistance;

		private Vessel targetVessel;

		private RaycastHit hit;

		private uint targetID;
		private Guid targetVesselID;
		private bool loaded = true;

		public int MaxTransfers
		{
			get { return maxTransfers; }
		}

		public override void OnStart(PartModule.StartState state)
		{
			useProfession = professionValid(useProfession);
			minLevel = (int)clampValue(minLevel, 0, 5);
			maxDistance = clampValue(maxDistance, 10, 500);
			maxSlack = clampValue(maxSlack, 5, 50);
			maxTransfers = (int)clampValue(maxTransfers, 1, 8);
			transferSpeed = clampValue(transferSpeed, 4, 50);
			fillMode = (int)clampValue(fillMode, 0, 2);

			compoundPart.maxLength = maxDistance;

			base.OnStart(state);

			if (state == StartState.Editor)
				return;

			Events["pickupEVAFuelLine"].guiName = "Pickup EVA Transfer Line";
			Events["cutEVAFuelLine"].guiName = "Cut EVA Transfer Line";
			Events["dropEVAFuelLine"].guiName = "Drop EVA Transfer Line";
			Events["openEVAFuelTransfer"].guiName = "Open EVA Transfer Control";
			Events["closeEVAFuelTransfer"].guiName = "Close EVA Transfer Control";

			GameEvents.onPartPack.Add(partPack);
		}

		private void partPack(Part p)
		{
			if (p != this.part)
				return;

			if (evaWindow != null)
			{
				if (evaWindow.Visible)
				{
					Events["openEVAFuelTransfer"].active = true;
					Events["closeEVAFuelTransfer"].active = false;
					evaWindow.StopRepeatingWorker();
					evaWindow.Visible = false;
				}

				if (evaWindow.TransferActive)
					evaWindow.toggleTransfer();
			}
		}

		private void Update()
		{
			if (HighLogic.LoadedSceneIsEditor)
			{
				if (compoundPart.attachState == CompoundPart.AttachState.Attaching)
				{

					compoundPart.attachState = CompoundPart.AttachState.Detached;
					InputLockManager.ClearControlLocks();
				}
				return;
			}

			if (!loaded)
			{
				loaded = true;

				StartCoroutine(loadConnections());
			}

			if (EVAAttachState != CompoundPart.AttachState.Attaching)
				return;

			if (!HighLogic.LoadedSceneIsFlight)
				return;

			if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 20, 1) && checkDistance)
			{
				//print("[EVA Transfer] Hit Target");
				Part p = hit.collider.gameObject.GetComponentUpwards<Part>();

				if (p == null || p.vessel == this.vessel || p.vessel.isEVA)
				{
					//print("[EVA Transfer] Wrong Target Type...");
					compoundPart.direction = Vector3.zero;
					setKerbalAttach();
				}
				//else if (Physics.Raycast(new Ray(startCap.transform.position, base.transform.InverseTransformPoint(hit.point).normalized), (startCap.transform.position - hit.point).magnitude - 1, (1 << 0) | (1 << 10) | (1 << 15)))
				//{
				//	print("[EVA Transfer] Something In The Way... Distance " + (startCap.transform.position - hit.point).magnitude);
				//	setKerbalAttach();
				//}
				else
				{
					//print("[EVA Transfer] Found Target....");
					compoundPart.target = p;

					compoundPart.direction = base.transform.InverseTransformPoint(hit.point).normalized;
					compoundPart.targetPosition = base.transform.InverseTransformPoint(hit.point);
					compoundPart.targetRotation = Quaternion.FromToRotation(Vector3.right, base.transform.InverseTransformDirection(hit.normal));

					if (Input.GetMouseButtonUp(0))
					{
						if ((startCap.transform.position - hit.point).magnitude < maxDistance)
						{
							attachFuelLine();
						}
					}
				}
			}
			else
				setKerbalAttach();

			base.OnPreviewAttachment(compoundPart.direction, compoundPart.targetPosition, compoundPart.targetRotation);
		}

		public override void OnSave(ConfigNode node)
		{
			if (EVAAttachState == CompoundPart.AttachState.Attached)
			{
				node.AddValue("TargetVesselID", targetVesselID);
				node.AddValue("TargetPartID", targetID);
			}

			base.OnSave(node);
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			if (!HighLogic.LoadedSceneIsFlight)
				return;

			if (!node.HasValue("TargetVesselID") || !node.HasValue("TargetPartID"))
			{
				loaded = true;
				severFuelLine();
				return;
			}

			try
			{
				targetVesselID = new Guid(node.GetValue("TargetVesselID"));
			}
			catch (Exception e)
			{
				print("[EVA Refuel] Exception in assigning vessel ID:\n" + e);
			}

			try
			{
				targetID = uint.Parse(node.GetValue("TargetPartID"));
			}
			catch (Exception e)
			{
				print("[EVA Refuel] Exception in assigning target part ID:\n" + e);
			}

			EVAAttachState = CompoundPart.AttachState.Attached;

			loaded = false;
		}

		IEnumerator loadConnections()
		{
			if (!HighLogic.LoadedSceneIsFlight)
				yield break;

			int timer = 0;

			while (!FlightGlobals.ready || FlightGlobals.ActiveVessel == null || (this.vessel == FlightGlobals.ActiveVessel && timer < 20))
			{
				if (FlightGlobals.ready)
					timer++;

				yield return null;
			}

			try
			{
				targetVessel = FlightGlobals.Vessels.FirstOrDefault(v => v.id == targetVesselID);
			}
			catch (Exception e)
			{
				print("[EVA Transfer] Exception While Loading Target Vessel\n" + e);
				severFuelLine();
				yield break;
			}

			if (targetVessel == null)
			{
				severFuelLine();
				print("[EVA Transfer] Target Vessel Not Found...");
				yield break;
			}

			try
			{
				compoundPart.target = targetVessel.Parts.FirstOrDefault(p => p.craftID == targetID);
			}
			catch (Exception e)
			{
				print("[EVA Transfer] Exception While Loading Target Part\n" + e);
				severFuelLine();
				yield break;
			}

			if (compoundPart.target == null)
			{
				severFuelLine();
				print("[EVA Transfer] Target Part Not Found...");
				yield break;
			}

			attachFuelLine();
		}

		private void OnDestroy()
		{
			if (EVAAttachState != CompoundPart.AttachState.Attached)
				severFuelLine();

			GameEvents.onPartPack.Remove(partPack);
		}

		public override void OnTargetSet(Part target)
		{
			base.OnTargetSet(target);
		}

		public override void OnTargetLost()
		{
			base.OnTargetLost();
		}

		private void LateUpdate()
		{
			if (EVAAttachState != CompoundPart.AttachState.Attached)
				return;

			OnTargetUpdate();
		}

		public override void OnTargetUpdate()
		{
			base.OnTargetUpdate();
		}

		private void FixedUpdate()
		{
			if (EVAAttachState != CompoundPart.AttachState.Attached)
				return;

			float distance = (endCap.transform.position - startCap.transform.position).magnitude;

			if (((endCap.transform.position - startCap.transform.position).magnitude - connectionDistance) > maxSlack)
				severFuelLine();
		}

		public override void OnPreviewAttachment(UnityEngine.Vector3 rDir, UnityEngine.Vector3 rPos, UnityEngine.Quaternion rRot)
		{
			base.OnPreviewAttachment(rDir, rPos, rRot);
		}

		[KSPEvent(guiActive = false, guiActiveUnfocused = true, externalToEVAOnly = true, active = true, unfocusedRange = 4)]
		public void pickupEVAFuelLine()
		{
			if (!checkEVAVessel)
			{
				ScreenMessages.PostScreenMessage("Current Vessel is not an EVA Kerbal...", 6f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			if (!checkProfession)
			{
				ScreenMessages.PostScreenMessage("The Kerbal must be an " + useProfession + " to activate the transfer line.", 6f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			if (!checkLevel)
			{
				ScreenMessages.PostScreenMessage("The Kerbal must be above level " + minLevel + " to activate the transfer line.", 6f, ScreenMessageStyle.UPPER_CENTER);
				return;
			}

			if (!setEVAPosition())
				return;

			Events["pickupEVAFuelLine"].active = false;
			Events["dropEVAFuelLine"].active = true;

			EVAAttachState = CompoundPart.AttachState.Attaching;
			compoundPart.attachState = CompoundPart.AttachState.Detached;

			this.startCap.gameObject.SetActive(true);
		}

		[KSPEvent(guiActive = false, guiActiveUnfocused = true, externalToEVAOnly = true, active = false, unfocusedRange = 4)]
		public void cutEVAFuelLine()
		{
			if (FlightGlobals.ActiveVessel.isEVA)
			{
				if (!checkEVAVessel)
					return;

				if (!checkEVADistance)
					return;
			}

			severFuelLine();
		}

		[KSPEvent(guiActive = false, guiActiveUnfocused = true, externalToEVAOnly = true, active = false, unfocusedRange = 10)]
		public void dropEVAFuelLine()
		{
			Events["dropEVAFuelLine"].active = false;
			Events["pickupEVAFuelLine"].active = true;
			Events["cutEVAFuelLine"].active = false;
			Events["openEVAFuelTransfer"].active = false;
			Events["closeEVAFuelTransfer"].active = false;
			Events["cutEVAFuelLine"].unfocusedRange = 4;
			Events["openEVAFuelTransfer"].unfocusedRange = 5;
			Events["closeEVAFuelTransfer"].unfocusedRange = 5;

			EVAAttachState = CompoundPart.AttachState.Detached;
			compoundPart.attachState = CompoundPart.AttachState.Detached;

			OnTargetLost();
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = true, externalToEVAOnly = false, active = false, unfocusedRange = 5)]
		public void openEVAFuelTransfer()
		{
			if (FlightGlobals.ActiveVessel.isEVA)
			{
				if (!checkEVAVessel)
					return;

				if (!checkEVADistance)
					return;
			}
			else if (FlightGlobals.ActiveVessel != vessel && FlightGlobals.ActiveVessel != targetVessel)
				return;

			if (evaWindow == null)
			{
				evaWindow = gameObject.AddComponent<EVATransfer_Window>();
				evaWindow.setup(transferLF, transferLOX, transferMono, transferXen, transferEC, transferOre, transferAll, this);
			}

			if (evaWindow == null)
				return;

			evaWindow.Visible = true;

			evaWindow.StartRepeatingWorker(0.2f);

			Events["openEVAFuelTransfer"].active = false;
			Events["closeEVAFuelTransfer"].active = true;
		}

		[KSPEvent(guiActive = true, guiActiveUnfocused = true, externalToEVAOnly = false, active = false, unfocusedRange = 5)]
		public void closeEVAFuelTransfer()
		{
			if (FlightGlobals.ActiveVessel.isEVA)
			{
				if (!checkEVAVessel)
					return;

				if (!checkEVADistance)
					return;
			}
			else if (FlightGlobals.ActiveVessel != vessel && FlightGlobals.ActiveVessel != targetVessel)
				return;

			if (evaWindow != null)
			{
				evaWindow.Visible = false;
				evaWindow.StopRepeatingWorker();
			}

			Events["openEVAFuelTransfer"].active = true;
			Events["closeEVAFuelTransfer"].active = false;
		}

		private void attachFuelLine()
		{
			targetVessel = compoundPart.target.vessel;
			targetVesselID = targetVessel.id;
			targetID = compoundPart.target.craftID;

			if (evaWindow == null)
			{
				evaWindow = gameObject.AddComponent<EVATransfer_Window>();
				evaWindow.setup(transferLF, transferLOX, transferMono, transferXen, transferEC, transferOre, transferAll, this);
			}

			evaWindow.activateVessels(vessel, targetVessel);

			Events["dropEVAFuelLine"].active = false;
			Events["pickupEVAFuelLine"].active = false;
			Events["cutEVAFuelLine"].active = true;
			Events["openEVAFuelTransfer"].active = true;
			Events["closeEVAFuelTransfer"].active = false;

			OnTargetSet(compoundPart.target);

			connectionDistance = (endCap.transform.position - startCap.transform.position).magnitude;

			Events["cutEVAFuelLine"].unfocusedRange = connectionDistance + 10;
			Events["openEVAFuelTransfer"].unfocusedRange = connectionDistance + 10;
			Events["closeEVAFuelTransfer"].unfocusedRange = connectionDistance + 10;

			compoundPart.attachState = CompoundPart.AttachState.Detached;
			EVAAttachState = CompoundPart.AttachState.Attached;
		}

		private void severFuelLine()
		{
			Events["dropEVAFuelLine"].active = false;
			Events["pickupEVAFuelLine"].active = true;
			Events["cutEVAFuelLine"].active = false;
			Events["openEVAFuelTransfer"].active = false;
			Events["closeEVAFuelTransfer"].active = false;
			Events["cutEVAFuelLine"].unfocusedRange = 4;
			Events["openEVAFuelTransfer"].unfocusedRange = 5;
			Events["closeEVAFuelTransfer"].unfocusedRange = 5;

			if (evaWindow != null)
			{
				evaWindow.severConnection();
			}

			OnTargetLost();

			targetVessel = null;

			compoundPart.target = null;

			compoundPart.direction = Vector3.zero;
			compoundPart.targetPosition = Vector3.zero;
			compoundPart.targetRotation = Quaternion.identity;

			compoundPart.attachState = CompoundPart.AttachState.Detached;
			EVAAttachState = CompoundPart.AttachState.Detached;
		}

		private string professionValid(string s)
		{
			switch (s)
			{
				case "Pilot":
				case "Engineer":
				case "Scientist":
					return s;
				case "pilot":
					return "Pilot";
				case "engineer":
					return "Engineer";
				case "scientist":
					return "Scientist";
			}

			return "";
		}

		private float clampValue(float value, float min, float max)
		{
			return Mathf.Clamp(value, min, max);
		}

		private void setKerbalAttach()
		{
			if (EVAJetpack == null)
				compoundPart.attachState = CompoundPart.AttachState.Detached;

			compoundPart.direction = base.transform.InverseTransformPoint(EVAJetpack.position).normalized;
			compoundPart.targetPosition = base.transform.InverseTransformPoint(EVAJetpack.position);
			compoundPart.targetRotation = Quaternion.FromToRotation(Vector3.left, base.transform.InverseTransformDirection(EVAJetpack.position));
		}

		private bool setEVAPosition()
		{
			List<SkinnedMeshRenderer> meshes = new List<SkinnedMeshRenderer>(EVA.rootPart.GetComponentsInChildren<SkinnedMeshRenderer>() as SkinnedMeshRenderer[]);
			foreach (SkinnedMeshRenderer m in meshes)
			{
				if (m == null)
					continue;

				if (m.name != "jetpack_base01")
					continue;

				foreach(Transform bone in m.bones)
				{
					if (bone == null)
						continue;

					if (bone.name != "bn_jetpack01")
						continue;

					EVAJetpack = bone.transform;
					return true;
				}
			}

			return false;
		}

		private bool checkEVAVessel
		{
			get
			{
				EVA = FlightGlobals.ActiveVessel;

				if (EVA == null)
					return false;

				if (!EVA.isEVA)
					return false;

				if (EVA.GetVesselCrew().Count != 1)
					return false;

				return true;
			}
		}

		private bool checkProfession
		{
			get
			{
				if (string.IsNullOrEmpty(useProfession))
					return true;

				if (EVA.GetVesselCrew().First().experienceTrait.TypeName != useProfession)
					return false;

				return true;
			}
		}

		private bool checkLevel
		{
			get
			{
				if (minLevel <= 0)
					return true;

				if (EVA.GetVesselCrew().First().experienceLevel < minLevel)
					return false;

				return true;
			}
		}

		private bool checkDistance
		{
			get
			{
				return EVA == FlightGlobals.ActiveVessel && (EVA.transform.position - hit.point).magnitude < 8 && (startCap.transform.position - hit.point).magnitude < maxDistance;
			}
		}

		private bool checkEVADistance
		{
			get
			{
				if (FlightGlobals.ActiveVessel.isEVA)
				{
					return (startCap.transform.position - EVA.transform.position).magnitude < 10 || (endCap.transform.position - EVA.transform.position).magnitude < 10;
				}

				return true;
			}
		}
	}
}
