using Godot;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using GardenPlanner.Database;

public partial class GardenManager : Node2D
{
	// Konfiguracja
	private Vector2 _gardenSize = new Vector2(10,10);
	private bool _isConfigured = false;
	private const float PixelsPerMeter = 50.0f;
	
	// UI
	private VBoxContainer plantListContainer;
	private ScrollContainer scrollContainer;
	private OptionButton filterOptionButton;
	private Sprite2D ghostSprite;
	private Button simulationButton;
	private Button stopButton;
	private Button saveButton;
	private Button loadButton;
	private Button speedButton;
	private Button resetButton;
	private FileDialog saveFileDialog;
	private FileDialog loadFileDialog;
	private Label dateLabel;
	private Control _setupPanel;
	private OptionButton _soilOptionButton;
	private SpinBox _widthSpinBox;
	private SpinBox _heightSpinBox;
	private Button _confirmSetupButton;
	
	// Logika symulacja
	private PackedScene plantScene;
	private List<Plant> plantedPlants = new List<Plant>();
	
	// Klasa pomocnicza dla instancji dziury w pamięci
	private class BoarHoleInstance
	{
		public Vector2 Position;
		public float Radius;
		public int MonthsLeft;
	}

	// Lista dziur
	private List<BoarHoleInstance> boarHoles = new List<BoarHoleInstance>();

	private List<PlantTypeData> availablePlantTypes = new List<PlantTypeData>();
	private List<SoilData> availableSoilTypes = new List<SoilData>();
	
	private SoilData currentSoilType = InitialData.SoilTypes[0];
	
	private PlantTypeData? selectedPlantType = null;

	private int totalYearsToSimulate = 10;
	private int simulationStepMonths = 1;
	private int currentSimulationMonth = 0;
	private float simulationSpeedSeconds = 0.5f;
	private float timeScale = 1.0f;
	private Timer simulationTimer;
	private bool isSimulationRunning = false;
	private ulong _lastPlantTime = 0;

	// Pory roku
	public enum Season
	{
		Spring,
		Summer,
		Autumn,
		Winter
	};
	
	private Season currentSeason = Season.Spring;
	private Tween _colorTween;



	private DatabaseManager dbManager;
	
	//KAMERA
	private Camera2D _camera;
	private bool _isPanning = false;
	private Vector2 _panStartMousePos;
	private Vector2 _panStartCameraPos;
	private float _zoomSpeed = 0.1f;
	private Vector2 _minZoom = new Vector2(0.1f, 0.1f);
	private Vector2 _maxZoom = new Vector2(3.0f, 3.0f);
	
	public override async void _Ready()
	{
		GD.Print("Inicjalizacja Godot Garden Manager ...");
		
		SetupCamera(); //Inicajalizacja kamery
		
		SetupSaveDialog();
		SetupLoadDialog();
		
		SetupControlButtons();

		dbManager = new DatabaseManager();
		AddChild(dbManager);

		await LoadInitialDataAsync();

		_setupPanel = GetNodeOrNull<Control>("SetupPanel");
		_soilOptionButton = GetNodeOrNull<OptionButton>("SetupPanel/SoilOption");
		_widthSpinBox = GetNodeOrNull<SpinBox>("SetupPanel/Width");
		_heightSpinBox = GetNodeOrNull<SpinBox>("SetupPanel/Height");
		_confirmSetupButton = GetNodeOrNull<Button>("SetupPanel/ConfirmButton");
		
		ApplyCustomTheme();
		
		if (_confirmSetupButton != null)
		{
			_confirmSetupButton.Pressed += OnSetupConfirmed;
		}

		if (_widthSpinBox != null){
			_widthSpinBox.Value = 10;
			_widthSpinBox.MinValue = 2;
			_widthSpinBox.MaxValue = 50;
		}

		if (_heightSpinBox != null){
			_heightSpinBox.Value = 10;
			_heightSpinBox.MinValue = 2;
			_heightSpinBox.MaxValue = 50;
		}

		PopulateSoilOptions();


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
			dateLabel.ZIndex = 200; 

			var style = new StyleBoxFlat();
			style.BgColor = new Color(0.05f, 0.05f, 0.05f, 0.85f); 
			
			style.CornerRadiusTopLeft = 5;
			style.CornerRadiusTopRight = 5;
			style.CornerRadiusBottomRight = 5;
			style.CornerRadiusBottomLeft = 5;
			
			style.ExpandMarginLeft = 8;
			style.ExpandMarginRight = 8;
			style.ExpandMarginTop = 4;
			style.ExpandMarginBottom = 4;
			
			dateLabel.AddThemeStyleboxOverride("normal", style);
			UpdateDateLabel(0);
		}

		CreateGhostSprite();
		SetupSimulationTimer();

		if (plantListContainer != null) plantListContainer.Visible = false;

		UpdateSeasonVisuals();
	}

	private void UpdateSeason(int monthTotal)
	{
		int monthInYear = ((monthTotal - 1) % 12) + 1;

		Season newSeason;
		if (monthInYear >= 3 && monthInYear <= 5) newSeason = Season.Spring;
		else if (monthInYear >= 6 && monthInYear <= 8) newSeason = Season.Summer;
		else if (monthInYear >= 9 && monthInYear <= 11) newSeason = Season.Autumn;
		else newSeason = Season.Winter;

		if (currentSeason != newSeason)
		{
			currentSeason = newSeason;
			UpdateSeasonVisuals();
			GD.Print($"Zmiana pory roku: {currentSeason}");
		}
	}

	private void UpdateSeasonVisuals()
	{
		Color bgColor;
		switch (currentSeason)
		{
			case Season.Spring:
				bgColor = new Color(0.4f, 0.7f, 0.4f);
				break;
			case Season.Summer:
				bgColor = new Color(0.2f, 0.6f, 0.2f);
				break;
			case Season.Autumn:
				bgColor = new Color(0.7f, 0.5f, 0.3f);
				break;
			case Season.Winter:
				bgColor = new Color(0.9f, 0.95f, 1.0f);
				break;
			default:
				bgColor = new Color(0.3f, 0.3f, 0.3f);
				break;
		}
		
		Color startColor = RenderingServer.GetDefaultClearColor();

		if (_colorTween != null && _colorTween.IsValid())
		{
			_colorTween.Kill();
		}

		_colorTween = CreateTween();

		_colorTween.TweenMethod(
			Callable.From<Color>(c => RenderingServer.SetDefaultClearColor(c)), 
			startColor, 
			bgColor, 
			1.5f 
		);
		_colorTween.SetEase(Tween.EaseType.InOut).SetTrans(Tween.TransitionType.Cubic);

	}

	private float GetSeasonGrowthModifier()
	{
		switch (currentSeason)
		{
			case Season.Spring: return 1.2f;
			case Season.Summer: return 1.0f;
			case Season.Autumn: return 0.5f;
			case Season.Winter: return 0.0f;
			default: return 1.0f;
		}
	}

	private void SetupCamera()
	{
		_camera = GetNodeOrNull<Camera2D>("Camera2D");
		_camera.MakeCurrent();
	}

	private void PopulateSoilOptions(){
		if (_soilOptionButton == null) return;
		_soilOptionButton.Clear();
		foreach (var soil in availableSoilTypes){
			_soilOptionButton.AddItem(soil.Name);
		}
	}
	

	private void OnSetupConfirmed()
	{
		if (_soilOptionButton != null)
		{
			int soilIndex = _soilOptionButton.Selected;
			if (soilIndex >= 0 && soilIndex < availableSoilTypes.Count) 
				currentSoilType = availableSoilTypes[soilIndex];
		}

		float width = 10;
		float height = 10;

		if (_widthSpinBox != null) width = (float)_widthSpinBox.Value;
		if (_heightSpinBox != null) height = (float)_heightSpinBox.Value;

		_gardenSize = new Vector2(width, height);
		
		_isConfigured = true;
		if (_setupPanel != null) _setupPanel.Visible = false;
		if (plantListContainer != null) plantListContainer.Visible = true;
		
		GD.Print($"Ogród skonfigurowany: {_gardenSize.X}x{_gardenSize.Y}m, Gleba: {currentSoilType.Name}");
		QueueRedraw(); 
	}

	public override void _Draw()
	{
		if (!_isConfigured) return;

		Vector2 sizeInPixels = _gardenSize * PixelsPerMeter;
		Rect2 gardenRect = new Rect2(new Vector2(300,100), sizeInPixels);
		
		// Tło ogrodu
		DrawRect(gardenRect, new Color(0.5f, 0.4f, 0.2f, 0.2f), true);
		DrawRect(gardenRect, Colors.White, false, 2.0f);

		// Rysowanie dziur po dzikach
		foreach (var hole in boarHoles)
		{
			Vector2 holePos = hole.Position;
			float radiusMeters = hole.Radius;
			float radiusPixels = radiusMeters * PixelsPerMeter;
			
			// Oblicz przezroczystość na podstawie czasu życia (MonthsLeft)
			float opacity = Mathf.Clamp((float)hole.MonthsLeft / 3.0f, 0.0f, 1.0f);
			
			// Ciemnobrązowy kolor ziemi z uwzględnieniem zanikania
			Color dirtColor = new Color(0.25f, 0.15f, 0.1f, 0.7f * opacity);
			DrawCircle(holePos, radiusPixels, dirtColor);
			
			// obrys dziury
			DrawArc(holePos, radiusPixels, 0, Mathf.Pi * 2, 32, new Color(0.15f, 0.1f, 0.05f, 1.0f * opacity), 2.0f);
		}
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
		simulationTimer.WaitTime = simulationSpeedSeconds / timeScale;
		simulationTimer.OneShot = false;
		simulationTimer.Timeout += OnSimulationTimerTimeout;
		AddChild(simulationTimer);
	}

	private void SetupControlButtons()
	{
		simulationButton = GetNodeOrNull<Button>("CanvasLayer/Control/SimulationButton");
		if(simulationButton != null) 
		{ 
			if (!simulationButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnSimulationButtonPressed)))
				simulationButton.Pressed += OnSimulationButtonPressed; 
		}
		else
		{
			GD.PrintErr("BŁĄD: Nie znaleziono przycisku CanvasLayer/Control/SimulationButton");
		}
		
		stopButton = GetNodeOrNull<Button>("CanvasLayer/Control/StopButton");
		if(stopButton != null) 
		{ 
			if (!stopButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnStopButtonPressed)))
				stopButton.Pressed += OnStopButtonPressed; 
			stopButton.Visible = false; 
		}
		else
		{
			GD.PrintErr("BŁĄD: Nie znaleziono przycisku CanvasLayer/Control/StopButton");
		}
		
		saveButton = GetNodeOrNull<Button>("CanvasLayer/Control/SaveButton");
		if (saveButton != null)
		{
			if (!saveButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnSaveButtonPressed)))
				saveButton.Pressed += OnSaveButtonPressed;
			saveButton.Visible = false;
		}
		
		loadButton = GetNodeOrNull<Button>("CanvasLayer/Control/LoadButton");
		if (loadButton != null)
		{
			if (!loadButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnLoadButtonPressed)))
				loadButton.Pressed += OnLoadButtonPressed;
			loadButton.Visible = true;
		}
		
		speedButton = GetNodeOrNull<Button>("CanvasLayer/Control/SpeedButton");
		if (speedButton != null)
		{
			if (!speedButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnSpeedButtonPressed)))
				speedButton.Pressed += OnSpeedButtonPressed;
			speedButton.Visible = true;
		}
		
		resetButton = GetNodeOrNull<Button>("CanvasLayer/Control/ResetButton");
		if (resetButton != null)
		{
			if (!resetButton.IsConnected(Button.SignalName.Pressed, Callable.From(OnResetButtonPressed)))
				resetButton.Pressed += OnResetButtonPressed;
			resetButton.Visible = true;
		}
	}

	private void CreateGhostSprite()
	{
		ghostSprite = new Sprite2D();
		ghostSprite.Modulate = new Color(1, 1, 1, 0.5f);
		ghostSprite.Visible = false;
		ghostSprite.Scale = new Vector2(0.1f, 0.1f);
		ghostSprite.ZIndex = 100;
		AddChild(ghostSprite);
	}

	private void GeneratePlantButtons(int categoryFilter = 0)
{
	if (plantListContainer == null)
	{
		GD.PrintErr("BŁĄD KRYTYCZNY: Zmienna 'plantListContainer' jest NULL.");
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
		btn.Alignment = HorizontalAlignment.Left;
		
		if (ResourceLoader.Exists(plantData.TexturePath))
		{
			var icon = (Texture2D)ResourceLoader.Load(plantData.TexturePath);
			btn.Icon = icon;
			btn.ExpandIcon = true;
		}
		
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
		if (!_isConfigured) return;
		if (_camera == null) return;

		bool isMouseOverUI = false;
		var panel = GetNodeOrNull<Control>("CanvasLayer/Panel");
		if (panel != null && panel.Visible)
		{
			if (panel.GetGlobalRect().HasPoint(GetViewport().GetMousePosition()))
			{
				isMouseOverUI = true;
			}
		}
		
		if (@event is InputEventMouseMotion mouseMotion)
		{
			if (selectedPlantType != null && ghostSprite.Visible)
			{
				ghostSprite.GlobalPosition = GetGlobalMousePosition();
			}
			
			if (_isPanning)
			{
				Vector2 currentMousePos = mouseMotion.Position;
				Vector2 diff = currentMousePos - _panStartMousePos;
				_camera.Position = _panStartCameraPos - (diff / _camera.Zoom);
			}
		}

		if (@event is InputEventMouseButton mouseButton)
		{
			//Zoomowanie kamery
			if (mouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				if (!isMouseOverUI)
				{
					Vector2 newZoom = _camera.Zoom + new Vector2(_zoomSpeed, _zoomSpeed);
					if (newZoom.X < _maxZoom.X)
					{
						_camera.Zoom = newZoom;
					}
				}
			}
			else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				if (!isMouseOverUI)
				{
					Vector2 newZoom = _camera.Zoom - new Vector2(_zoomSpeed, _zoomSpeed);
					if (newZoom.X < _maxZoom.X)
					{
						_camera.Zoom = newZoom;
					}
				}
			}

			//Przesuwanie kamery
			if (mouseButton.ButtonIndex == MouseButton.Middle)
			{
				if (mouseButton.Pressed)
				{
					_isPanning = true;
					_panStartMousePos = mouseButton.Position;
					_panStartCameraPos = _camera.Position;
				}
				else
				{
					_isPanning = false;
				}
			}

			//Sadzenie / Anulowanie
			if (mouseButton.Pressed)
			{
				if (mouseButton.ButtonIndex == MouseButton.Left)
				{
					if (selectedPlantType != null)
					{
						if (_setupPanel != null && _setupPanel.Visible)
							isMouseOverUI = true;
						if (!isMouseOverUI)
						{
							ulong now = Time.GetTicksMsec();
							if (now - _lastPlantTime > 200)
							{
								PlacePlant(selectedPlantType.Value, GetGlobalMousePosition());
								_lastPlantTime = now;
								GetViewport().SetInputAsHandled();
							}
						}
					}
					else
					{
						if (!isMouseOverUI)
						{
							RemovePlantAt(GetGlobalMousePosition());
							GetViewport().SetInputAsHandled();
						}
					}
				}
				else if (mouseButton.ButtonIndex == MouseButton.Right)
				{
					CancelPlacement();
				}
			}
		}
	}

	private void RemovePlantAt(Vector2 position)
	{
		var plantsUnderCursor = new List<Plant>();
		foreach (var plant in plantedPlants)
		{
			float clickRadius = plant.CurrentRadius * PixelsPerMeter;
			if (plant.Position.DistanceTo(position) <= clickRadius)
			{
				plantsUnderCursor.Add(plant);
			}
		}

		if (plantsUnderCursor.Count >= 0)
		{
			plantsUnderCursor.Sort((a, b) =>
			{
				int radiusCompare = a.CurrentRadius.CompareTo(b.CurrentRadius);
				if(radiusCompare != 0) return radiusCompare;
				return 0;
			});
			
			Plant plantToRemove = plantsUnderCursor[0];
			
			plantedPlants.Remove(plantToRemove);
			plantToRemove.QueueFree();
			GD.Print($"Usunięto roślinę: {plantToRemove.TypeData.Name} z pozycji {position}.");
		}
		
	}
	
	public Plant PlacePlant(PlantTypeData typeData, Vector2 position)
	{

		if (!_isConfigured) return null;

		Vector2 maxPos = _gardenSize * PixelsPerMeter + new Vector2(300,100);
		if (position.X < 300 || position.Y < 100 || position.X > maxPos.X || position.Y > maxPos.Y)
		{
			GD.Print("Nie można sadzić poza granicami ogrodu!");
			return null;
		}

		if (plantScene == null) return null;

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
		
		return newPlant;
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

	private void OnSpeedButtonPressed()
	{
		if (timeScale == 1.0f)
		{
			timeScale = 2.0f;
			speedButton.Text = "2x";
		}
		else
		{
			timeScale = 1.0f;
			speedButton.Text = "1x";
		}
		
		if (isSimulationRunning && simulationTimer != null)
		{
			simulationTimer.WaitTime = simulationSpeedSeconds / timeScale;	
		}
		
		GD.Print($"Zmieniono prędkość symulacji na {timeScale}x");
	}
	
	private void OnResetButtonPressed()
	{
		GD.Print("Resetowanie ogrodu...");
		StopSimulation();
		
		foreach (var plant in plantedPlants)
		{
			plant.QueueFree();
		}
		plantedPlants.Clear();
		
		// Czyszczenie dziur
		boarHoles.Clear();
		
		currentSimulationMonth = 0;
		UpdateDateLabel(currentSimulationMonth);
		
		_isConfigured = false;
		
		if(_setupPanel != null) _setupPanel.Visible = true;
		if(plantListContainer != null) plantListContainer.Visible = false;
		if(simulationButton != null) simulationButton.Visible = true;
		if(saveButton != null) saveButton.Visible = false;
		if(_camera != null) _camera.Zoom = new Vector2(1, 1);
		
		_gardenSize = new Vector2(10, 10);
		var gardenBg = GetNodeOrNull<ColorRect>("GardenBackground");
		if (gardenBg != null)
		{
			gardenBg.Size = _gardenSize * PixelsPerMeter;
		}

		_camera.Position = new Vector2(600, 300);
		QueueRedraw();
		
		GD.Print("Ogród zresetowany.");
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
		if(stopButton != null) stopButton.Visible = true;
		if(saveButton != null) saveButton.Visible = false;
		if(loadButton != null) loadButton.Visible = false;
		
		simulationTimer.WaitTime = simulationSpeedSeconds / timeScale;
		simulationTimer.Start();
		GD.Print($"--- Rozpoczecie symulacji ---");
	}
	
	private void StopSimulation()
	{
		isSimulationRunning = false;
		simulationTimer.Stop();
		
		if(simulationButton != null) simulationButton.Disabled = false;
		if(stopButton != null) stopButton.Visible = false;
		if(saveButton != null) saveButton.Visible = true;
		if(loadButton != null) loadButton.Visible = true;
		
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
	
	private void UpdateDateLabel(int totalMonths)
	{
		if (dateLabel == null) return;

		int year = (totalMonths -1) / 12 + 1;
		int monthInYear = ((totalMonths -1) % 12) + 1;

		dateLabel.Text = $"Rok: {year}, Miesiąc: {monthInYear}";
	}

	private void SimulateStep(int month)
	{
		UpdateSeason(month);
		
		// Event losowy
		TriggerBoarEvent();

		// Aktualizacja dziur po dzikach (zanikanie)
		for (int i = boarHoles.Count - 1; i >= 0; i--)
		{
			boarHoles[i].MonthsLeft--;
			if (boarHoles[i].MonthsLeft <= 0)
			{
				boarHoles.RemoveAt(i);
			}
		}
		QueueRedraw(); // Odśwież widok, żeby pokazać zanikanie
		
		int year = (month -1) / 12 + 1;
		int monthInYear = ((month -1) % 12) + 1;
		
		GD.Print($"--- Symulacja Rok {year}, Miesiąc {monthInYear} ---");

		float seasonFactor = GetSeasonGrowthModifier();
		
		for (int i = plantedPlants.Count - 1; i >= 0; i--)
		{
			var plantA = plantedPlants[i];
			
			// Jeśli roślina została oznaczona do usunięcia (np. przez dzika w tej klatce), pomiń
			if (!IsInstanceValid(plantA) || plantA.IsQueuedForDeletion()) continue;

			if (seasonFactor <= 0.01f)
			{
				continue;
			}
			
			float sunLevel = CalculateSunLevel(plantA);
			float growthFactorSoil = GetGrowthFactor(plantA);
			plantA.SimulateMonth(growthFactorSoil, sunLevel);
		}
	}
	
	private void TriggerBoarEvent()
	{
		// 1% szansy na pojawienie się dzika w danym miesiącu
		if (GD.Randf() > 0.01f) return;

		// Zakres ogrodu
		float minX = 300;
		float minY = 100;
		float maxX = 300 + (_gardenSize.X * PixelsPerMeter);
		float maxY = 100 + (_gardenSize.Y * PixelsPerMeter);

		// Losowa pozycja dziury
		Vector2 holePos = new Vector2(
			(float)GD.RandRange(minX, maxX),
			(float)GD.RandRange(minY, maxY)
		);

		// Losowy promień (w metrach: 0.5m do 1.5m)
		float holeRadiusMeters = (float)GD.RandRange(0.5f, 1.5f);

		// Dodanie dziury do listy i wizualizacja.
		boarHoles.Add(new BoarHoleInstance { 
			Position = holePos, 
			Radius = holeRadiusMeters,
			MonthsLeft = 3 
		});
		
		GD.Print($"[EVENT] Dzik zrył ziemię w punkcie {holePos} (promień: {holeRadiusMeters:F2}m)!");

		List<Plant> plantsToKill = new List<Plant>();

		foreach (var plant in plantedPlants)
		{
			// Sprawdzenie kolizji dziury z rośliną
			float dist = plant.Position.DistanceTo(holePos);
			float limit = holeRadiusMeters * PixelsPerMeter; // Promień w pikselach

			// Jeśli roślina jest w zasięgu rycia
			if (dist < limit)
			{
				// Sprawdzamy ID typu rośliny
				// 3=Kwiaty, 4=Paprocie, 5=Zioła, 6=Trawy
				// (Drzewa=1 i Krzewy=2 są bezpieczne)
				int typeId = plant.TypeData.TypeId;
				if (typeId == 3 || typeId == 4 || typeId == 5 || typeId == 6)
				{
					plantsToKill.Add(plant);
				}
			}
		}

		if (plantsToKill.Count > 0)
		{
			GD.Print($"[EVENT] Dzik zniszczył {plantsToKill.Count} roślin!");
			foreach (var p in plantsToKill)
			{
				plantedPlants.Remove(p);
				p.QueueFree();
			}
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
			if (!IsInstanceValid(plantA)) continue;

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
	
	private void SetupSaveDialog()
	{
		saveFileDialog = new FileDialog();
		saveFileDialog.Name = "SaveFileDialog";
		saveFileDialog.Access = FileDialog.AccessEnum.Filesystem; 
		saveFileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
		saveFileDialog.AddFilter("*.json", "Pliki JSON");
		saveFileDialog.MinSize = new Vector2I(600, 400); 
		if (!saveFileDialog.IsConnected(FileDialog.SignalName.FileSelected, Callable.From<string>(OnFileSelectedForSave)))
		{
			saveFileDialog.FileSelected += OnFileSelectedForSave;
		}
		
		var canvas = GetNodeOrNull("CanvasLayer");
		if (canvas != null)
		{
			canvas.AddChild(saveFileDialog);
		}
		else
		{
			GD.Print("Ostrzeżenie: Brak CanvasLayer, dodaję FileDialog bezpośrednio do sceny.");
			AddChild(saveFileDialog);
		}
	}
	
	private void OnSaveButtonPressed()
	{
		if (saveFileDialog != null)
		{
			saveFileDialog.PopupCentered();
		}
		else
		{
			GD.PrintErr("BŁĄD: saveFileDialog jest null!");
		}
	}
	
	private void OnFileSelectedForSave(string path)
	{
		SaveGardenState(path);
	}

	private void SaveGardenState(string path)
	{
		GD.Print("Zapisywanie stanu ogrodu...");
		
		var gardenBg = GetNodeOrNull<ColorRect>("GardenBackground");
		
		var saveData = new GardenSaveData
		{
			CurrentSimulationMonth = currentSimulationMonth,
			SoilTypeID = currentSoilType.Id,
			GardenWidth = gardenBg != null ? (int)(gardenBg.Size.X / PixelsPerMeter) : (int)_gardenSize.X,
			GardenHeight = gardenBg != null ? (int)(gardenBg.Size.Y / PixelsPerMeter) : (int)_gardenSize.Y,
			Plants = new List<PlantSaveData>(),
			Holes = new List<BoarHoleSaveData>()
		};
		
		// Zapisz rośliny
		foreach (var plant in plantedPlants)
		{
			saveData.Plants.Add(new PlantSaveData
			{
				TypeID = plant.TypeData.Id,
				PositionX = plant.Position.X,
				PositionY = plant.Position.Y,
				AgeMonths = plant.AgeMonths,
				CurrentHeight = plant.CurrentHeight,
				CurrentRadius = plant.CurrentRadius
			});
		}

		// Zapisz dziury po dzikach
		foreach (var hole in boarHoles)
		{
			saveData.Holes.Add(new BoarHoleSaveData
			{
				PositionX = hole.Position.X,
				PositionY = hole.Position.Y,
				Radius = hole.Radius,
				MonthsLeft = hole.MonthsLeft
			});
		}

		try
		{
			string jsonString = JsonSerializer.Serialize(saveData, new JsonSerializerOptions {WriteIndented =  true});
			
			using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
			if (file == null)
			{
				GD.PrintErr($"Błąd otwarcia pliku do zapisu: {FileAccess.GetOpenError()}");
				return;
			}

			file.StoreString(jsonString);
			GD.Print("Stan ogrodu zapisanym pomyślnie");
		}
		catch (System.Exception e)
		{
			GD.PrintErr($"Błąd podczas zapisywania stanu ogrodu: {e.Message}");
		}
	}
	
	private void SetupLoadDialog()
	{
		loadFileDialog = new FileDialog();
		loadFileDialog.Name = "LoadFileDialog";
		loadFileDialog.Access = FileDialog.AccessEnum.Filesystem; 
		loadFileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
		loadFileDialog.AddFilter("*.json", "Pliki JSON");
		loadFileDialog.MinSize = new Vector2I(600, 400); 
		if (!loadFileDialog.IsConnected(FileDialog.SignalName.FileSelected, Callable.From<string>(OnFileSelectedForLoad)))
		{
			loadFileDialog.FileSelected += OnFileSelectedForLoad;
		}
		
		var canvas = GetNodeOrNull("CanvasLayer");
		if (canvas != null)
		{
			canvas.AddChild(loadFileDialog);
		}
		else
		{
			GD.Print("Ostrzeżenie: Brak CanvasLayer, dodaję FileDialog bezpośrednio do sceny.");
			AddChild(loadFileDialog);
		}
	}
	
	private void OnLoadButtonPressed()
	{
		if (loadFileDialog != null)
		{
			loadFileDialog.PopupCentered();
		}
		else
		{
			GD.PrintErr("BŁĄD: loadFileDialog jest null!");
		}
	}

	private void OnFileSelectedForLoad(string path)
	{
		LoadGardenState(path);
	}

	private void LoadGardenState(string path)
	{
		
		GD.Print($"Wczytywanie stanu ogrodu z: {path}...");
		try
		{
			using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			if (file == null)
			{
				GD.PrintErr($"Błąd otwarcia pliku do wczytania: {FileAccess.GetOpenError()}");
				return;
			}

			string jsonString = file.GetAsText();
			GardenSaveData saveData = JsonSerializer.Deserialize<GardenSaveData>(jsonString);

			if (saveData == null)
			{
				GD.PrintErr("Błąd deserializacji danych ogrodu.");
				return;
			}

			// Czyszczenie stanu
			foreach (var plant in plantedPlants)
			{
				plant.QueueFree();
			}
			plantedPlants.Clear();
			boarHoles.Clear();

			currentSimulationMonth = saveData.CurrentSimulationMonth;
			UpdateDateLabel(currentSimulationMonth);

			var savedSoil = availableSoilTypes.FirstOrDefault(s => s.Id == saveData.SoilTypeID);
			if (savedSoil.Id != 0)
			{
				currentSoilType = savedSoil;
				GD.Print($"Przywrócono typ gleby: {currentSoilType.Name}");
			}
			
			if (saveData.GardenWidth > 0 && saveData.GardenHeight > 0)
			{
				_gardenSize = new Vector2(saveData.GardenWidth, saveData.GardenHeight);
				var gardenBg = GetNodeOrNull<ColorRect>("GardenBackground");
				if (gardenBg != null)
				{
					gardenBg.Size = _gardenSize * PixelsPerMeter;
				}
			}
			
			_isConfigured = true;
			_setupPanel.Visible = false;
			QueueRedraw();	
			if (plantListContainer != null) plantListContainer.Visible = true;
			
			// Wczytywanie roślin
			foreach (var plantData in saveData.Plants)
			{
				var typeData = availablePlantTypes.FirstOrDefault(p => p.Id == plantData.TypeID);
				if (typeData.Id == 0)
				{
					GD.PrintErr($"Nie znaleziono typu rośliny o ID: {plantData.TypeID}. Pomijam.");
					continue;
				}
				
				Vector2 pos = new  Vector2(plantData.PositionX, plantData.PositionY);
				Plant newPlant = PlacePlant(typeData, pos);

				if (newPlant != null)
				{
					newPlant.LoadState(plantData.AgeMonths, plantData.CurrentHeight, plantData.CurrentRadius);
				}
			}

			// Wczytywanie dziur po dzikach
			if (saveData.Holes != null)
			{
				foreach (var holeData in saveData.Holes)
				{
					boarHoles.Add(new BoarHoleInstance {
						Position = new Vector2(holeData.PositionX, holeData.PositionY), 
						Radius = holeData.Radius,
						// Jeśli wczytujemy stary zapis (bez MonthsLeft), domyślnie dajemy 3 miesiące
						MonthsLeft = holeData.MonthsLeft > 0 ? holeData.MonthsLeft : 3
					});
				}
			}
		
			GD.Print("Stan ogrodu wczytany pomyślnie");
			
			
		}
		catch (System.Exception e)
		{
			GD.PrintErr($"Błąd podczas wczytywania stanu ogrodu: {e.Message}");
		}
	}
	
	private void ApplyCustomTheme()
	{
		var theme = new Theme();

		// Kolory
		var baseGreen = new Color(0.26f, 0.42f, 0.28f); // Ciemna zieleń
		var hoverGreen = new Color(0.34f, 0.52f, 0.36f); // Jaśniejsza
		var pressedGreen = new Color(0.19f, 0.32f, 0.21f); // Bardzo ciemna
		var panelBg = new Color(0.12f, 0.13f, 0.12f, 0.86f); // Półprzezroczysty czarny

		// StyleBox dla przycisków
		var btnNormal = new StyleBoxFlat {
			BgColor = baseGreen, CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomRight = 6, CornerRadiusBottomLeft = 6,
			BorderWidthBottom = 3, BorderColor = pressedGreen, ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 5, ContentMarginBottom = 5
		};
		var btnHover = new StyleBoxFlat {
			BgColor = hoverGreen, CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomRight = 6, CornerRadiusBottomLeft = 6,
			BorderWidthBottom = 3, BorderColor = pressedGreen, ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 5, ContentMarginBottom = 5
		};
		var btnPressed = new StyleBoxFlat {
			BgColor = pressedGreen, CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6, CornerRadiusBottomRight = 6, CornerRadiusBottomLeft = 6,
			BorderWidthTop = 2, BorderColor = new Color(0,0,0,0), ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 4
		};

		// StyleBox dla Paneli
		var pnlStyle = new StyleBoxFlat {
			BgColor = panelBg, CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomRight = 12, CornerRadiusBottomLeft = 12,
			BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2, BorderColor = hoverGreen
		};

		// Przypisanie styli do Theme
		theme.SetStylebox("normal", "Button", btnNormal);
		theme.SetStylebox("hover", "Button", btnHover);
		theme.SetStylebox("pressed", "Button", btnPressed);
		theme.SetStylebox("panel", "Panel", pnlStyle);
		
		theme.SetColor("font_color", "Button", Colors.WhiteSmoke);
		theme.SetColor("font_color", "Label", new Color(0.9f, 0.94f, 0.9f));

		// Aplikacja motywu do głównych kontenerów
		var setupPanel = GetNodeOrNull<Control>("SetupPanel");
		var sidePanel = GetNodeOrNull<Panel>("CanvasLayer/Panel");
		var controlPanel = GetNodeOrNull<Control>("CanvasLayer/Control");

		if (setupPanel != null) setupPanel.Theme = theme;
		if (sidePanel != null) sidePanel.Theme = theme;
		if (controlPanel != null) controlPanel.Theme = theme;
	}
}


public class GardenSaveData
{
	public int CurrentSimulationMonth { get; set; }
	public int SoilTypeID { get; set; }
	public int GardenWidth { get; set; }
	public int GardenHeight { get; set; }
	public List<PlantSaveData> Plants { get; set; } = new List<PlantSaveData>();
	public List<BoarHoleSaveData> Holes { get; set; } = new List<BoarHoleSaveData>();
}
public class PlantSaveData
{
	public int TypeID { get; set; }
	public float PositionX { get; set; }
	public float PositionY { get; set; }
	public int AgeMonths { get; set; }
	public float CurrentHeight { get; set; }
	public float CurrentRadius { get; set; }
}
public class BoarHoleSaveData
{
	public float PositionX { get; set; }
	public float PositionY { get; set; }
	public float Radius { get; set; }
	public int MonthsLeft { get; set; }
}