using System;
using System.Reflection;
using HarmonyLib;

namespace SteelUI450SlotsBackpack;

[HarmonyPatch]
internal class PlayerInventoryCtorPatch
{
	private static MethodBase TargetMethod()
	{
		Type typeFromHandle = typeof(XUiM_PlayerInventory);
		ConstructorInfo[] constructors = typeFromHandle.GetConstructors();
		int num = 0;
		if (num < constructors.Length)
		{
			return constructors[num];
		}
		return null;
	}

	private static void Postfix(XUiM_PlayerInventory __instance, EntityPlayerLocal _player)
	{
		BackpackPatches_PlayerInventoryBridge.Invoke(__instance, _player);
	}
}
