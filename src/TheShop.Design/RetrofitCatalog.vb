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

Public Module RetrofitCatalog
    Private ReadOnly supported As New HashSet(Of String)(StringComparer.Ordinal) From {
        "crane", "winch", "medical-clinic", "valkyrie-module", "roll-bar",
        "electronics-port", "spotlight", "crash-cage", "retransmission-unit"
    }

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
