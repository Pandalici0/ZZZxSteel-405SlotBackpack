using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SteelUI450SlotsBackpack;

[HarmonyPatch]
public static class BackpackPatches
{
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
            return false;

        if (tabSelector == null)
            return false;

        if (config.RequireShiftForWheelPaging && !IsShiftHeld())
            return false;

        if (Time.unscaledTime < _nextAllowedWheelPagingTime)
            return false;

        return true;
    }

    private static ItemStack[] NormalizeSlots(ItemStack[] original, int desired)
    {
        desired = Math.Max(1, desired);

        if (original == null)
            return ItemStack.CreateArray(desired);

        if (original.Length == desired)
            return original;

        ItemStack[] array = ItemStack.CreateArray(desired);
        int num = Math.Min(original.Length, desired);

        for (int i = 0; i < num; i++)
            array[i] = original[i] != null ? original[i].Clone() : ItemStack.Empty.Clone();

        return array;
    }

    private static FieldInfo FindField(Type type, string fieldName)
    {
        while (type != null)
        {
            FieldInfo field = AccessTools.Field(type, fieldName);
            if (field != null)
                return field;

            type = type.BaseType;
        }

        return null;
    }

    private static ItemStack[] GetBagItems(Bag bag)
    {
        if (bag == null)
            return null;

        try
        {
            FieldInfo field = FindField(bag.GetType(), "items");
            return field?.GetValue(bag) as ItemStack[];
        }
        catch (Exception e)
        {
            Debug.LogError($"[Steel Backpack] GetBagItems failed: {e}");
            return null;
        }
    }

    private static void SetBagItems(Bag bag, ItemStack[] items)
    {
        if (bag == null)
            return;

        try
        {
            FieldInfo field = FindField(bag.GetType(), "items");
            field?.SetValue(bag, items);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Steel Backpack] SetBagItems failed: {e}");
        }
    }

    private static void SetBackpackWindowLocalPlayer(XUiC_BackpackWindow window, EntityPlayerLocal player)
    {
        if (window == null)
            return;

        try
        {
            FieldInfo field = FindField(window.GetType(), "localPlayer");
            field?.SetValue(window, player);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Steel Backpack] SetBackpackWindowLocalPlayer failed: {e}");
        }
    }

    private static void EnsureBagSize(Bag bag)
    {
        try
        {
            ItemStack[] currentItems = GetBagItems(bag);
            ItemStack[] resizedItems = NormalizeSlots(currentItems, DesiredSlots);

            if (!ReferenceEquals(currentItems, resizedItems))
                SetBagItems(bag, resizedItems);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Steel Backpack] EnsureBagSize failed: {e}");
        }
    }

    [HarmonyPatch(typeof(Bag), "checkBagAssigned")]
    [HarmonyPrefix]
    private static void Bag_CheckBagAssigned_Prefix(ref int slotCount)
    {
        if (slotCount < DesiredSlots)
            slotCount = DesiredSlots;
    }

    [HarmonyPatch(typeof(XUiC_Backpack), "OnOpen")]
    [HarmonyPrefix]
    private static void XUiC_Backpack_OnOpen_Prefix(XUiC_Backpack __instance)
    {
        try
        {
            var bag = __instance?.xui?.playerUI?.entityPlayer?.bag;
            if (bag != null)
                EnsureBagSize(bag);
        }
        catch (Exception e)
        {
            Debug.LogError($"[Steel Backpack] OnOpen failed: {e}");
        }
    }

    [HarmonyPatch(typeof(XUiC_BackpackWindow), "OnOpen")]
    [HarmonyPrefix]
    private static void XUiC_BackpackWindow_OnOpen_Prefix(XUiC_BackpackWindow __instance)
    {
        try
        {
            var player = __instance?.xui?.playerUI?.entityPlayer;
            if (player != null)
            {
                SetBackpackWindowLocalPlayer(__instance, player);
                if (player.bag != null)
                    EnsureBagSize(player.bag);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Steel Backpack] BackpackWindow.OnOpen failed: {e}");
        }
    }

    [HarmonyPatch(typeof(XUiC_TabSelector), "Update")]
    [HarmonyPostfix]
    private static void XUiC_TabSelector_Update_Postfix(XUiC_TabSelector __instance)
    {
        try
        {
            if (!ShouldHandleWheelPaging(__instance))
                return;

            float delta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(delta) < 0.01f)
                delta = Input.GetAxis("Mouse ScrollWheel");

            if (Mathf.Abs(delta) < 0.01f)
                return;

            int direction = delta > 0 ? -1 : 1;

            if (ModApi.Config != null && ModApi.Config.InvertWheelPagingDirection)
                direction *= -1;

            MethodInfo method = AccessTools.Method(
                typeof(XUiC_TabSelector),
                "ToggleCategory",
                new Type[] { typeof(int), typeof(bool) });

            method?.Invoke(__instance, new object[] { direction, true });

            float cooldown = 0.05f;
            if (ModApi.Config != null)
                cooldown = Mathf.Max(0.01f, ModApi.Config.WheelPagingCooldownSeconds);

            _nextAllowedWheelPagingTime = Time.unscaledTime + cooldown;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Steel Backpack] Wheel paging failed: {e}");
        }
    }
}
