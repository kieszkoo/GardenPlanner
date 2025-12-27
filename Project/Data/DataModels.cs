using Godot;
using System.Collections.Generic;

/// <summary>
/// Definicja gleby i jej parametrów wpływających na wzrost.
/// Docelowo wczytywane z bazy danych.
/// </summary>
public struct SoilData
{
	public int Id;
	public string Name;
	public float WaterRetention;
	public float NutrientLevel;
}

/// <summary>
/// Definicja gatunku rośliny.
/// Docelowo wczytywane z bazy danych.
/// </summary>
public struct PlantTypeData
{
	public int Id;
	public string Name;
	public string TexturePath;
	public float MaxHeight; //Maksymalna wysokosc (m) 
	public float MaxWidth; //Maksymalna szerokosc korony (m)
	public float RootDepth; //Glebokosc korzeni (m)
	public float CanopyRadius; //Poczatkowy promien okregu (m)
	public float SunPreference; //Preferencje slonca (0.0 - cien, 10.0 - slonce)
	public int TypeId; //ID typu rosliny
	public float BaseGrowthRate; //Bazowa miesieczna szybkoscc wzrostu
	
}

/// <summary>
/// Dane wzrostu dla konkretnej pary Roślina/Gleba.
/// Docelowo wczytywane z bazy danych.
/// </summary>
public struct GrowthRule{
	public int PlantTypeId;
	public int SoilTypeId;
	public float GrowthMultiplier; //Multiplikator wzrostu
}

/// <summary>
/// Klasa przechowująca przykładowe dane w aplikacji, zanim podłączymy bazę.
/// </summary>

public static class InitialData
{
	public static readonly List<SoilData> SoilTypes = new List<SoilData>
	{
		new SoilData { Id = 1, Name = "Gliniasta", WaterRetention = 0.85f, NutrientLevel = 0.70f },
		new SoilData { Id = 2, Name = "Piaskowa", WaterRetention = 0.20f, NutrientLevel = 0.30f },
		new SoilData { Id = 3, Name = "Żyzna", WaterRetention = 0.60f, NutrientLevel = 0.95f }
	};

	public static readonly List<PlantTypeData> PlantTypes = new List<PlantTypeData>
	{
		new PlantTypeData
		{
			Id = 1, Name = "Róża", TexturePath = "res://Textures/Plants/rose.jpg", MaxHeight = 1.5f,
			MaxWidth = 1.0f, RootDepth = 0.5f, CanopyRadius = 0.5f, SunPreference = 8.0f, TypeId = 1, BaseGrowthRate = 0.05f
		},
		new PlantTypeData
		{
			Id = 2, Name = "Dąb", TexturePath = "res://Textures/Plants/oak.jpg", MaxHeight = 25.0f,
			MaxWidth = 15.0f, RootDepth = 3.0f, CanopyRadius = 7.5f, SunPreference = 6.0f, TypeId = 1, BaseGrowthRate = 0.15f
		},
		new PlantTypeData
		{
			Id = 3, Name = "Paproć", TexturePath = "res://Textures/Plants/fern.jpg", MaxHeight = 0.7f,
			MaxWidth = 0.8f, RootDepth = 0.3f, CanopyRadius = 0.4f, SunPreference = 2.0f, TypeId = 2, BaseGrowthRate = 0.04f
		}
	};

	public static readonly List<GrowthRule> GrowthRules = new List<GrowthRule>
	{
		new GrowthRule { PlantTypeId = 1, SoilTypeId = 1, GrowthMultiplier = 1.10f },
		new GrowthRule { PlantTypeId = 1, SoilTypeId = 2, GrowthMultiplier = 0.70f },
		new GrowthRule { PlantTypeId = 2, SoilTypeId = 3, GrowthMultiplier = 1.25f },
		new GrowthRule { PlantTypeId = 3, SoilTypeId = 3, GrowthMultiplier = 1.05f }
	};
}
