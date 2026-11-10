Public NotInheritable Class RetrofitQuote
    Public Property PartsCostNuyen As Decimal
    Public Property StreetIndex As Decimal
    Public Property Availability As String
    Public Property CargoConsumed As Decimal
    Public Property LoadConsumedKg As Decimal
    Public ReadOnly Property StreetPartsCostNuyen As Decimal
        Get
            Return PartsCostNuyen * StreetIndex
        End Get
    End Property
End Class

Public NotInheritable Class InstalledRetrofit
    Public Property Name As String
    Public Property Quantity As Integer
    Public Property Quote As RetrofitQuote
End Class

Public Module RetrofitCatalog
    Private ReadOnly supported As New HashSet(Of String)(StringComparer.Ordinal) From {
        "crane", "winch", "medical-clinic", "valkyrie-module", "roll-bar",
        "electronics-port", "spotlight", "crash-cage", "retransmission-unit", "datajack-port",
        "bucket-seat", "bench-seat", "folding-bench-seat", "reinforced-bucket-seat",
        "ejection-bucket-seat", "reinforced-ejection-bucket-seat", "cabin-gas-seal", "cabin-water-seal",
        "engine-water-seal", "life-support", "hovercraft-water-seal", "gridlink"
    }

    Friend Function Apply(selections As IReadOnlyList(Of ModificationSelection), designSelections As IReadOnlyList(Of ModificationSelection),
                          chassis As StockChassis, plant As StockPowerPlant, upgrades As UpgradedRatings,
                          totals As DesignTotals) As IReadOnlyList(Of InstalledRetrofit)
        If selections Is Nothing OrElse selections.Count = 0 Then Return Nothing
        Dim entries As New List(Of InstalledRetrofit)
        Dim combined As New List(Of ModificationSelection)
        If designSelections IsNot Nothing Then combined.AddRange(designSelections)
        Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each selection In selections
            If selection Is Nothing Then Throw New ArgumentException("An installed retrofit cannot be null.")
            Dim rule = ModificationCatalog.Rules.SingleOrDefault(Function(candidate) String.Equals(candidate.Id, selection.Id, StringComparison.OrdinalIgnoreCase))
            If rule Is Nothing OrElse Not seen.Add(rule.Id) Then Throw New ArgumentException("An installed retrofit is unknown or repeated.")
            entries.Add(New InstalledRetrofit With {.Name = rule.Name, .Quantity = selection.Quantity,
                .Quote = Quote(rule, selection.Quantity, chassis, plant)})
            combined.Add(New ModificationSelection With {.Id = rule.Id, .Quantity = selection.Quantity})
        Next
        Dim merged = combined.GroupBy(Function(item) item.Id, StringComparer.OrdinalIgnoreCase).
            Select(Function(group) New ModificationSelection With {
                .Id = group.Key, .Quantity = CInt(group.Sum(Function(item) CLng(item.Quantity)))
            }).ToList()
        Dim validationRatings As ModifiedRatings = Nothing
        ModificationCatalog.Apply(merged, chassis, plant, New VehicleDesign With {.Additions = New List(Of DesignAddition)},
                                  upgrades, validationRatings)
        Dim cargo = totals.CargoRemaining - entries.Sum(Function(item) item.Quote.CargoConsumed)
        Dim load = totals.LoadRemainingKg - entries.Sum(Function(item) item.Quote.LoadConsumedKg)
        If cargo < 0D OrElse load < 0D Then Throw New ArgumentException("Installed retrofits exceed the vehicle's available cargo or load.")
        totals.CargoRemaining = cargo
        totals.LoadRemainingKg = load
        Return entries
    End Function

    Public Function AvailableRules(chassis As StockChassis, plant As StockPowerPlant) As IReadOnlyList(Of ModificationRule)
        Return ModificationCatalog.Rules.Where(Function(rule) supported.Contains(rule.Id) AndAlso
            ModificationCatalog.IsCompatible(rule, chassis, plant)).ToArray()
    End Function

    Public Function Quote(rule As ModificationRule, quantity As Integer, chassis As StockChassis, plant As StockPowerPlant) As RetrofitQuote
        If Not supported.Contains(rule.Id) OrElse Not ModificationCatalog.IsCompatible(rule, chassis, plant) Then
            Throw New ArgumentException("This retrofit is unavailable for the selected vehicle.")
        End If
        If quantity < 1 OrElse quantity > ModificationCatalog.MaximumQuantity(rule, chassis) Then
            Throw New ArgumentException("The retrofit quantity or rating is outside the allowed range.")
        End If

        Dim body = CDec(chassis.Body.GetValueOrDefault())
        Select Case rule.Id
            Case "crane"
                Return BuildQuote(2D * quantity, 2D, "6 / 14 days", 35D, body * 80D)
            Case "winch"
                Return BuildQuote(quantity / 2D, 2D, "6 / 14 days", 1D, body * 2.5D)
            Case "medical-clinic"
                Dim factor = quantity + 4D
                Return BuildQuote(factor * factor * factor * 800D, 3D, $"{quantity + 3} / {quantity * 2} days", 75D, 400D)
            Case "valkyrie-module"
                Return BuildQuote(1000000D * quantity, 3D, "10 / 2 weeks", 15D * quantity, 500D * quantity)
            Case "roll-bar"
                Return BuildQuote(2000D, 1D, "3 / 72 hours", 0D, 0D)
            Case "electronics-port"
                Return BuildQuote(1000D * quantity, 1D, "3 / 6 days", 0D, 0D)
            Case "spotlight"
                Return BuildQuote(600D * quantity, 1.5D, "4 / 96 hours", 0D, 0D)
            Case "crash-cage"
                Return BuildQuote(3500D, 2D, "4 / 96 hours", 2D, 25D)
            Case "retransmission-unit"
                Return BuildQuote(25000D * quantity, 3D, "8 / 14 days", 1.5D * quantity, 5D * quantity)
            Case "datajack-port"
                Return BuildQuote(If(chassis.Category = "Bike", 5000D, 2500D), 1.5D, "3 / 72 hours", 1D, 15D)
            Case "bucket-seat"
                Return BuildQuote(700D * quantity, 1D, "3 / 48 hours", 6D * quantity, 100D * quantity)
            Case "bench-seat", "folding-bench-seat"
                Return BuildQuote(750D * quantity, 1D, "3 / 48 hours", 6D * quantity, 150D * quantity)
            Case "reinforced-bucket-seat"
                Return BuildQuote(1500D * quantity, 1D, "3 / 48 hours", 8D * quantity, 250D * quantity)
            Case "ejection-bucket-seat"
                Return BuildQuote(3000D * quantity, 2D, "5 / 10 days", 7D * quantity, 110D * quantity)
            Case "reinforced-ejection-bucket-seat"
                Return BuildQuote(6000D * quantity, 2D, "5 / 10 days", 7D * quantity, 250D * quantity)
            Case "cabin-gas-seal"
                Return BuildQuote(250D * body, 2.5D, "8 / 14 days", 1D, 0D)
            Case "cabin-water-seal"
                Return BuildQuote(750D * body, 2.5D, "8 / 14 days", 1D, 0D)
            Case "engine-water-seal"
                Return BuildQuote(1000D * body, 2.5D, "8 / 14 days", 1D, 0D)
            Case "life-support"
                Return BuildQuote(500D + 1000D * quantity, 2.5D, "8 / 14 days", quantity, 25D * quantity)
            Case "hovercraft-water-seal"
                Return BuildQuote(500D * body, 1D, "4 / 96 hours", 0D, 0D)
            Case "gridlink"
                Return BuildQuote(600D, 1D, "3 / 96 hours", 1D, 0D)
            Case Else
                Throw New ArgumentException("This retrofit has no catalog pricing.")
        End Select
    End Function

    Private Function BuildQuote(cost As Decimal, index As Decimal, availability As String, cargo As Decimal, load As Decimal) As RetrofitQuote
        Return New RetrofitQuote With {
            .PartsCostNuyen = cost, .StreetIndex = index, .Availability = availability,
            .CargoConsumed = cargo, .LoadConsumedKg = load
        }
    End Function
End Module
