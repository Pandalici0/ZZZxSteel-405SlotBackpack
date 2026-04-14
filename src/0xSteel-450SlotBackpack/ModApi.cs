using System;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace SteelUI450SlotsBackpack;

public class ModApi : IModApi
{
	private static readonly string ConfigPath = Path.Combine(Path.GetDirectoryName(typeof(ModApi).Assembly.Location) ?? ".", "config.json");

	private static Harmony _harmony;

	public static BackpackConfig Config { get; private set; }

	public void InitMod(Mod _modInstance)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		Config = BackpackConfig.Load(ConfigPath);
		Debug.Log((object)$"[Steel405 Rebuild] InitMod called. TotalSlots={Config.TotalSlots}");
		if (_harmony != null)
		{
			return;
		}
		try
		{
			_harmony = new Harmony("steelui.backpack.rebuild.v26");
			_harmony.PatchAll(typeof(ModApi).Assembly);
			Debug.Log((object)"[Steel405 Rebuild] Harmony patches applied.");
		}
		catch (Exception arg)
		{
			Debug.LogError((object)$"[Steel405 Rebuild] Failed to patch: {arg}");
		}
	}
}
