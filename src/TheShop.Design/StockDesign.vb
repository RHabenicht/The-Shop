Imports System.Text.Json
Imports System.Text.Json.Serialization

Public NotInheritable Class StockDesign
    <JsonRequired>
    Public Property ChassisId As String
    <JsonRequired>
    Public Property PowerPlantId As String
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property AuxiliaryPowerPlantId As String
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property StreetIndex As Decimal?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Name As String
    Public Property SpecialtyClass As VehicleSpecialtyClass
    Public Property MarkupAdjustment As Decimal
    Public Property CargoCapacity As Decimal?
    Public Property LoadCapacityKg As Decimal?
    Public Property Additions As New List(Of DesignAddition)
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property Upgrades As DesignUpgrades
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property LegacyReference As JsonElement?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property LegacyImportNotes As List(Of String)
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property AvailabilityClass As VehicleAvailabilityClass?
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property CatalogModifications As List(Of ModificationSelection)
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property InstalledRetrofits As List(Of ModificationSelection)

    Public Function Calculate(catalog As IReadOnlyList(Of StockChassis)) As StockDesignResult
        Dim chassis = StockCatalog.FindById(catalog, ChassisId)
        Dim powerPlant = chassis.PowerPlants.SingleOrDefault(
            Function(candidate) String.Equals(candidate.Id, PowerPlantId, StringComparison.OrdinalIgnoreCase))
        If powerPlant Is Nothing Then
            Throw New ArgumentException($"Power plant '{PowerPlantId}' is unavailable for '{chassis.Id}' in this catalog.")
        End If

        powerPlant = DesignEnhancements.PreparePlant(chassis, powerPlant, CatalogModifications)
        Dim auxiliary = AuxiliaryEngine.Resolve(AuxiliaryPowerPlantId, chassis)
        If auxiliary IsNot Nothing Then
            Dim sharedEnhancements = CatalogModifications?.Where(Function(item) item IsNot Nothing AndAlso
                Not String.Equals(item.Id, "hybrid-engine", StringComparison.OrdinalIgnoreCase)).ToArray()
            auxiliary = DesignEnhancements.PreparePlant(chassis, auxiliary, sharedEnhancements)
        End If
        Dim maximumLoad = If(auxiliary Is Nothing, powerPlant.MaximumLoadKg, Math.Min(powerPlant.MaximumLoadKg, auxiliary.MaximumLoadKg))
        Dim startingLoad = If(auxiliary Is Nothing, powerPlant.StartingLoadKg, Math.Min(powerPlant.StartingLoadKg, auxiliary.StartingLoadKg))
        Dim cargo = If(CargoCapacity, chassis.StartingCargo)
        Dim load = If(LoadCapacityKg, startingLoad)
        Dim design As New VehicleDesign With {
            .ChassisDesignPoints = chassis.DesignPoints,
            .PowerPlantDesignPoints = powerPlant.DesignPoints,
            .ChassisMarkup = chassis.Markup,
            .CargoCapacity = cargo,
            .LoadCapacityKg = load,
            .StreetIndex = If(StreetIndex, BookStreetIndex()),
            .MarkupAdjustment = MarkupAdjustment,
            .Additions = Additions
        }
        If auxiliary IsNot Nothing Then
            Dim size = CDec(If(chassis.Hull, chassis.Body).GetValueOrDefault())
            Dim factor = If(chassis.Hull.HasValue, 100D * size * size, 1D)
            design.Additions = New List(Of DesignAddition)(design.Additions) From {
                New DesignAddition With {.Name = "Auxiliary engine — " & auxiliary.Name,
                    .DesignPoints = auxiliary.DesignPoints, .CargoConsumed = (size + factor) * 2D,
                    .LoadConsumedKg = maximumLoad * 0.1D}
            }
        End If
        Dim ratings = Upgrades?.Apply(chassis, powerPlant, design, CargoCapacity, LoadCapacityKg)
        If design.CargoCapacity > chassis.MaximumCargo Then
            Throw New ArgumentException($"Cargo capacity exceeds the catalog maximum of {chassis.MaximumCargo} CF.")
        End If
        If design.LoadCapacityKg > maximumLoad Then
            Throw New ArgumentException($"Load capacity exceeds the catalog maximum of {maximumLoad} kg.")
        End If

        Dim validationCargo = design.CargoCapacity
        Dim validationLoad = design.LoadCapacityKg
        Dim modifiedRatings As ModifiedRatings = Nothing
        Dim modifications = ModificationCatalog.Apply(CatalogModifications, chassis, powerPlant, design, ratings, modifiedRatings)
        Dim totals = design.Calculate()
        Dim retrofits = RetrofitCatalog.Apply(InstalledRetrofits, CatalogModifications, chassis, powerPlant, ratings, totals, validationCargo, validationLoad, modifiedRatings)
        Dim externalCargo = If(retrofits?.Sum(Function(item) item.Quote.ExternalCargoCapacity), 0D)
        Dim availability = If(AvailabilityClass.HasValue, VehicleAvailability.Calculate(AvailabilityClass.Value, totals.BaseCostNuyen), Nothing)
        If availability IsNot Nothing AndAlso DesignEnhancements.Has(CatalogModifications, "fin-drive") Then
            availability.Rating += 4D
            availability.BaseDays += 4D
            availability.PlayerAccessible = availability.Rating <= 30D
        End If
        Return New StockDesignResult With {
            .ChassisId = chassis.Id, .PowerPlantId = powerPlant.Id, .Totals = totals, .UpgradedRatings = ratings,
            .AppliedModifications = modifications, .ModifiedRatings = modifiedRatings, .InstalledRetrofits = retrofits, .AuxiliaryEngine = AuxiliaryEngine.Ratings(auxiliary),
            .ExternalCargoCapacity = externalCargo, .OperatingModes = VehicleOperatingModes.Build(CatalogModifications, powerPlant, chassis, ratings, modifiedRatings, externalCargo),
            .Availability = availability, .EffectiveStreetIndex = design.StreetIndex
        }
    End Function

    Private Function BookStreetIndex() As Decimal
        Dim index = 1D
        Select Case AvailabilityClass.GetValueOrDefault()
            Case VehicleAvailabilityClass.Luxury, VehicleAvailabilityClass.Security
                index = 2D
            Case VehicleAvailabilityClass.Military
                index = 3D
        End Select
        Select Case SpecialtyClass
            Case VehicleSpecialtyClass.Medical
                index += 0.5D
            Case VehicleSpecialtyClass.NonCombat
                index += 1D
            Case VehicleSpecialtyClass.None
            Case Else
                Throw New ArgumentException("Unknown vehicle specialty class.")
        End Select
        If DesignEnhancements.Has(CatalogModifications, "smart-materials") Then index += 1D
        Return index
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
    Friend Function Copy() As StockPowerPlant
        Return DirectCast(MemberwiseClone(), StockPowerPlant)
    End Function
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
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property AppliedModifications As IReadOnlyList(Of DesignAddition)
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property ModifiedRatings As ModifiedRatings
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingDefault)>
    Public Property ExternalCargoCapacity As Decimal
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property OperatingModes As IReadOnlyList(Of VehicleOperatingMode)
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property AuxiliaryEngine As AuxiliaryEngineRatings
    <JsonIgnore(Condition:=JsonIgnoreCondition.WhenWritingNull)>
    Public Property InstalledRetrofits As IReadOnlyList(Of InstalledRetrofit)
    Public Property EffectiveStreetIndex As Decimal
    Public ReadOnly Property RetrofitListPartsCostNuyen As Decimal
        Get
            Return If(InstalledRetrofits?.Sum(Function(item) item.Quote.PartsCostNuyen), 0D)
        End Get
    End Property
    Public ReadOnly Property RetrofitPartsCostNuyen As Decimal?
        Get
            If InstalledRetrofits IsNot Nothing AndAlso InstalledRetrofits.Any(Function(item) Not item.Quote.StreetPartsCostNuyen.HasValue) Then Return Nothing
            Return If(InstalledRetrofits?.Sum(Function(item) item.Quote.StreetPartsCostNuyen.GetValueOrDefault()), 0D)
        End Get
    End Property
End Class
