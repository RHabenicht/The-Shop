Public NotInheritable Class FuelExpansionRule
    Public ReadOnly Property SeparateCapacity As Boolean
    Public ReadOnly Property StartingCapacity As Decimal
    Public ReadOnly Property Amount As Decimal
    Public ReadOnly Property Unit As String
    Public ReadOnly Property Points As Decimal
    Public ReadOnly Property Cargo As Decimal
    Public ReadOnly Property Load As Decimal

    Private Sub New(start As Decimal, amount As Decimal, unit As String, points As Decimal, cargo As Decimal, load As Decimal, Optional separate As Boolean = False)
        SeparateCapacity = separate
        StartingCapacity = start
        Me.Amount = amount
        Me.Unit = unit
        Me.Points = points
        Me.Cargo = cargo
        Me.Load = load
    End Sub

    Public Shared Function ForPlant(chassis As StockChassis, plant As StockPowerPlant) As FuelExpansionRule
        Dim electrical = If(plant.BatteryPowerFactors, plant.FuelCellPowerFactors)
        If electrical.HasValue Then
            If Not chassis.Hull.HasValue AndAlso chassis.Body.GetValueOrDefault() = 0 Then
                Return New FuelExpansionRule(electrical.Value, 1D, "PF", 20D, 0D, 0.2D)
            End If
            Return New FuelExpansionRule(electrical.Value, 5D, "PF", 2D, 0D, 1D)
        End If
        If plant.Id = "electric-fuel-cell" AndAlso plant.FuelLiters.HasValue Then
            Return New FuelExpansionRule(0D, 5D, "PF", 2D, 0D, 1D, separate:=True)
        End If
        If plant.Id = "jet-turbine" AndAlso plant.FuelBars.HasValue Then
            Return New FuelExpansionRule(0D, 10D, "L", 1D, 0.2D, 0D, separate:=True)
        End If
        If plant.Id = "methane" AndAlso plant.FuelBars.HasValue Then
            Return New FuelExpansionRule(plant.FuelBars.Value, 5D, "bars", 2D, 0.5D, 0D)
        End If
        If plant.FuelLiters.HasValue Then
            Select Case plant.Id
                Case "gasoline", "diesel"
                    Return New FuelExpansionRule(plant.FuelLiters.Value, 1D, "L", 2D, 0.02D, 0D)
                Case "jet-propeller", "jet-turbine"
                    Return New FuelExpansionRule(plant.FuelLiters.Value, 10D, "L", 1D, 0.2D, 0D)
            End Select
        End If
        Return Nothing
    End Function
End Class
