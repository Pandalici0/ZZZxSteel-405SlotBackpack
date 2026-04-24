using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SteelUI450SlotsBackpack;

[HarmonyPatch]
internal static class DeathBackpackPaging
{
    // Stable version: keeps 405-slot paging + Shift mouse wheel, but does not override loot slot change handling.
    // 405 slots total = 9 pages x 45 visible slots.
    private const int SlotsPerPage = 45;

    private static readonly Dictionary<int, int> ContainerPageByEntityId = new Dictionary<int, int>();
    private static float _nextPageInputTime;

    private static bool _moveAllPagesRunning;
    private static MethodInfo _moveAllMethod;


    private static FieldInfo _itemControllersField;
    private static FieldInfo _itemsField;
    private static FieldInfo _localTileEntityField;
    private static PropertyInfo _gridCellSizeProperty;

    private static FieldInfo ItemControllersField
    {
        get
        {
            if (_itemControllersField == null)
                _itemControllersField = AccessTools.Field(typeof(XUiC_ItemStackGrid), "itemControllers");
            return _itemControllersField;
        }
    }

    private static FieldInfo ItemsField
    {
        get
        {
            if (_itemsField == null)
                _itemsField = AccessTools.Field(typeof(XUiC_ItemStackGrid), "items");
            return _itemsField;
        }
    }

    private static FieldInfo LocalTileEntityField
    {
        get
        {
            if (_localTileEntityField == null)
                _localTileEntityField = AccessTools.Field(typeof(XUiC_LootContainer), "localTileEntity");
            return _localTileEntityField;
        }
    }

    private static PropertyInfo GridCellSizeProperty
    {
        get
        {
            if (_gridCellSizeProperty == null)
                _gridCellSizeProperty = AccessTools.Property(typeof(XUiC_LootContainer), "GridCellSize");
            return _gridCellSizeProperty;
        }
    }

    private static XUiC_ItemStack[] GetItemControllers(XUiC_LootContainer container)
    {
        return ItemControllersField != null ? ItemControllersField.GetValue(container) as XUiC_ItemStack[] : null;
    }

    private static void SetUiItems(XUiC_LootContainer container, ItemStack[] items)
    {
        if (ItemsField != null)
            ItemsField.SetValue(container, items);
    }

    private static ITileEntityLootable GetLocalTileEntity(XUiC_LootContainer container)
    {
        return LocalTileEntityField != null ? LocalTileEntityField.GetValue(container) as ITileEntityLootable : null;
    }

    private static Vector2i GetGridCellSize(XUiC_LootContainer container)
    {
        if (GridCellSizeProperty != null)
        {
            object value = GridCellSizeProperty.GetValue(container, null);
            if (value is Vector2i)
                return (Vector2i)value;
        }

        return new Vector2i(72, 72);
    }

    private static int GetEntityId(ITileEntityLootable lootable)
    {
        return lootable != null ? lootable.EntityId : -1;
    }

    private static int GetMaxPage(ITileEntityLootable lootable)
    {
        if (lootable == null || lootable.items == null || lootable.items.Length <= 0)
            return 0;

        return Math.Max(0, (lootable.items.Length - 1) / SlotsPerPage);
    }

    private static int GetCurrentPage(ITileEntityLootable lootable)
    {
        int id = GetEntityId(lootable);
        if (id < 0)
            return 0;

        int page;
        if (!ContainerPageByEntityId.TryGetValue(id, out page))
            page = 0;

        int maxPage = GetMaxPage(lootable);

        if (page > maxPage)
            page = maxPage;

        if (page < 0)
            page = 0;

        return page;
    }

    private static void SetCurrentPage(ITileEntityLootable lootable, int page)
    {
        int id = GetEntityId(lootable);
        if (id < 0)
            return;

        int maxPage = GetMaxPage(lootable);
        page = Mathf.Clamp(page, 0, maxPage);
        ContainerPageByEntityId[id] = page;
    }

    private static bool IsPagedDeathBackpack(ITileEntityLootable lootable, XUiC_LootContainer container)
    {
        if (lootable == null || lootable.items == null)
            return false;

        XUiC_ItemStack[] controllers = GetItemControllers(container);
        if (controllers == null || controllers.Length == 0)
            return false;

        return lootable.items.Length > SlotsPerPage;
    }

    private static void RebindVisiblePage(XUiC_LootContainer container)
    {
        if (container == null)
            return;

        ITileEntityLootable lootable = GetLocalTileEntity(container);
        if (lootable == null || lootable.items == null)
            return;

        XUiC_ItemStack[] controllers = GetItemControllers(container);
        if (controllers == null || controllers.Length == 0)
            return;

        int currentPage = GetCurrentPage(lootable);
        int start = currentPage * SlotsPerPage;
        int visibleCount = Math.Min(SlotsPerPage, controllers.Length);

        XUiC_ItemInfoWindow infoWindow = container.xui.GetChildByType<XUiC_ItemInfoWindow>();
        ItemStack[] backendItems = lootable.items;

        // Keep the real backend item array available to the base grid.
        SetUiItems(container, backendItems);

        for (int i = 0; i < controllers.Length; i++)
        {
            XUiC_ItemStack ctrl = controllers[i];
            if (ctrl == null)
                continue;

            ctrl.SlotChangedEvent -= container.HandleLootSlotChangedEvent;
            ctrl.InfoWindow = infoWindow;
            ctrl.StackLocation = XUiC_ItemStack.StackLocationTypes.LootContainer;

            int absoluteIndex = start + i;
            bool visible = i < visibleCount && absoluteIndex < backendItems.Length;

            if (visible)
            {
                ctrl.SlotNumber = absoluteIndex;
                ctrl.ForceSetItemStack(backendItems[absoluteIndex].Clone());
                ctrl.ViewComponent.IsVisible = true;
                ctrl.SlotChangedEvent += container.HandleLootSlotChangedEvent;
            }
            else
            {
                ctrl.SlotNumber = absoluteIndex;
                ctrl.ItemStack = ItemStack.Empty.Clone();
                ctrl.ViewComponent.IsVisible = false;
            }
        }

        XUiV_Grid grid = container.viewComponent as XUiV_Grid;
        if (grid != null)
        {
            grid.Columns = 9;
            grid.Rows = 5; // 9 x 5 = 45 visible slots
            Vector2i cellSize = GetGridCellSize(container);
            grid.CellWidth = cellSize.x;
            grid.CellHeight = cellSize.y;
        }

        container.windowGroup.Controller.SetAllChildrenDirty();
        container.IsDirty = true;

        Debug.Log("[Steel] Death backpack page " + (currentPage + 1) + "/" + (GetMaxPage(lootable) + 1) +
                  " bound. start=" + start + ", visible=" + visibleCount + ", total=" + backendItems.Length);
    }

    private static XUiC_LootContainer TryFindOpenLootContainer(XUi xui)
    {
        if (xui == null)
            return null;

        XUiC_LootContainer lootWindow = xui.GetChildByType<XUiC_LootContainer>();
        if (lootWindow == null)
            return null;

        if (lootWindow.IsDormant)
            return null;

        return lootWindow;
    }

    [HarmonyPatch(typeof(XUiC_LootContainer), "OnOpen")]
    [HarmonyPostfix]
    private static void XUiC_LootContainer_OnOpen_Postfix(XUiC_LootContainer __instance)
    {
        try
        {
            ITileEntityLootable lootable = GetLocalTileEntity(__instance);
            if (!IsPagedDeathBackpack(lootable, __instance))
                return;

            SetCurrentPage(lootable, 0);
            RebindVisiblePage(__instance);
        }
        catch (Exception e)
        {
            Debug.LogError("[Steel] Loot paging OnOpen failed: " + e);
        }
    }

    [HarmonyPatch(typeof(XUiC_LootContainer), "OnClose")]
    [HarmonyPostfix]
    private static void XUiC_LootContainer_OnClose_Postfix(XUiC_LootContainer __instance)
    {
        try
        {
            ITileEntityLootable lootable = GetLocalTileEntity(__instance);
            int id = GetEntityId(lootable);
            if (id >= 0)
                ContainerPageByEntityId.Remove(id);
        }
        catch (Exception e)
        {
            Debug.LogError("[Steel] Loot paging OnClose failed: " + e);
        }
    }

    [HarmonyPatch(typeof(XUiC_LootContainer), "SetSlots")]
    [HarmonyPrefix]
    private static bool XUiC_LootContainer_SetSlots_Prefix(XUiC_LootContainer __instance, ITileEntityLootable lootContainer, ItemStack[] stackList)
    {
        try
        {
            if (stackList == null)
                return false;

            if (!IsPagedDeathBackpack(lootContainer, __instance))
                return true;

            if (LocalTileEntityField != null)
                LocalTileEntityField.SetValue(__instance, lootContainer);

            SetUiItems(__instance, lootContainer.items);

            if (!ContainerPageByEntityId.ContainsKey(lootContainer.EntityId))
                ContainerPageByEntityId[lootContainer.EntityId] = 0;

            RebindVisiblePage(__instance);
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError("[Steel] Loot paging SetSlots failed: " + e);
            return true;
        }
    }

    [HarmonyPatch(typeof(XUiC_LootContainer), "OnTileEntityChanged")]
    [HarmonyPrefix]
    private static bool XUiC_LootContainer_OnTileEntityChanged_Prefix(XUiC_LootContainer __instance, ITileEntity _te)
    {
        try
        {
            ITileEntityLootable lootable = GetLocalTileEntity(__instance);
            if (!IsPagedDeathBackpack(lootable, __instance))
                return true;

            RebindVisiblePage(__instance);
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError("[Steel] Loot paging OnTileEntityChanged failed: " + e);
            return true;
        }
    }


    private static MethodInfo MoveAllMethod
    {
        get
        {
            if (_moveAllMethod == null)
                _moveAllMethod = AccessTools.Method(typeof(XUiC_ContainerStandardControls), "MoveAll");

            return _moveAllMethod;
        }
    }

    private static bool IsItemStackEmpty(ItemStack stack)
    {
        return stack == null || stack.Equals(ItemStack.Empty);
    }

    private static int FindFirstNonEmptyPage(ITileEntityLootable lootable)
    {
        if (lootable == null || lootable.items == null)
            return 0;

        int maxPage = GetMaxPage(lootable);

        for (int page = 0; page <= maxPage; page++)
        {
            int start = page * SlotsPerPage;
            int end = Math.Min(start + SlotsPerPage, lootable.items.Length);

            for (int i = start; i < end; i++)
            {
                if (!IsItemStackEmpty(lootable.items[i]))
                    return page;
            }
        }

        return 0;
    }

    private static bool HasAnyItemsOnPage(ITileEntityLootable lootable, int page)
    {
        if (lootable == null || lootable.items == null)
            return false;

        int start = page * SlotsPerPage;
        int end = Math.Min(start + SlotsPerPage, lootable.items.Length);

        for (int i = start; i < end; i++)
        {
            if (!IsItemStackEmpty(lootable.items[i]))
                return true;
        }

        return false;
    }

    [HarmonyPatch(typeof(XUiC_ContainerStandardControls), "MoveAll")]
    [HarmonyPrefix]
    private static bool XUiC_ContainerStandardControls_MoveAll_Prefix(XUiC_ContainerStandardControls __instance)
    {
        try
        {
            if (_moveAllPagesRunning)
                return true;

            if (__instance == null || __instance.xui == null)
                return true;

            XUiC_LootContainer lootWindow = TryFindOpenLootContainer(__instance.xui);
            if (lootWindow == null)
                return true;

            ITileEntityLootable lootable = GetLocalTileEntity(lootWindow);
            if (!IsPagedDeathBackpack(lootable, lootWindow))
                return true;

            MethodInfo moveAll = MoveAllMethod;
            if (moveAll == null)
                return true;

            int originalPage = GetCurrentPage(lootable);
            int maxPage = GetMaxPage(lootable);

            _moveAllPagesRunning = true;

            try
            {
                for (int page = 0; page <= maxPage; page++)
                {
                    if (!HasAnyItemsOnPage(lootable, page))
                        continue;

                    SetCurrentPage(lootable, page);
                    RebindVisiblePage(lootWindow);

                    // Re-enter MoveAll once with the guard enabled.
                    // That lets vanilla move the currently visible 45-slot page.
                    moveAll.Invoke(__instance, null);
                }
            }
            finally
            {
                _moveAllPagesRunning = false;
            }

            int targetPage = FindFirstNonEmptyPage(lootable);
            SetCurrentPage(lootable, targetPage);
            RebindVisiblePage(lootWindow);

            Debug.Log("[Steel] Death backpack MoveAll across all pages finished.");

            return false;
        }
        catch (Exception e)
        {
            _moveAllPagesRunning = false;
            Debug.LogError("[Steel] Death backpack MoveAll across pages failed: " + e);
            return true;
        }
    }

    // XUi.Update does not exist in 7DTD 2.6.
    // XUiC_TabSelector.Update does exist in this mod setup and is already used by the backpack wheel patch.
    [HarmonyPatch(typeof(XUiC_TabSelector), "Update")]
    [HarmonyPostfix]
    private static void XUiC_TabSelector_Update_Postfix(XUiC_TabSelector __instance)
    {
        try
        {
            if (__instance == null || __instance.xui == null)
                return;

            if (Time.unscaledTime < _nextPageInputTime)
                return;

            XUiC_LootContainer lootWindow = TryFindOpenLootContainer(__instance.xui);
            if (lootWindow == null)
                return;

            ITileEntityLootable lootable = GetLocalTileEntity(lootWindow);
            if (!IsPagedDeathBackpack(lootable, lootWindow))
                return;

            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!shift)
                return;

            float delta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(delta) < 0.01f)
                delta = Input.GetAxis("Mouse ScrollWheel");

            if (Mathf.Abs(delta) < 0.01f)
                return;

            int oldPage = GetCurrentPage(lootable);
            int newRequestedPage = oldPage + (delta > 0 ? -1 : 1);

            SetCurrentPage(lootable, newRequestedPage);

            int newPage = GetCurrentPage(lootable);
            if (newPage != oldPage)
            {
                RebindVisiblePage(lootWindow);
                Debug.Log("[Steel] Death backpack page changed to " + (newPage + 1) + "/" + (GetMaxPage(lootable) + 1));
            }

            _nextPageInputTime = Time.unscaledTime + 0.15f;
        }
        catch (Exception e)
        {
            Debug.LogError("[Steel] Death backpack paging input failed: " + e);
        }
    }
}
