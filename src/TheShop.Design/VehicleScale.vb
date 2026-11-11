Public Module VehicleScale
    Public Function RuleBody(chassis As StockChassis) As Decimal
        If chassis.Hull.HasValue Then Return 100D * chassis.Hull.Value * chassis.Hull.Value
        Return chassis.Body.GetValueOrDefault()
    End Function
    Public Function MountBudget(chassis As StockChassis) As Integer
        Return CInt(Math.Min(Integer.MaxValue, RuleBody(chassis)))
    End Function
End Module
