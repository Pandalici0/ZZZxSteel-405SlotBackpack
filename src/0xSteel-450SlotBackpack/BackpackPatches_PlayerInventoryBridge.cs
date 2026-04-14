using System;
using HarmonyLib;

namespace SteelUI450SlotsBackpack;

internal static class BackpackPatches_PlayerInventoryBridge
{
	public static void Invoke(XUiM_PlayerInventory __instance, EntityPlayerLocal _player)
	{
		AccessTools.Method(typeof(BackpackPatches), "XUiM_PlayerInventory_Ctor_Postfix", new Type[2]
		{
			typeof(XUiM_PlayerInventory),
			typeof(EntityPlayerLocal)
		}, (Type[])null)?.Invoke(null, new object[2] { __instance, _player });
	}
}
