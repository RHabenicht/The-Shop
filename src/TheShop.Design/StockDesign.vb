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

    Public Function Calculate(catalog As IReadOnlyList(Of StockChassis)) As StockDesignResult
        Dim chassis = StockCatalog.FindById(catalog, ChassisId)
        Dim powerPlant = chassis.PowerPlants.SingleOrDefault(
            Function(candidate) String.Equals(candidate.Id, PowerPlantId, StringComparison.OrdinalIgnoreCase))
        If powerPlant Is Nothing Then
            Throw New ArgumentException($"Power plant '{PowerPlantId}' is unavailable for '{chassis.Id}' in this catalog.")
        End If

        Dim cargo = If(CargoCapacity, chassis.StartingCargo)
        Dim load = If(LoadCapacityKg, powerPlant.StartingLoadKg)
        If cargo > chassis.MaximumCargo Then
            Throw New ArgumentException($"Cargo capacity exceeds the catalog maximum of {chassis.MaximumCargo} CF.")
        End If
        If load > powerPlant.MaximumLoadKg Then
            Throw New ArgumentException($"Load capacity exceeds the catalog maximum of {powerPlant.MaximumLoadKg} kg.")
        End If

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
        Return New StockDesignResult With {.ChassisId = chassis.Id, .PowerPlantId = powerPlant.Id, .Totals = design.Calculate()}
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
    Public Property DesignPoints As Decimal
    Public Property Markup As Decimal
    Public Property Body As Integer
    Public Property Armor As Integer
    Public Property OnRoadHandling As Integer
    Public Property OffRoadHandling As Integer
    Public Property StartingCargo As Decimal
    Public Property MaximumCargo As Decimal
    Public Property PowerPlants As StockPowerPlant()
End Class

Public NotInheritable Class StockPowerPlant
    Public Property Id As String
    Public Property Name As String
    Public Property DesignPoints As Decimal
    Public Property StartingLoadKg As Decimal
    Public Property MaximumLoadKg As Decimal
    Public Property StartingSpeed As Integer
    Public Property MaximumSpeed As Integer
    Public Property StartingAcceleration As Integer
    Public Property MaximumAcceleration As Integer
    Public Property Signature As Integer
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
End Class

Public NotInheritable Class StockDesignResult
    Public Property ChassisId As String
    Public Property PowerPlantId As String
    Public Property Totals As DesignTotals
End Class
