using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SteelUI450SlotsBackpack;

[HarmonyPatch]
public static class BackpackPatches
{
	private static readonly FieldRef<Bag, ItemStack[]> BagItemsRef = AccessTools.FieldRefAccess<Bag, ItemStack[]>("items");

	private static readonly FieldRef<XUiC_BackpackWindow, EntityPlayerLocal> BackpackWindowLocalPlayerRef = AccessTools.FieldRefAccess<XUiC_BackpackWindow, EntityPlayerLocal>("localPlayer");

	private static float _nextAllowedWheelPagingTime;

	private static int DesiredSlots => ModApi.Config?.TotalSlots ?? 405;

	private static bool IsShiftHeld()
	{
		return Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303);
	}

	private static bool ShouldHandleWheelPaging(XUiC_TabSelector tabSelector)
	{
		BackpackConfig config = ModApi.Config;
		if (config == null || !config.WheelPagingEnabled)
		{
			return false;
		}
		if (tabSelector == null)
		{
			return false;
		}
		if (config.RequireShiftForWheelPaging && !IsShiftHeld())
		{
			return false;
		}
		if (Time.unscaledTime < _nextAllowedWheelPagingTime)
		{
			return false;
		}
		return true;
	}

	private static ItemStack[] NormalizeSlots(ItemStack[] original, int desired)
	{
		desired = Math.Max(1, desired);
		if (original == null)
		{
			return ItemStack.CreateArray(desired);
		}
		if (original.Length == desired)
		{
			return original;
		}
		ItemStack[] array = ItemStack.CreateArray(desired);
		int num = Math.Min(original.Length, desired);
		for (int i = 0; i < num; i++)
		{
			array[i] = ((original[i] != null) ? original[i].Clone() : ItemStack.Empty.Clone());
		}
		return array;
	}

	private static void EnsureBagSize(Bag bag)
	{
		try
		{
			ItemStack[] array = BagItemsRef.Invoke(bag);
			ItemStack[] array2 = NormalizeSlots(array, DesiredSlots);
			if (array != array2)
			{
				BagItemsRef.Invoke(bag) = array2;
			}
		}
		catch (Exception arg)
		{
			Debug.LogError((object)$"[Steel405 Rebuild] EnsureBagSize failed: {arg}");
		}
	}

	[HarmonyPatch(typeof(Bag), "checkBagAssigned")]
	[HarmonyPrefix]
	private static void Bag_CheckBagAssigned_Prefix(ref int slotCount)
	{
		if (slotCount < DesiredSlots)
		{
			slotCount = DesiredSlots;
		}
	}

	[HarmonyPatch(typeof(XUiC_Backpack), "OnOpen")]
	[HarmonyPrefix]
	private static void XUiC_Backpack_OnOpen_Prefix(XUiC_Backpack __instance)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Expected O, but got Unknown
		object obj;
		if (__instance == null)
		{
			obj = null;
		}
		else
		{
			XUi xui = ((XUiController)__instance).xui;
			if ((Object)(object)xui == (Object)null)
			{
				obj = null;
			}
			else
			{
				LocalPlayerUI playerUI = xui.playerUI;
				obj = (((Object)(object)playerUI != (Object)null && (Object)(object)playerUI.entityPlayer != (Object)null) ? ((EntityAlive)playerUI.entityPlayer).bag : null);
			}
		}
		Bag val = (Bag)obj;
		if (val != null)
		{
			EnsureBagSize(val);
		}
	}

	[HarmonyPatch(typeof(XUiC_BackpackWindow), "OnOpen")]
	[HarmonyPrefix]
	private static void XUiC_BackpackWindow_OnOpen_Prefix(XUiC_BackpackWindow __instance)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		try
		{
			object obj;
			if (__instance == null)
			{
				obj = null;
			}
			else
			{
				XUi xui = ((XUiController)__instance).xui;
				if ((Object)(object)xui == (Object)null)
				{
					obj = null;
				}
				else
				{
					LocalPlayerUI playerUI = xui.playerUI;
					obj = (((Object)(object)playerUI != (Object)null) ? playerUI.entityPlayer : null);
				}
			}
			EntityPlayerLocal val = (EntityPlayerLocal)obj;
			if (val != null)
			{
				BackpackWindowLocalPlayerRef.Invoke(__instance) = val;
				if (((EntityAlive)val).bag != null)
				{
					EnsureBagSize(((EntityAlive)val).bag);
				}
			}
		}
		catch (Exception arg)
		{
			Debug.LogError((object)$"[Steel405 Rebuild] BackpackWindow.OnOpen sync failed: {arg}");
		}
	}

	private static void XUiM_PlayerInventory_Ctor_Postfix(XUiM_PlayerInventory __instance, EntityPlayerLocal _player)
	{
		try
		{
			if ((Object)(object)_player != (Object)null && ((EntityAlive)_player).bag != null)
			{
				EnsureBagSize(((EntityAlive)_player).bag);
			}
		}
		catch (Exception arg)
		{
			Debug.LogError((object)$"[Steel405 Rebuild] XUiM_PlayerInventory ctor sync failed: {arg}");
		}
	}

	[HarmonyPatch(typeof(XUiC_TabSelector), "Update")]
	[HarmonyPostfix]
	private static void XUiC_TabSelector_Update_Postfix(XUiC_TabSelector __instance)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!ShouldHandleWheelPaging(__instance))
			{
				return;
			}
			float num = Input.mouseScrollDelta.y;
			if (Mathf.Abs(num) < 0.01f)
			{
				num = Input.GetAxis("Mouse ScrollWheel");
			}
			if (!(Mathf.Abs(num) < 0.01f))
			{
				int num2 = ((!(num > 0f)) ? 1 : (-1));
				if (ModApi.Config.InvertWheelPagingDirection)
				{
					num2 *= -1;
				}
				MethodInfo methodInfo = AccessTools.Method(typeof(XUiC_TabSelector), "ToggleCategory", new Type[2]
				{
					typeof(int),
					typeof(bool)
				}, (Type[])null);
				if (methodInfo == null)
				{
					Debug.LogWarning((object)"[Steel405 Rebuild] Could not find XUiC_TabSelector.ToggleCategory(int, bool) for wheel paging.");
					return;
				}
				methodInfo.Invoke(__instance, new object[2] { num2, true });
				_nextAllowedWheelPagingTime = Time.unscaledTime + Mathf.Max(0.01f, ModApi.Config.WheelPagingCooldownSeconds);
			}
		}
		catch (Exception arg)
		{
			Debug.LogError((object)$"[Steel405 Rebuild] Wheel paging failed: {arg}");
		}
	}
}
