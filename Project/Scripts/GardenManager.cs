using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GardenPlanner.Database;

public partial class GardenManager : Node2D
{
	private PackedScene plantScene;
	private List<Plant> plantedPlants = new List<Plant>();

	private List<PlantTypeData> availablePlantTypes = new List<PlantTypeData>();
	private List<SoilData> availableSoilTypes = new List<SoilData>();
	private List<GrowthRule> availablleGrowthRules = new List<GrowthRule>();

	private SoilData currentSoilType;
	
	// UI
	private VBoxContainer plantListContainer;
	private Sprite2D ghostSprite;
	private Button simulationButton;
	private Button stopButton;
	private Label dateLabel;
	
	private PlantTypeData? selectedPlantType = null;

	private int totalYearsToSimulate = 10;
	private int simulationStepMonths = 1;
	private int currentSimulationMonth = 0;
	private float simulationSpeedSeconds = 0.5f;
	private Timer simulationTimer;
	private bool isSimulationRunning = false;

	private DatabaseManager dbManager;
	
	public override async void _Ready()
	{
		GD.Print("Inicjalizacja Godot Garden Manager ...");

		dbManager = new DatabaseManager();
		AddChild(dbManager);

		await LoadInitialDataAsync();
		
		plantScene = ResourceLoader.Load<PackedScene>("res://Scenes/Plant.tscn");

		if (plantScene == null)
		{
			GD.PrintErr("Błąd: Nie można załadować sceny");
			return;
		}
		plantListContainer = GetNode<VBoxContainer>("CanvasLayer/Panel/PlantList");

		if (plantListContainer != null)
		{
			GeneratePlantButtons();
		}
		else
		{
			GD.PrintErr("BŁĄD: Nie znaleziono Vbox Container (PlantList) w UI. Sprawdź ścieżkę");
		}
		
		dateLabel = GetNodeOrNull<Label>("CanvasLayer/DateLabel");
		if (dateLabel == null)
		{
			GD.PrintErr("BŁĄD: Nie znaleziono CanvasLayer/DateLabel");
		}
		else
		{
			UpdateDateLabel(0);
		}

		CreateGhostSprite();
		SetupSimulationTimer();
		SetupControlButtons();
	}

	private async Task LoadInitialDataAsync()
	{
		GD.Print("[DB] Rozpoczecie ładaowania danych...");

		Task<List<PlantTypeData>> plantsTask = dbManager.LoadPlantTypesAsync();
		Task<List<SoilData>> soilsTask = dbManager.LoadSoilDataAsync();
		Task<List<GrowthRule>> rulesTask = dbManager.LoadGrowthRulesAsync();
		
		await Task.WhenAll(plantsTask, soilsTask, rulesTask);

		availablePlantTypes = plantsTask.Result;
		availableSoilTypes = soilsTask.Result;
		availablleGrowthRules = rulesTask.Result;

		if (availableSoilTypes.Count > 0)
		{
			currentSoilType = availableSoilTypes[0];
		}
		else
		{
			GD.PrintErr("[DB ERROR] Nie załadowano typów gleby. Używam danych statystycznych.");
			currentSoilType = InitialData.SoilTypes[0];
			availablePlantTypes = InitialData.PlantTypes;
			availableSoilTypes = InitialData.SoilTypes;
			availablleGrowthRules = InitialData.GrowthRules;
		}
		
		GD.Print("[DB] Ładowanie danych zakończone");
	}

	private void SetupSimulationTimer()
	{
		simulationTimer = new  Timer();
		simulationTimer.WaitTime = simulationSpeedSeconds;
		simulationTimer.OneShot = false;
		simulationTimer.Timeout += OnSimulationTimerTimeout;
		AddChild(simulationTimer);
	}

	private void SetupControlButtons()
	{
		simulationButton = GetNodeOrNull<Button>("Control/SimulationButton");
		if (simulationButton != null)
		{
			simulationButton.Text = "Rozpocznij Symulację";
			
			simulationButton.Pressed -= OnSimulationButtonPressed;
			simulationButton.Pressed += OnSimulationButtonPressed;
		}
		else
		{
			GD.PrintErr("BŁĄD: Nie znaleziono przycisku Control/SimulationButton");
		}
		stopButton = GetNodeOrNull<Button>("Control/StopButton");
		if (stopButton != null)
		{
			stopButton.Text = "Stop";
			stopButton.Visible = false;
			
			stopButton.Pressed -= OnStopButtonPressed;
			stopButton.Pressed += OnStopButtonPressed;
		}
		else
		{
			GD.PrintErr("BŁĄD: Nie znaleziono przycisku Control/StopButton");
		}
	}

	private void CreateGhostSprite()
	{
		ghostSprite = new Sprite2D();
		ghostSprite.Modulate = new Color(1, 1, 1, 0.5f);
		ghostSprite.Visible = false;
		ghostSprite.Scale = new Vector2(0.5f, 0.5f);
		ghostSprite.ZIndex = 100;
		AddChild(ghostSprite);
	}

	private void GeneratePlantButtons()
{
	if (plantListContainer == null)
	{
		GD.PrintErr("BŁĄD KRYTYCZNY: Zmienna 'plantListContainer' jest NULL.");
		GD.PrintErr("Rozwiązanie: Jeśli używasz [Export], przeciągnij węzeł VBoxContainer do pola w Inspektorze.");
		GD.PrintErr("Rozwiązanie: Jeśli używasz GetNode, sprawdź czy ścieżka jest poprawna.");
		return;
	}
	
	if (InitialData.PlantTypes == null)
	{
		GD.PrintErr("BŁĄD KRYTYCZNY: Lista 'InitialData.PlantTypes' nie istnieje (jest null). Sprawdź plik DataModels.cs.");
		return;
	}

	GD.Print($"Generowanie przycisków... Znaleziono {InitialData.PlantTypes.Count} roślin.");

	foreach (var child in plantListContainer.GetChildren())
	{
		child.QueueFree();
	}
	
	foreach (var plantData in InitialData.PlantTypes)
	{
		var btn = new Button();
		btn.Text = plantData.Name;
		btn.CustomMinimumSize = new Vector2(0, 40);
		
		var pData = plantData; 
		btn.Pressed += () => SelectPlantToPlace(pData);

		plantListContainer.AddChild(btn);
	}
}

	private void SelectPlantToPlace(PlantTypeData plantType)
	{
		selectedPlantType = plantType;
		GD.Print($"Wybrano do sadzenia: {plantType.Name}");

		if (ResourceLoader.Exists(plantType.TexturePath))
		{
			ghostSprite.Texture = (Texture2D)ResourceLoader.Load(plantType.TexturePath);
			ghostSprite.Visible = true;
		}
	}

	private void CancelPlacement()
	{
		selectedPlantType = null;
		ghostSprite.Visible = false;
		GD.Print("Anulowano sadzenie");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion)
		{
			if (selectedPlantType != null && ghostSprite.Visible)
			{
				ghostSprite.GlobalPosition = GetGlobalMousePosition();
			}
		}

		if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left)
			{
				if (selectedPlantType != null)
				{
					PlacePlant(selectedPlantType.Value, GetGlobalMousePosition());
				}
			}
			else if (mouseButton.ButtonIndex == MouseButton.Right)
			{
				CancelPlacement();
			}
		}
	}
	
	public void PlacePlant(PlantTypeData typeData, Vector2 position)
	{
		if (plantScene == null) return;

		var newPlant = plantScene.Instantiate<Plant>();
		AddChild(newPlant);

		newPlant.Position = position;
		newPlant.Initialize(typeData);

		if (newPlant.GetNode<CollisionShape2D>("CollisionShape2D").Shape == null)
		{
			GD.PrintErr("Błąd: Roslina nie ma ustawionego CircleShape2D");
			newPlant.GetNode<CollisionShape2D>("CollisionSahpe2D").Shape = new CircleShape2D();
		}
		
		plantedPlants.Add(newPlant);
		GD.Print($"Posadzono {typeData.Name} na pozycji {position}.");
	}
	
	private void OnSimulationButtonPressed()
	{
		if (isSimulationRunning) return;
		StartSimulation();
	}
	
	private void OnStopButtonPressed()
	{
		StopSimulation();
	}

	private void StartSimulation()
	{
		if (plantedPlants.Count == 0)
		{
			GD.Print("Brak roślin do symulacji");
			return;
		}
		isSimulationRunning = true;
		simulationButton.Disabled = true;
		stopButton.Visible = true;
		
		simulationTimer.Start();
		GD.Print($"--- Rozpoczecie symulacji ---");
	}
	
	private void StopSimulation()
	{
		isSimulationRunning = false;
		simulationTimer.Stop();
		
		if(simulationButton != null) simulationButton.Disabled = false;
		if(stopButton != null) stopButton.Visible = false;
		
		GD.Print($"--- Zatrzymanie symulacji ---");
	}
	
	private void OnSimulationTimerTimeout()
	{
		currentSimulationMonth++;
		SimulateStep(currentSimulationMonth);
		UpdateDateLabel(currentSimulationMonth);
		
		if (currentSimulationMonth >= totalYearsToSimulate * 12)
		{
			StopSimulation();
			GD.Print("Koniec symulacji (osiągnięto limit czasu).");
		}
	}
	
	private void UpdateDateLabel(int total_months)
	{
		if (dateLabel == null) return;

		int year = (total_months -1) / 12 + 1;
		int monthInYear = ((total_months -1) % 12) + 1;

		dateLabel.Text = $"Rok: {year}, Miesiąc: {monthInYear}";
	}

	private void SimulateStep(int month)
	{
		int year = (month -1) / 12 + 1;
		int monthInYear = ((month -1) % 12) + 1;
		
		GD.Print($"--- Symulacja Rok {year}, Miesiąc {monthInYear} ---");

		foreach (var plantA in plantedPlants)
		{
			float sunLevel = CalculateSunLevel(plantA);
			float growthFactorSoil = GetGrowthFactor(plantA);
			plantA.SimulateMonth(growthFactorSoil, sunLevel);
		}
	}

	private float GetGrowthFactor(Plant plant)
	{
		var rule = InitialData.GrowthRules.FirstOrDefault(r =>
			r.PlantTypeId == plant.TypeData.Id && r.SoilTypeId == currentSoilType.Id);

		return rule.GrowthMultiplier > 0 ? rule.GrowthMultiplier : 1.0f;
	}

	private float CalculateSunLevel(Plant plantB)
	{
		float sunLevel = 1.0f;

		foreach (var plantA in plantedPlants)
		{
			if (plantA == plantB) continue;

			if (plantA.CurrentHeight > plantB.CurrentHeight)
			{
				float distance = plantA.Position.DistanceTo(plantB.Position);
				float combinedRadius = plantA.CurrentRadius + 0.5f;

				if (distance < combinedRadius)
				{
					float heightDifference = plantA.CurrentHeight - plantB.CurrentHeight;
					float shadowImpact = Mathf.Min(heightDifference / 0.5f, 0.8f);
					
					sunLevel -= shadowImpact;
				}
			}
		}
		
		sunLevel = Mathf.Max(0.2f, sunLevel);

		float preferenceImpact = Mathf.Abs(sunLevel * 10.0f - plantB.TypeData.SunPreference) / 10.0f;

		return sunLevel * (1.0f - preferenceImpact / 2.0f);
	}
}
