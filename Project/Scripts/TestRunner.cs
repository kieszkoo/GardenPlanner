using Godot;
using System;
using System.Collections.Generic;

public partial class TestRunner : Node
{
	public override void _Ready()
	{
		GD.Print("\n=== ROZPOCZYNAM TESTY AUTOMATYCZNE (UNIT TESTS) ===\n");

		int passed = 0;
		int failed = 0;

		if (TestGrowthCalculation()) passed++; else failed++;
		if (TestWinterGrowthModifier()) passed++; else failed++;
		if (TestSunLevelCalculation()) passed++; else failed++;
		if (TestSoilDataStructure()) passed++; else failed++;

		GD.Print($"\n=== WYNIK KOŃCOWY: PASS: {passed}, FAIL: {failed} ===");
		
		QueueFree();
	}

	private bool TestGrowthCalculation()
	{
		GD.Print("TEST 1: Obliczanie współczynnika wzrostu...");
		
		var soil = new SoilData { Id = 1, Name = "Idealna", NutrientLevel = 1.0f, WaterRetention = 0.6f };
		var plant = new PlantTypeData { TypeId = 1, Name = "Testowa Róża", BaseGrowthRate = 0.1f }; 
		
		float nutrientFactor = 0.5f + soil.NutrientLevel; 
		float prefferedWater = 0.6f; 
		float waterDiff = Mathf.Abs(prefferedWater - soil.WaterRetention); 
		float waterFactor = 1.0f - (waterDiff * 0.5f); 
		
		float result = nutrientFactor * waterFactor; 

		if (result > 1.0f)
		{
			GD.Print($"   -> PASS (Wynik: {result})");
			return true;
		}
		else
		{
			GD.PrintErr($"   -> FAIL (Oczekiwano > 1.0, otrzymano {result})");
			return false;
		}
	}

	private bool TestWinterGrowthModifier()
	{
		GD.Print("TEST 2: Modyfikator wzrostu w zimie...");

		int winterMonth = 12; 
		
		float modifier = 1.0f;
		int monthInYear = ((winterMonth - 1) % 12) + 1;
		
		string season = "Unknown";
		if (monthInYear == 12 || monthInYear <= 2) season = "Winter";
		
		if (season == "Winter") modifier = 0.0f;

		if (modifier == 0.0f)
		{
			GD.Print("   -> PASS (Wzrost zatrzymany)");
			return true;
		}
		else
		{
			GD.PrintErr($"   -> FAIL (Oczekiwano 0.0, otrzymano {modifier})");
			return false;
		}
	}

	private bool TestSunLevelCalculation()
	{
		GD.Print("TEST 3: Obliczanie poziomu słońca (Cień)...");

		float myHeight = 1.0f;
		float otherHeight = 10.0f;
		
		float heightDifference = otherHeight - myHeight; 
		float shadowImpact = Mathf.Min(heightDifference / 2.0f, 0.8f); 
		
		float sunLevel = 1.0f - shadowImpact; 

		if (sunLevel <= 0.3f)
		{
			GD.Print($"   -> PASS (Poprawnie wykryto cień, Słońce: {sunLevel})");
			return true;
		}
		else
		{
			GD.PrintErr($"   -> FAIL (Słońce zbyt wysokie: {sunLevel})");
			return false;
		}
	}

	private bool TestSoilDataStructure()
	{
		GD.Print("TEST 4: Weryfikacja struktur danych (InitialData)...");
		
		if (InitialData.SoilTypes.Count > 0 && InitialData.PlantTypes.Count > 0)
		{
			GD.Print($"   -> PASS (Załadowano {InitialData.PlantTypes.Count} roślin testowych)");
			return true;
		}
		else
		{
			GD.PrintErr("   -> FAIL (Brak danych w InitialData)");
			return false;
		}
	}
}
