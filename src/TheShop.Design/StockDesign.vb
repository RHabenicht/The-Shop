Imports System.Text.Json
Imports System.Text.Json.Serialization

Public NotInheritable Class StockDesign
    <JsonRequired>
    Public Property ChassisId As String
    <JsonRequired>
    Public Property PowerPlantId As String
    <JsonRequired>
    Public Property StreetIndex As Decimal
    Public Property MarkupAdjustment As Decimal
    Public Property CargoCapacity As Decimal?
    Public Property LoadCapacityKg As Decimal?
    Public Property Additions As New List(Of DesignAddition)
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Upgrades As DesignUpgrades
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property LegacyReference As JsonElement?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property AvailabilityClass As VehicleAvailabilityClass?

    Public Function Calculate(catalog As IReadOnlyList(Of StockChassis)) As StockDesignResult
        Dim chassis = StockCatalog.FindById(catalog, ChassisId)
        Dim powerPlant = chassis.PowerPlants.SingleOrDefault(
            Function(candidate) String.Equals(candidate.Id, PowerPlantId, StringComparison.OrdinalIgnoreCase))
        If powerPlant Is Nothing Then
            Throw New ArgumentException($"Power plant '{PowerPlantId}' is unavailable for '{chassis.Id}' in this catalog.")
        End If

        Dim cargo = If(CargoCapacity, chassis.StartingCargo)
        Dim load = If(LoadCapacityKg, powerPlant.StartingLoadKg)
        Dim design As New VehicleDesign With {
            .ChassisDesignPoints = chassis.DesignPoints,
            .PowerPlantDesignPoints = powerPlant.DesignPoints,
            .ChassisMarkup = chassis.Markup,
            .CargoCapacity = cargo,
            .LoadCapacityKg = load,
            .StreetIndex = StreetIndex,
            .MarkupAdjustment = MarkupAdjustment,
            .Additions = Additions
        }
        Dim ratings = Upgrades?.Apply(chassis, powerPlant, design, CargoCapacity, LoadCapacityKg)
        If design.CargoCapacity > chassis.MaximumCargo Then
            Throw New ArgumentException($"Cargo capacity exceeds the catalog maximum of {chassis.MaximumCargo} CF.")
        End If
        If design.LoadCapacityKg > powerPlant.MaximumLoadKg Then
            Throw New ArgumentException($"Load capacity exceeds the catalog maximum of {powerPlant.MaximumLoadKg} kg.")
        End If

        Dim totals = design.Calculate()
        Return New StockDesignResult With {
            .ChassisId = chassis.Id, .PowerPlantId = powerPlant.Id, .Totals = totals, .UpgradedRatings = ratings,
            .Availability = If(AvailabilityClass.HasValue, VehicleAvailability.Calculate(AvailabilityClass.Value, totals.BaseCostNuyen), Nothing)
        }
    End Function
End Class

Public Module StockCatalog
    Public Function Read(options As JsonSerializerOptions) As IReadOnlyList(Of StockChassis)
        Using data = GetType(StockChassis).Assembly.GetManifestResourceStream("TheShop.Design.StockCatalog.json")
            Return JsonSerializer.Deserialize(Of StockChassis())(data, options)
        End Using
    End Function

    Public Function FindById(catalog As IReadOnlyList(Of StockChassis), id As String) As StockChassis
        Dim chassis = catalog.SingleOrDefault(
            Function(candidate) String.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase))
        If chassis Is Nothing Then
            Throw New ArgumentException($"Unknown catalog chassis '{id}'. Use --catalog to list available chassis.")
        End If
        Return chassis
    End Function
End Module

Public NotInheritable Class StockChassis
    Public Property Id As String
    Public Property Name As String
    Public Property Category As String
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingDefault)>
    Public Property IsDrone As Boolean
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Pilot As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Autonav As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Sensor As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property TakeoffLandingProfile As String
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property SetupMinutes As Integer?
    Public Property DesignPoints As Decimal
    Public Property Markup As Decimal
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Body As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Armor As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Hull As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Bulwark As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Sonar As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property StartingDepth As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property MaximumDepth As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property OnRoadHandling As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property OffRoadHandling As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Handling As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property PeopleSpaceCf As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property MaximumPeopleSpaceCf As Decimal?
    Public Property StartingCargo As Decimal
    Public Property MaximumCargo As Decimal
    Public Property PowerPlants As StockPowerPlant()
End Class

Public NotInheritable Class StockPowerPlant
    <JsonIgnore>
    Public ReadOnly Property EconomyUnit As String
        Get
            If StartingEconomyKmPerLiter.HasValue Then Return "km/L"
            If StartingEconomyKmPerPowerFactor.HasValue Then Return "km/PF"
            If StartingEconomyKmPerBar.HasValue Then Return "km/bar"
            Return Nothing
        End Get
    End Property

    Public Property Id As String
    Public Property Name As String
    Public Property DesignPoints As Decimal
    Public Property StartingLoadKg As Decimal
    Public Property MaximumLoadKg As Decimal
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property StartingSpeed As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property MaximumSpeed As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property StartingStallSpeed As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property MaximumStallSpeed As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property StartingAcceleration As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property MaximumAcceleration As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Signature As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property SonarSignature As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property StartingSecondarySpeed As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property MaximumSecondarySpeed As Integer?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property StartingEconomyKmPerLiter As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property MaximumEconomyKmPerLiter As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property FuelLiters As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property StartingEconomyKmPerPowerFactor As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property MaximumEconomyKmPerPowerFactor As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property BatteryPowerFactors As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property FuelCellPowerFactors As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property RocketPowerFactors As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property FuelBars As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property StartingEconomyKmPerBar As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property MaximumEconomyKmPerBar As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property PeopleSpaceKg As Decimal?
End Class

Public NotInheritable Class StockDesignResult
    Public Property ChassisId As String
    Public Property PowerPlantId As String
    Public Property Totals As DesignTotals
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property UpgradedRatings As UpgradedRatings
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Availability As VehicleAvailability
End Class
