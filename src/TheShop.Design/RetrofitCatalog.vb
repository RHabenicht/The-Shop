Public NotInheritable Class RetrofitQuote
    Public Property SourcePage As Integer
    Public Property InstallationBaseHours As Decimal?
    Public Property EquipmentRequired As String
    Public Property InstallationTest As String
    Public Property PartsCostNuyen As Decimal
    Public Property StreetIndex As Decimal?
    Public Property Availability As String
    Public Property ExternalCargoCapacity As Decimal
    Public Property CargoConsumed As Decimal
    Public Property LoadConsumedKg As Decimal
    Public ReadOnly Property StreetPartsCostNuyen As Decimal?
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
        "engine-water-seal", "life-support", "hovercraft-water-seal", "gridlink",
        "external-fixed-firmpoint", "external-fixed-hardpoint", "internal-fixed-firmpoint", "internal-fixed-hardpoint",
        "pintle-mount", "ring-mount", "medium-launch-control", "heavy-launch-control",
        "smartlink-integration-1", "smartlink-integration-2", "battletac-fddm", "battletac-ivis", "dipping-sonar", "ccss-integration",
        "sensors", "sonar", "ecm", "eccm", "electronic-deception", "electronic-counter-deception",
        "improved-anti-theft-security", "anti-theft-electric-system", "proximity-alert", "autonav", "advanced-drone-pilot", "external-cargo-mounts"
    }

    Friend Function Apply(selections As IReadOnlyList(Of ModificationSelection), designSelections As IReadOnlyList(Of ModificationSelection),
                          chassis As StockChassis, plant As StockPowerPlant, upgrades As UpgradedRatings,
                          totals As DesignTotals, validationCargo As Decimal, validationLoad As Decimal,
                          ByRef finalRatings As ModifiedRatings, vehicleStreetIndex As Decimal, vehicleAvailability As VehicleAvailability, originalCargoCapacity As Decimal,
                          priorAdditions As IReadOnlyList(Of DesignAddition)) As IReadOnlyList(Of InstalledRetrofit)
        If selections Is Nothing OrElse selections.Count = 0 Then Return Nothing
        Dim entries As New List(Of InstalledRetrofit)
        Dim combined As New List(Of ModificationSelection)
        If designSelections IsNot Nothing Then combined.AddRange(designSelections)
        Dim quoteContext = If(chassis.FactoryEquipment, New List(Of ModificationSelection)).Concat(combined).Concat(selections).ToArray()
        Dim seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each selection In selections
            If selection Is Nothing Then Throw New ArgumentException("An installed retrofit cannot be null.")
            Dim rule = ModificationCatalog.Rules.SingleOrDefault(Function(candidate) String.Equals(candidate.Id, selection.Id, StringComparison.OrdinalIgnoreCase))
            If rule Is Nothing OrElse Not seen.Add(rule.Id) Then Throw New ArgumentException("An installed retrofit is unknown or repeated.")
            entries.Add(New InstalledRetrofit With {.Name = rule.Name, .Quantity = selection.Quantity,
                .Quote = Quote(rule, selection.Quantity, chassis, plant, selection.StreetIndexOverride, totals.BaseCostNuyen, quoteContext, vehicleStreetIndex, vehicleAvailability, True, originalCargoCapacity)})
            combined.Add(New ModificationSelection With {.Id = rule.Id, .Quantity = selection.Quantity})
        Next
        If selections.Any(Function(item) item.Id = "cabin-overpressure") AndAlso selections.Any(Function(item) item.Id = "cabin-gas-seal" OrElse item.Id = "cabin-water-seal") Then
            entries.Single(Function(item) item.Name = ModificationCatalog.Rules.Single(Function(rule) rule.Id = "cabin-overpressure").Name).Quote.CargoConsumed -= 1D
        End If
        Dim groups = combined.GroupBy(Function(item) item.Id, StringComparer.OrdinalIgnoreCase).ToArray()
        For Each group In groups.Where(Function(items) items.Count() > 1)
            Dim rule = ModificationCatalog.Rules.Single(Function(candidate) String.Equals(candidate.Id, group.Key, StringComparison.OrdinalIgnoreCase))
            If IsReplacementRating(rule.Id) Then
                If group.Last().Quantity <= group.First().Quantity Then Throw New ArgumentException($"Choose a higher replacement rating for '{rule.Name}'.")
            ElseIf Not {"units", "seats", "ports", "sets", "racks", "mounts", "arms", "lights", "tires", "weapons", "10 person-hour blocks", "added points", "points", "levels"}.Contains(rule.QuantityLabel) Then
                Throw New ArgumentException($"'{rule.Name}' is already present in the design.")
            End If
        Next
        Dim merged = groups.Where(Function(group) Not String.Equals(group.Key, "external-cargo-mounts", StringComparison.OrdinalIgnoreCase)).Select(Function(group) New ModificationSelection With {
                .Id = group.Key, .Quantity = If(IsReplacementRating(group.Key), group.Last().Quantity, CInt(group.Sum(Function(item) CLng(item.Quantity))))
            }).ToList()
        If DesignEnhancements.Has(designSelections, "gunnery-recoil-adjusters") AndAlso Not DesignEnhancements.Has(selections, "gunnery-recoil-adjusters") Then
            If DesignEnhancements.Has(selections, "weapon-gyro-stabilization") Then Throw New ArgumentException("Gunnery recoil adjusters and gyro-stabilization are incompatible.")
            ' Manufacture-time uniform adjusters cover the original mounts, not new retrofit mounts.
            merged.RemoveAll(Function(item) item.Id = "gunnery-recoil-adjusters")
        End If
        Dim validationRatings As ModifiedRatings = Nothing
        Dim baseline As New VehicleDesign With {.Additions = New List(Of DesignAddition)(priorAdditions), .CargoCapacity = validationCargo, .LoadCapacityKg = validationLoad}
        Dim baselineRatings As ModifiedRatings = Nothing
        ModificationCatalog.Apply(designSelections, chassis, plant, baseline, upgrades, baselineRatings)
        Dim validation As New VehicleDesign With {.Additions = New List(Of DesignAddition)(priorAdditions), .CargoCapacity = validationCargo, .LoadCapacityKg = validationLoad}
        ModificationCatalog.Apply(merged, chassis, plant, validation, upgrades, validationRatings, True)
        Dim oldSpeed = If(baselineRatings?.Speed, If(upgrades?.Speed, plant.StartingSpeed)).GetValueOrDefault()
        Dim oldAccel = If(baselineRatings?.Acceleration, If(upgrades?.Acceleration, plant.StartingAcceleration)).GetValueOrDefault()
        Dim oldSecondary = If(baselineRatings?.SecondarySpeed, If(upgrades?.SecondarySpeed, plant.StartingSecondarySpeed)).GetValueOrDefault()
        For Each installed In selections
            Select Case installed.Id
                Case "engine-customization-speed"
                    If validationRatings.Speed.GetValueOrDefault() > Math.Max(oldSpeed + 30D, Math.Ceiling(oldSpeed * 1.5D)) Then Throw New ArgumentException("Engine retrofit exceeds the 1.5 × original Speed limit.")
                Case "engine-customization-acceleration"
                    If validationRatings.Acceleration.GetValueOrDefault() > Math.Max(oldAccel + 2D, Math.Ceiling(oldAccel * 1.5D)) Then Throw New ArgumentException("Engine retrofit exceeds the 1.5 × original Acceleration limit.")
                Case "engine-customization-secondary-speed"
                    If validationRatings.SecondarySpeed.GetValueOrDefault() > Math.Max(oldSecondary + 30D, Math.Ceiling(oldSecondary * 1.5D)) Then Throw New ArgumentException("Engine retrofit exceeds the 1.5 × original secondary Speed limit.")
                Case "engine-customization-load"
                    If validation.LoadCapacityKg > Math.Max(baseline.LoadCapacityKg + VehicleScale.RuleBody(chassis) * 50D, baseline.LoadCapacityKg * 1.5D) Then Throw New ArgumentException("Engine retrofit exceeds the 1.5 × original Load limit.")
                Case "turbocharging"
                    If validationRatings.Speed.GetValueOrDefault() > Math.Max(oldSpeed + 15D, oldSpeed * 1.5D) Then Throw New ArgumentException("Turbocharging retrofit exceeds the 1.5 × original Speed limit.")
                    If plant.StartingSecondarySpeed.HasValue AndAlso validationRatings.SecondarySpeed.GetValueOrDefault() > Math.Max(oldSecondary + 15D, oldSecondary * 1.5D) Then Throw New ArgumentException("Turbocharging retrofit exceeds the 1.5 × original secondary Speed limit.")
            End Select
        Next
        Dim cargo = totals.CargoRemaining + validation.CargoCapacity - baseline.CargoCapacity - entries.Sum(Function(item) item.Quote.CargoConsumed)
        Dim load = totals.LoadRemainingKg + validation.LoadCapacityKg - baseline.LoadCapacityKg - entries.Sum(Function(item) item.Quote.LoadConsumedKg)
        If cargo < 0D OrElse load < 0D Then Throw New ArgumentException("Installed retrofits exceed the vehicle's available cargo or load.")
        finalRatings = validationRatings
        totals.CargoRemaining = cargo
        totals.LoadRemainingKg = load
        Return entries
    End Function

    Public ReadOnly Property Rules As IReadOnlyList(Of ModificationRule)
        Get
            Return ModificationCatalog.Rules.Where(Function(rule) (supported.Contains(rule.Id) OrElse ExtendedRetrofits.Contains(rule.Id))).ToArray()
        End Get
    End Property

    Public Function MaximumQuantity(rule As ModificationRule, chassis As StockChassis, Optional originalCargoCapacity As Decimal? = Nothing) As Integer
        If rule.Id = "external-cargo-mounts" AndAlso originalCargoCapacity.HasValue Then Return CInt(Math.Min(Integer.MaxValue, Math.Max(0D, Math.Floor(originalCargoCapacity.Value))))
        Return If(rule.Id = "nitrous", Math.Min(5, ModificationCatalog.MaximumQuantity(rule, chassis)), ModificationCatalog.MaximumQuantity(rule, chassis))
    End Function

    Public Function CombinedSelections(design As IReadOnlyList(Of ModificationSelection), installed As IReadOnlyList(Of ModificationSelection)) As IReadOnlyList(Of ModificationSelection)
        Return NormalizeSelections(design).Concat(NormalizeSelections(installed)).GroupBy(Function(item) item.Id, StringComparer.OrdinalIgnoreCase).
            Select(Function(group) New ModificationSelection With {.Id = group.Key, .Quantity = If(IsReplacementRating(group.Key), group.Last().Quantity, CInt(group.Sum(Function(item) CLng(item.Quantity))))}).ToArray()
    End Function

    Friend Function NormalizeSelections(selections As IReadOnlyList(Of ModificationSelection)) As IReadOnlyList(Of ModificationSelection)
        If selections Is Nothing Then Return Array.Empty(Of ModificationSelection)()
        If selections.Any(Function(item) item Is Nothing OrElse String.IsNullOrWhiteSpace(item.Id)) Then Throw New ArgumentException("A component selection requires an ID.")
        Return selections.Select(Function(item) New ModificationSelection With {.Id = item.Id.ToLowerInvariant(), .Quantity = item.Quantity, .StreetIndexOverride = item.StreetIndexOverride}).ToArray()
    End Function

    Private Function IsReplacementRating(id As String) As Boolean
        Return {"sensors", "sonar", "ecm", "eccm", "electronic-deception", "electronic-counter-deception", "improved-anti-theft-security", "autonav", "advanced-drone-pilot", "robotic-pilot"}.Contains(id.ToLowerInvariant())
    End Function

    Private Function Compatible(rule As ModificationRule, chassis As StockChassis, plant As StockPowerPlant, Optional originalCargoCapacity As Decimal? = Nothing) As Boolean
        If rule.Id = "external-cargo-mounts" Then Return If(originalCargoCapacity, chassis.StartingCargo) >= 1D
        Return ModificationCatalog.IsCompatible(rule, chassis, plant)
    End Function

    Public Function AvailableRules(chassis As StockChassis, plant As StockPowerPlant, Optional originalCargoCapacity As Decimal? = Nothing) As IReadOnlyList(Of ModificationRule)
        Return ModificationCatalog.Rules.Where(Function(rule) (supported.Contains(rule.Id) OrElse ExtendedRetrofits.Contains(rule.Id)) AndAlso
            Compatible(rule, chassis, plant, originalCargoCapacity) AndAlso MaximumQuantity(rule, chassis, originalCargoCapacity) >= 1).ToArray()
    End Function

    Public Function Quote(rule As ModificationRule, quantity As Integer, chassis As StockChassis, plant As StockPowerPlant,
                          Optional streetIndexOverride As Decimal? = Nothing, Optional listCost As Decimal? = Nothing,
                          Optional context As IReadOnlyList(Of ModificationSelection) = Nothing,
                          Optional vehicleStreetIndex As Decimal? = Nothing, Optional vehicleAvailability As VehicleAvailability = Nothing,
                          Optional contextIncludesSelection As Boolean = False, Optional originalCargoCapacity As Decimal? = Nothing) As RetrofitQuote
        context = NormalizeSelections(If(context, chassis.FactoryEquipment))
        context = CombinedSelections(context, If(contextIncludesSelection, Nothing, New ModificationSelection() {New ModificationSelection With {.Id = rule.Id, .Quantity = quantity}}))
        If rule.Id = "battletac-fddm" OrElse rule.Id = "battletac-ivis" Then
            Dim pilot = Math.Max(chassis.Pilot.GetValueOrDefault(), context.Where(Function(item) item.Id = "advanced-drone-pilot" OrElse item.Id = "robotic-pilot").Select(Function(item) item.Quantity).DefaultIfEmpty(0).Max())
            If pilot < 1 AndAlso Not DesignEnhancements.Has(context, "remote-control-interface") Then Throw New ArgumentException("Drone electronics require a Pilot rating or remote-control interface.")
        End If
        Dim result = BaseQuote(rule, quantity, chassis, plant, If(listCost, (chassis.DesignPoints + plant.DesignPoints) * chassis.Markup * 100D), context, originalCargoCapacity)
        If {"aircraft-drop-tanks", "sidecar", "runflat-upgrade", "standard-tires", "performance-tires", "off-road-tires", "dual-purpose-tires", "spare-standard-tires", "spare-performance-tires", "spare-off-road-tires", "spare-dual-purpose-tires"}.Contains(rule.Id) Then
            result.StreetIndex = vehicleStreetIndex
            result.Availability = If(vehicleAvailability Is Nothing, "Same as vehicle (R3 Revised p. 124)", $"{vehicleAvailability.Rating} / {vehicleAvailability.BaseDays} days")
        End If
        If result.Availability.StartsWith("Same as vehicle", StringComparison.Ordinal) AndAlso vehicleAvailability IsNot Nothing Then result.Availability = $"{vehicleAvailability.Rating} / {vehicleAvailability.BaseDays} days"
        If rule.Id = "anti-theft-electric-system" OrElse rule.Id = "proximity-alert" Then
            Dim security = If(context, Array.Empty(Of ModificationSelection)()).LastOrDefault(Function(item) item.Id = "improved-anti-theft-security")
            If security IsNot Nothing Then
                Dim systemQuote = BaseQuote(ModificationCatalog.Rules.Single(Function(item) item.Id = security.Id), security.Quantity, chassis, plant, If(listCost, 0D), context)
                result.StreetIndex = systemQuote.StreetIndex
                result.Availability = systemQuote.Availability
            Else
                result.StreetIndex = vehicleStreetIndex
                result.Availability = If(vehicleAvailability Is Nothing, "Same as vehicle's security system", $"{vehicleAvailability.Rating} / {vehicleAvailability.BaseDays} days")
            End If
        End If
        If streetIndexOverride.HasValue Then
            If streetIndexOverride.Value <= 0D Then Throw New ArgumentException("A retrofit Street Index must be positive.")
            result.StreetIndex = streetIndexOverride.Value
        End If
        InstallationRules.Describe(result, rule, quantity, chassis, context)
        Return result
    End Function

    Private Function BaseQuote(rule As ModificationRule, quantity As Integer, chassis As StockChassis, plant As StockPowerPlant, listCost As Decimal, context As IReadOnlyList(Of ModificationSelection), Optional originalCargoCapacity As Decimal? = Nothing) As RetrofitQuote
        If Not (supported.Contains(rule.Id) OrElse ExtendedRetrofits.Contains(rule.Id)) OrElse Not Compatible(rule, chassis, plant, originalCargoCapacity) Then
            Throw New ArgumentException("This retrofit is unavailable for the selected vehicle.")
        End If
        If quantity < 1 OrElse quantity > MaximumQuantity(rule, chassis, originalCargoCapacity) Then
            Throw New ArgumentException("The retrofit quantity or rating is outside the allowed range.")
        End If

        If ExtendedRetrofits.Contains(rule.Id) Then Return ExtendedRetrofits.Quote(rule, quantity, chassis, plant, listCost, context)
        If ElectronicRetrofits.Contains(rule.Id) Then Return ElectronicRetrofits.Quote(rule.Id, quantity)
        Dim body = VehicleScale.RuleBody(chassis)
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
            Case "external-fixed-firmpoint", "medium-launch-control"
                Return BuildQuote(750D * quantity, 2D, "6 / 7 days", quantity, 10D * quantity)
            Case "external-fixed-hardpoint", "heavy-launch-control"
                Return BuildQuote(2000D * quantity, 2D, "6 / 7 days", 2D * quantity, 10D * quantity)
            Case "internal-fixed-firmpoint"
                Return BuildQuote(1500D * quantity, 2D, "6 / 7 days", 5D * quantity, 10D * quantity)
            Case "internal-fixed-hardpoint"
                Return BuildQuote(3000D * quantity, 2D, "6 / 7 days", 7D * quantity, 10D * quantity)
            Case "pintle-mount"
                Return BuildQuote(50D * quantity, 1.5D, "4 / 96 hours", 0D, 0D)
            Case "ring-mount"
                Return BuildQuote(3000D * quantity, 2D, "8 / 14 days", quantity, 25D * quantity)
            Case "smartlink-integration-1"
                Return BuildQuote(650D * quantity, 1D, "4 / 48 hours", quantity, 0D)
            Case "smartlink-integration-2"
                Return BuildQuote(900D * quantity, 2D, "6 / 48 hours", quantity, 0D)
            Case "battletac-fddm"
                Return BuildQuote(35000D, 3D, "10 / 21 days", 0D, 0D)
            Case "battletac-ivis"
                Return BuildQuote(25000D, 3D, "8 / 14 days", 0D, 0D)
            Case "dipping-sonar"
                Return BuildQuote(15000D * quantity, 4.5D, "8 / 21 days", 16D, 50D)
            Case "ccss-integration"
                Return BuildQuote(400000D * quantity, 4D, "6 / 21 days", 0D, 0D)
            Case "improved-anti-theft-security"
                If quantity < 3 Then Throw New ArgumentException("Improved security requires rating 3 or higher.")
                Dim rate = If(quantity <= 6, 400D, If(quantity <= 9, 1000D, 5000D))
                Return BuildQuote(rate * quantity, If(quantity <= 6, 1.25D, If(quantity <= 9, 1.5D, 2D)),
                                  If(quantity <= 6, "4 / 7 days", If(quantity <= 9, "5 / 10 days", "6 / 14 days")), 0D, 0D)
            Case "anti-theft-electric-system"
                Return BuildQuote(2000D, Nothing, "Use the security system's Availability", 0D, 0D)
            Case "proximity-alert"
                Return BuildQuote(250D, Nothing, "Use the security system's Availability", 0D, 0D)
            Case "autonav"
                Dim prices = {0D, 500D, 1000D, 5000D, 15000D}
                Dim indices = {0D, 1D, 1D, 1.5D, 2D}
                Dim availability = {"", "2 / 96 hours", "3 / 6 days", "4 / 8 days", "6 / 14 days"}
                Return BuildQuote(prices(quantity), indices(quantity), availability(quantity), 0D, 0D)
            Case "advanced-drone-pilot"
                Dim prices = {0D, 0D, 5000D, 25000D, 500000D, 2500000D}
                Return BuildQuote(prices(quantity), If(quantity = 5, CType(Nothing, Decimal?), If(quantity = 4, 4D, 2D)),
                                  If(quantity = 5, "14 / 70 days", If(quantity = 4, "10 / 35 days", "6 / 14 days")), 0D, 0D)
            Case "external-cargo-mounts"
                Dim result = BuildQuote(250D * quantity, 1D, "3 / 24 hours", 0D, 0D)
                result.ExternalCargoCapacity = quantity
                Return result
            Case Else
                Throw New ArgumentException("This retrofit has no catalog pricing.")
        End Select
    End Function

    Private Function BuildQuote(cost As Decimal, index As Decimal?, availability As String, cargo As Decimal, load As Decimal) As RetrofitQuote
        Return New RetrofitQuote With {
            .PartsCostNuyen = cost, .StreetIndex = index, .Availability = availability,
            .CargoConsumed = cargo, .LoadConsumedKg = load
        }
    End Function
End Module
