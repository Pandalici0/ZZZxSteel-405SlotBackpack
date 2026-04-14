using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using UnityEngine;

namespace SteelUI450SlotsBackpack;

[DataContract]
public sealed class BackpackConfig
{
	[DataMember]
	public bool ShiftClickMovesToCurrentPage { get; set; } = true;

	[DataMember]
	public bool RememberLastOpenedPage { get; set; } = false;

	[DataMember]
	public bool HoldShiftToScroll { get; set; } = false;

	[DataMember]
	public int SlotsPerPage { get; set; } = 45;

	[DataMember]
	public int TabsCount { get; set; } = 9;

	[DataMember]
	public bool WheelPagingEnabled { get; set; } = true;

	[DataMember]
	public bool RequireShiftForWheelPaging { get; set; } = false;

	[DataMember]
	public bool InvertWheelPagingDirection { get; set; } = false;

	[DataMember]
	public float WheelPagingCooldownSeconds { get; set; } = 0.08f;

	public int TotalSlots => Math.Max(1, SlotsPerPage) * Math.Max(1, TabsCount);

	public static BackpackConfig Load(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				BackpackConfig backpackConfig = LoadJson<BackpackConfig>(path);
				if (backpackConfig != null)
				{
					backpackConfig.SlotsPerPage = Math.Max(1, backpackConfig.SlotsPerPage);
					backpackConfig.TabsCount = Math.Max(1, backpackConfig.TabsCount);
					return backpackConfig;
				}
			}
		}
		catch (Exception arg)
		{
			Debug.LogWarning((object)$"[Steel405 Rebuild] Failed to load config.json: {arg}");
		}
		return new BackpackConfig();
	}

	public static T LoadJson<T>(string path)
	{
		using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(typeof(T));
		return (T)dataContractJsonSerializer.ReadObject((Stream)stream);
	}
}
