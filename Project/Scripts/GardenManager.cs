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
	
	private const float PixelsPerMeter = 50.0f;

	private SoilData currentSoilType;
	
	// UI
	private VBoxContainer plantListContainer;
	private ScrollContainer scrollContainer;
	private OptionButton filterOptionButton;
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
		
		SetupPlantListUI();
		
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
	
	private void SetupPlantListUI()
	{
		var panel = GetNodeOrNull<Panel>("CanvasLayer/Panel");
		if (panel == null)
		{
			GD.PrintErr("BŁĄD: Nie znaleziono CanvasLayer/Panel");
			return;
		}
		
		//PRZYCISK FILTROWANIA
		filterOptionButton = GetNodeOrNull<OptionButton>("CanvasLayer/Panel/FilterOptionButton");
		if (filterOptionButton == null)
		{
			GD.PrintErr("BŁĄD: Nie znaleziono CanvasLayer/Panel/FilterOptionButton");
			return;
		}
		// Ustaw opcje filtrowania
		filterOptionButton.Clear();
		filterOptionButton.AddItem("Wszystkie", 0);
		filterOptionButton.AddItem("Drzewa", 1);
		filterOptionButton.AddItem("Krzewy", 2);
		filterOptionButton.AddItem("Kwiaty", 3);
		filterOptionButton.AddItem("Paprocie", 4);
		filterOptionButton.AddItem("Zioła", 5);
		filterOptionButton.AddItem("Trawy", 6);
		
		if (!filterOptionButton.IsConnected(OptionButton.SignalName.ItemSelected, Callable.From<long>(OnFilterItemSelected)))
		{
			filterOptionButton.ItemSelected += OnFilterItemSelected;
		}
		
		//LISTA ROŚLIN
		plantListContainer = GetNode<VBoxContainer>("CanvasLayer/Panel/ScrollContainer/PlantList");
		if (plantListContainer != null)
		{
			GeneratePlantButtons(0);
		}
		else
		{
			GD.PrintErr("BŁĄD: Nie znaleziono Vbox Container (PlantList) w UI. Sprawdź ścieżkę");
		}
	}
	
	private void OnFilterItemSelected(long index)
	{
		int categoryId = filterOptionButton.GetItemId((int)index);
		GeneratePlantButtons(categoryId);
	}

	private async Task LoadInitialDataAsync()
	{
		GD.Print("[DB] Rozpoczecie ładaowania danych...");

		Task<List<PlantTypeData>> plantsTask = dbManager.LoadPlantTypesAsync();
		Task<List<SoilData>> soilsTask = dbManager.LoadSoilDataAsync();
		
		await Task.WhenAll(plantsTask, soilsTask);

		availablePlantTypes = plantsTask.Result;
		availableSoilTypes = soilsTask.Result;

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
		}
		
		GD.Print("[DB] Ładowanie danych zakończone");
		
		if (plantListContainer != null) GeneratePlantButtons(0);
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

	private void GeneratePlantButtons(int categoryFilter = 0)
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

	GD.Print($"Generowanie przycisków... Znaleziono {availablePlantTypes.Count} roślin.");

	foreach (var child in plantListContainer.GetChildren())
	{
		child.QueueFree();
	}
	
	foreach (var plantData in availablePlantTypes)
	{
		if (categoryFilter != 0)
		{
			if (plantData.TypeId != categoryFilter)	
			{
				continue;
			}
		}
		
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

	private float PixelsToMeters(float pixels)
	{
		return pixels / PixelsPerMeter;
	}

	private float MetersToPixels(float meters)
	{
		return meters * PixelsPerMeter;
	}
	private float GetGrowthFactor(Plant plant)
	{
		float nutrientFactor = 0.5f + currentSoilType.NutrientLevel;	//(0.5 - 1.5) im żyźniejsza gleba tym lepiej
		float prefferedWater = 0.5f;	//Domyślnie 50% wilgotności

		switch (plant.TypeData.TypeId)
		{
			case 1:	//Drzewa
			case 2:	//Krzewy
				prefferedWater = 0.6f;
				break;
			case 3:	//Kwiaty
			case 4:	//Paprocie
				prefferedWater = 0.8f;
				break;
			case 5:	//Zioła	
			case 6: //Trawy
				prefferedWater = 0.3f;
				break;
			default:
				prefferedWater = 0.5f;
				break;
		}
		
		float waterDifference = Mathf.Abs(prefferedWater - currentSoilType.WaterRetention);
		float waterFactor = 1.0f - (waterDifference * 0.5f);	//Różnica wody wpływa do 50% na wzrost
		
		return nutrientFactor * waterFactor;
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
				float shadowRadius = plantA.CurrentRadius * PixelsToMeters(1.0f);
				float distInMeters = distance / 50.0f;	//50 pixeli = 1 metr

				if (distInMeters < shadowRadius)
				{
					float heightDifference = plantA.CurrentHeight - plantB.CurrentHeight;
					float shadowImpact = Mathf.Min(heightDifference / 2.0f, 0.8f);
					
					sunLevel -= shadowImpact;
				}
			}
		}
		
		sunLevel = Mathf.Max(0.1f, sunLevel); //Min 10% swiatla zawsze dociera
		float actualSun = sunLevel * 10.0f;	//Skala 0-10
		float sunDiff = Mathf.Abs(actualSun - plantB.TypeData.SunPreference);
		float sunFactor = 1.0f - (sunDiff / 20.0f);	//Różnica w preferencjach wpływa do 50% na wzrost
		
		return sunFactor;
	}
}
