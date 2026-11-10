Imports System.Text.Json.Serialization

Public NotInheritable Class ModificationSelection
    <JsonRequired>
    Public Property Id As String
    <JsonRequired>
    Public Property Quantity As Integer
End Class

Public NotInheritable Class ModificationRule
    Public ReadOnly Property Id As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property QuantityLabel As String
    Public ReadOnly Property Maximum As Integer

    Public Sub New(id As String, name As String, quantityLabel As String, maximum As Integer)
        Me.Id = id
        Me.Name = name
        Me.QuantityLabel = quantityLabel
        Me.Maximum = maximum
    End Sub
End Class

Public Module ModificationCatalog
    Public ReadOnly Property Rules As IReadOnlyList(Of ModificationRule) = Array.AsReadOnly({
        New ModificationRule("airlock", "Airlock", "people per lock", Integer.MaxValue),
        New ModificationRule("crash-cage", "Crash cage", "", 1),
        New ModificationRule("datajack-port", "Datajack port", "", 1),
        New ModificationRule("electronics-port", "Electronics port", "ports", Integer.MaxValue),
        New ModificationRule("nitrous", "Nitrous oxide injectors", "rating", 6),
        New ModificationRule("rigger-adaptation", "Rigger adaptation", "", 1),
        New ModificationRule("spotlight", "Spotlight", "lights", Integer.MaxValue),
        New ModificationRule("performance-tires", "Performance tires", "tires", Integer.MaxValue),
        New ModificationRule("off-road-tires", "Off-road tires", "tires", Integer.MaxValue),
        New ModificationRule("dual-purpose-tires", "Dual-purpose tires", "tires", Integer.MaxValue)
    })

    Public Function IsCompatible(rule As ModificationRule, chassis As StockChassis, plant As StockPowerPlant) As Boolean
        Select Case rule.Id
            Case "airlock"
                Return chassis.Category = "Submarine"
            Case "crash-cage"
                Return Not chassis.IsDrone AndAlso chassis.Category <> "Bike"
            Case "datajack-port", "rigger-adaptation"
                Return Not chassis.IsDrone AndAlso chassis.Id <> "semiballistic" AndAlso chassis.Id <> "suborbital"
            Case "nitrous"
                Return plant.Id = "gasoline" OrElse plant.Id = "diesel"
            Case "performance-tires", "off-road-tires", "dual-purpose-tires"
                Return chassis.Body.GetValueOrDefault() > 0 AndAlso Not chassis.IsDrone AndAlso
                    (chassis.Category = "Bike" OrElse
                    (chassis.Category = "Car" AndAlso chassis.Id <> "tracked-apc" AndAlso Not chassis.Id.EndsWith("-caterpillar", StringComparison.Ordinal)))
            Case Else
                Return True
        End Select
    End Function

    Friend Function Apply(selections As IReadOnlyList(Of ModificationSelection), chassis As StockChassis, plant As StockPowerPlant,
                          design As VehicleDesign) As IReadOnlyList(Of DesignAddition)
        If selections Is Nothing OrElse selections.Count = 0 Then Return Nothing
        If design.Additions Is Nothing Then Throw New ArgumentException("Additions must be an array.")
        Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim result As New List(Of DesignAddition)

        For Each choice In selections
            If choice Is Nothing Then Throw New ArgumentException("A modification selection cannot be null.")
            Dim rule = Rules.SingleOrDefault(Function(item) String.Equals(item.Id, choice.Id, StringComparison.OrdinalIgnoreCase))
            If rule Is Nothing Then Throw New ArgumentException($"Unknown catalog modification '{choice.Id}'.")
            If Not seen.Add(rule.Id) Then Throw New ArgumentException($"'{rule.Name}' was selected more than once.")
            If choice.Quantity < 1 OrElse choice.Quantity > rule.Maximum Then
                Throw New ArgumentException($"'{rule.Name}' requires a quantity from 1 to {rule.Maximum}.")
            End If
            If Not IsCompatible(rule, chassis, plant) Then
                Throw New ArgumentException($"'{rule.Name}' is unavailable for this chassis or power plant.")
            End If
            result.Add(Price(rule.Id, rule.Name, choice.Quantity, chassis.Body.GetValueOrDefault()))
        Next
        If seen.Contains("datajack-port") AndAlso seen.Contains("rigger-adaptation") Then
            Throw New ArgumentException("Rigger adaptation already includes a datajack port.")
        End If
        If seen.Where(Function(id) id.EndsWith("-tires", StringComparison.Ordinal)).Count() > 1 Then
            Throw New ArgumentException("Select one tire type for the design.")
        End If
        design.Additions = design.Additions.Concat(result).ToList()
        Return result
    End Function

    Private Function Price(id As String, name As String, quantity As Integer, body As Integer) As DesignAddition
        Dim item As New DesignAddition With {.Name = name}
        Select Case id
            Case "airlock"
                item.DesignPoints = 500D
                item.CargoConsumed = 40D + 40D * quantity
                item.LoadConsumedKg = 1000D
            Case "crash-cage"
                item.DesignPoints = 40D
                item.LoadConsumedKg = 10D
            Case "datajack-port"
                item.DesignPoints = 25D
            Case "electronics-port"
                item.DesignPoints = 10D * quantity
            Case "nitrous"
                item.DesignPoints = 55D * quantity
                item.CargoConsumed = 1.5D
                item.LoadConsumedKg = 15D
            Case "rigger-adaptation"
                item.DesignPoints = 35D
            Case "spotlight"
                item.DesignPoints = 6D * quantity
            Case "performance-tires"
                item.DirectCostNuyen = 75D * body * quantity
            Case "off-road-tires"
                item.DirectCostNuyen = 125D * body * quantity
            Case "dual-purpose-tires"
                item.DirectCostNuyen = 250D * body * quantity
        End Select
        Return item
    End Function
End Module
