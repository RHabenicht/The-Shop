Public NotInheritable Class DesignUpgrades
    Public Property Speed As Integer
    Public Property SecondarySpeed As Integer
    Public Property Acceleration As Integer
    Public Property Handling As Integer
    Public Property OnRoadHandling As Integer
    Public Property OffRoadHandling As Integer
    Public Property CargoSteps As Integer
    Public Property LoadSteps As Integer
    Public Property Signature As Integer
    Public Property EconomySteps As Integer
    Public Property FuelSteps As Integer
    Public Property DepthSteps As Integer

    Friend Function Apply(chassis As StockChassis, plant As StockPowerPlant, design As VehicleDesign,
                          cargoOverride As Decimal?, loadOverride As Decimal?) As UpgradedRatings
        Dim steps = {Speed, SecondarySpeed, Acceleration, Handling, OnRoadHandling, OffRoadHandling, CargoSteps, LoadSteps, Signature, EconomySteps, FuelSteps, DepthSteps}
        If steps.Any(Function(value) value < 0) Then Throw New ArgumentException("Upgrade steps cannot be negative.")
        If steps.All(Function(value) value = 0) Then Return Nothing
        If chassis.Hull.HasValue AndAlso LoadSteps > 0 Then Throw New ArgumentException("Use manual entries for Hull-rated load increases.")
        If design.Additions Is Nothing Then Throw New ArgumentException("Additions must be an array.")
        If (CargoSteps > 0 AndAlso cargoOverride.HasValue) OrElse (LoadSteps > 0 AndAlso loadOverride.HasValue) Then
            Throw New ArgumentException("Clear the matching capacity override before applying an automatic cargo or load upgrade.")
        End If

        Dim ratings As New UpgradedRatings With {
            .Speed = Increase(plant.StartingSpeed, plant.MaximumSpeed, Speed, "Speed"),
            .SecondarySpeed = Increase(plant.StartingSecondarySpeed, plant.MaximumSecondarySpeed, SecondarySpeed, "Secondary speed"),
            .Acceleration = Increase(plant.StartingAcceleration, plant.MaximumAcceleration, Acceleration, "Acceleration"),
            .Handling = ImproveHandling(chassis.Handling, Handling, "Handling"),
            .OnRoadHandling = ImproveHandling(chassis.OnRoadHandling, OnRoadHandling, "On-road Handling"),
            .OffRoadHandling = ImproveHandling(chassis.OffRoadHandling, OffRoadHandling, "Off-road Handling"),
            .Signature = Increase(plant.Signature, Nothing, Signature, "Signature")
        }

        If EconomySteps > 0 Then
            Dim starting = If(plant.StartingEconomyKmPerLiter, If(plant.StartingEconomyKmPerPowerFactor, plant.StartingEconomyKmPerBar))
            Dim maximum = If(plant.MaximumEconomyKmPerLiter, If(plant.MaximumEconomyKmPerPowerFactor, plant.MaximumEconomyKmPerBar))
            If Not starting.HasValue OrElse Not maximum.HasValue Then Throw New ArgumentException("Economy is unavailable for this power plant.")
            Dim economy = starting.Value * (1D + 0.05D * EconomySteps)
            If economy > maximum.Value Then Throw New ArgumentException($"Economy exceeds the power plant maximum of {maximum.Value} {plant.EconomyUnit}.")
            ratings.Economy = economy
            ratings.EconomyUnit = plant.EconomyUnit
        End If

        design.CargoCapacity += CargoSteps * 0.2D
        design.LoadCapacityKg += LoadSteps * If(chassis.Body.GetValueOrDefault() = 0, 1D, 10D)
        Dim signatureLevels = CDec(Signature)
        Dim squared = signatureLevels * signatureLevels
        Dim points = 2D * (CDec(Speed) + SecondarySpeed) + 25D * (CDec(Acceleration) + Handling + OnRoadHandling + OffRoadHandling) +
                     CargoSteps + LoadSteps + 5D * EconomySteps + 200D * squared * squared
        Dim cargoUsed As Decimal
        Dim loadUsed As Decimal
        If DepthSteps > 0 Then
            ratings.Depth = Increase(chassis.StartingDepth, chassis.MaximumDepth, 10 * DepthSteps, "Depth")
            points += 2D * DepthSteps
            Dim size = CDec(If(chassis.Hull, chassis.Body).GetValueOrDefault())
            Dim hullFactor = If(chassis.Hull.HasValue, 100D * size * size, 1D)
            loadUsed += hullFactor * size * 2D * DepthSteps
        End If

        If FuelSteps > 0 Then
            Dim fuel = FuelExpansionRule.ForPlant(chassis, plant)
            If fuel Is Nothing Then Throw New ArgumentException("Fuel expansion is unavailable for this power plant and unit.")
            ratings.FuelCapacity = fuel.StartingCapacity + fuel.Amount * FuelSteps
            ratings.FuelUnit = fuel.Unit
            points += fuel.Points * FuelSteps
            cargoUsed += fuel.Cargo * FuelSteps
            loadUsed += fuel.Load * FuelSteps
            If fuel.Unit <> "PF" AndAlso chassis.TakeoffLandingProfile IsNot Nothing Then
                ratings.TakeoffLandingDistanceMultiplier = 1.1D
            End If
        End If

        design.Additions = New List(Of DesignAddition)(design.Additions) From {
            New DesignAddition With {.Name = "Catalog design upgrades", .DesignPoints = points, .CargoConsumed = cargoUsed, .LoadConsumedKg = loadUsed}
        }
        Return ratings
    End Function

    Private Shared Function Increase(start As Integer?, maximum As Integer?, steps As Integer, label As String) As Integer?
        If steps = 0 Then Return start
        If Not start.HasValue Then Throw New ArgumentException($"{label} is unavailable for this vehicle.")
        Dim adjusted = start.Value + CLng(steps)
        If maximum.HasValue AndAlso adjusted > maximum.Value Then
            Throw New ArgumentException($"{label} exceeds the power plant maximum of {maximum.Value}.")
        End If
        Return CInt(adjusted)
    End Function

    Private Shared Function ImproveHandling(start As Integer?, steps As Integer, label As String) As Integer?
        If steps = 0 Then Return start
        If Not start.HasValue Then Throw New ArgumentException($"{label} is unavailable for this chassis.")
        Dim minimum = Math.Ceiling(start.Value / 2D)
        If start.Value - CLng(steps) < minimum Then
            Throw New ArgumentException($"{label} cannot be reduced below {minimum}.")
        End If
        Return start.Value - steps
    End Function
End Class

Public NotInheritable Class UpgradedRatings
    Public Property Speed As Integer?
    Public Property SecondarySpeed As Integer?
    Public Property Acceleration As Integer?
    Public Property Handling As Integer?
    Public Property OnRoadHandling As Integer?
    Public Property OffRoadHandling As Integer?
    Public Property Signature As Integer?
    Public Property Economy As Decimal?
    Public Property EconomyUnit As String
    Public Property Depth As Integer?
    Public Property FuelCapacity As Decimal?
    Public Property FuelUnit As String
    Public Property TakeoffLandingDistanceMultiplier As Decimal?
End Class
