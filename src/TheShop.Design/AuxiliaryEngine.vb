Public NotInheritable Class AuxiliaryEngineRatings
    Public Property Name As String
    Public Property Speed As Integer?
    Public Property MaximumSpeed As Integer?
    Public Property SecondarySpeed As Integer?
    Public Property MaximumSecondarySpeed As Integer?
    Public Property MaximumAcceleration As Integer?
    Public Property Acceleration As Integer?
    Public Property Signature As Integer?
    Public Property Economy As Decimal?
    Public Property EconomyUnit As String
    Public Property FuelCapacity As Decimal?
    Public Property FuelUnit As String
End Class

Public Module AuxiliaryEngine
    Public Function Resolve(id As String, chassis As StockChassis) As StockPowerPlant
        If String.IsNullOrWhiteSpace(id) Then Return Nothing
        If chassis.Body.GetValueOrDefault() = 0 AndAlso Not chassis.Hull.HasValue Then
            Throw New ArgumentException("Body 0 vehicles cannot carry an auxiliary engine.")
        End If
        Dim engine = chassis.PowerPlants.SingleOrDefault(Function(item) String.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase))
        If engine Is Nothing OrElse engine.Id = "unpowered" Then Throw New ArgumentException("The auxiliary engine is unavailable for this chassis.")
        Return engine
    End Function

    Private Function HalfRating(value As Integer?) As Integer?
        If Not value.HasValue Then Return Nothing
        Return CInt(Math.Floor(value.Value / 2D))
    End Function

    Public Function Ratings(engine As StockPowerPlant) As AuxiliaryEngineRatings
        If engine Is Nothing Then Return Nothing
        Dim fuel = If(engine.FuelLiters, If(engine.FuelBars, If(engine.BatteryPowerFactors, If(engine.FuelCellPowerFactors, engine.RocketPowerFactors))))
        Return New AuxiliaryEngineRatings With {
            .Name = engine.Name,
            .Speed = HalfRating(engine.StartingSpeed),
            .Acceleration = HalfRating(engine.StartingAcceleration),
            .MaximumSpeed = HalfRating(engine.MaximumSpeed),
            .SecondarySpeed = HalfRating(engine.StartingSecondarySpeed),
            .MaximumSecondarySpeed = HalfRating(engine.MaximumSecondarySpeed),
            .MaximumAcceleration = HalfRating(engine.MaximumAcceleration),
            .Signature = engine.Signature,
            .Economy = If(engine.StartingEconomyKmPerLiter, If(engine.StartingEconomyKmPerPowerFactor, engine.StartingEconomyKmPerBar)),
            .EconomyUnit = engine.EconomyUnit, .FuelCapacity = fuel,
            .FuelUnit = If(engine.FuelLiters.HasValue, "L", If(engine.FuelBars.HasValue, "bars", "PF"))
        }
    End Function
End Module
