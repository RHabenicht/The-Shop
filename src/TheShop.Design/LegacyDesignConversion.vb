Imports System.Text.Json
Imports System.Text.RegularExpressions

Public NotInheritable Class LegacyOptionMapping
    Public Property Name As String
    Public Property Kind As String
    Public Property Id As String
    Public Property Flag As Boolean
    Public Property Quantity As Integer?
    Public Property TurretStyle As String
    Public Property Reason As String
End Class

Public Module LegacyDesignConversion
    Private NotInheritable Class Entry
        Public Property Name As String
        Public Property Level As Integer
        Public Property Mapping As LegacyOptionMapping
    End Class

    Private NotInheritable Class ConversionGroup
        Public Property Entries As New List(Of Entry)
        Public Property Apply As New List(Of Action(Of StockDesign))
        Public Property Failure As String
    End Class

    Private ReadOnly mappings As IReadOnlyList(Of LegacyOptionMapping) = ReadResource(Of LegacyOptionMapping())("LegacyOptionMappings.json")
    Private ReadOnly factorySensors As Dictionary(Of String, Integer) = ReadResource(Of Dictionary(Of String, Integer))("LegacyChassisSensors.json")

    Private Function ReadResource(Of T)(name As String) As T
        Using data = GetType(StockDesign).Assembly.GetManifestResourceStream("TheShop.Design." & name)
            Return JsonSerializer.Deserialize(Of T)(data)
        End Using
    End Function

    Private Function Key(name As String) As String
        Return Regex.Replace(If(name, "").ToLowerInvariant(), "[^a-z0-9]", "")
    End Function

    Public Function FindComponents(reference As JsonElement, catalog As IReadOnlyList(Of StockChassis)) As (Chassis As StockChassis, Plant As StockPowerPlant)
        Dim oldName = reference.GetProperty("Chassis").GetProperty("Name").GetString()
        Dim chassisKey = Key(oldName.Replace(" (clean)", "", StringComparison.OrdinalIgnoreCase))
        Dim aliases As New Dictionary(Of String, String) From {
            {"commutersubcomp", "subcompact"}, {"apctracked", "tracked-apc"}, {"apcwheeled", "wheeled-apc"},
            {"singleengine", "single-engine-aircraft"}, {"twinengine", "twin-engine-aircraft"}
        }
        For Each size In {"micro", "small", "medium", "large"}
            aliases("rotaryuav" & size) = size & "-rotary-wing-uav"
            aliases("fixedwinguav" & size) = size & "-fixed-wing-uav"
            aliases("tiltwinguav" & size) = size & "-tilt-wing-uav"
        Next
        Dim candidates = catalog.Where(Function(item) Key(item.Name) = chassisKey OrElse
            (aliases.ContainsKey(chassisKey) AndAlso item.Id = aliases(chassisKey))).ToArray()
        If candidates.Length <> 1 Then Return (Nothing, Nothing)
        Dim engineKey = Key(reference.GetProperty("Engine").GetProperty("Name").GetString().Split(","c)(0))
        If engineKey = "electric" Then engineKey = "electricbattery"
        If engineKey = "turbine" Then engineKey = "jetturbine"
        Dim plants = candidates(0).PowerPlants.Where(Function(item) Key(item.Name) = engineKey).ToArray()
        Return (candidates(0), If(plants.Length = 1, plants(0), Nothing))
    End Function

    Public Function CreateDesign(reference As JsonElement, chassisId As String, plantId As String,
                                 catalog As IReadOnlyList(Of StockChassis)) As StockDesign
        Dim chassis = StockCatalog.FindById(catalog, chassisId)
        Dim baseDesign As New StockDesign With {
            .ChassisId = chassisId, .PowerPlantId = plantId,
            .Name = reference.GetProperty("Name").GetString(), .LegacyReference = reference,
            .CatalogModifications = New List(Of ModificationSelection), .LegacyImportNotes = New List(Of String)
        }
        Dim components = FindComponents(reference, catalog)
        If components.Chassis Is Nothing OrElse components.Plant Is Nothing OrElse components.Chassis.Id <> chassisId OrElse components.Plant.Id <> plantId Then
            baseDesign.LegacyImportNotes.Add($"Review manually: Source chassis/engine are being rebuilt on {chassis.Name} / {plantId}. Original component ratings and prices are not carried over.")
        End If
        Dim markups As JsonElement
        If reference.TryGetProperty("Markups", markups) Then
            If markups.GetProperty("Ambulance").GetDouble() > 0 Then
                baseDesign.SpecialtyClass = VehicleSpecialtyClass.Medical
            ElseIf markups.GetProperty("NonCommercial").GetDouble() > 0 Then
                baseDesign.SpecialtyClass = VehicleSpecialtyClass.NonCombat
            End If
            If markups.GetProperty("Luxury").GetDouble() > 0 Then baseDesign.AvailabilityClass = VehicleAvailabilityClass.Luxury
            If markups.GetProperty("Unusual").GetDouble() <> 0 OrElse markups.GetProperty("Special").GetDouble() <> 0 Then
                baseDesign.LegacyImportNotes.Add("Review manually: Old unusual/special price modifiers were not carried into revised pricing.")
            End If
        End If
        Dim entries As New List(Of Entry)
        For Each item In reference.GetProperty("Modifications").EnumerateArray()
            Dim name = item.GetProperty("Name").GetString()
            Dim mapping = mappings.SingleOrDefault(Function(candidate) Key(candidate.Name) = Key(name))
            If mapping Is Nothing Then
                Dim rule = ModificationCatalog.Rules.SingleOrDefault(Function(candidate) Key(candidate.Name) = Key(name))
                If rule IsNot Nothing Then mapping = New LegacyOptionMapping With {
                    .Name = name, .Id = rule.Id, .Kind = "Catalog", .Flag = rule.Maximum = 1 AndAlso rule.QuantityLabel = ""}
            End If
            Dim level = item.GetProperty("Level").GetInt32()
            If mapping Is Nothing OrElse level < 0 Then
                baseDesign.LegacyImportNotes.Add($"Review manually: {name} (legacy level {level}). No unambiguous revised conversion.")
            ElseIf mapping.Kind = "Unresolved" Then
                baseDesign.LegacyImportNotes.Add($"Review manually: {name}. {mapping.Reason}")
            Else
                entries.Add(New Entry With {.Name = name, .Level = level, .Mapping = mapping})
            End If
        Next
        Dim groups As New Dictionary(Of String, ConversionGroup)(StringComparer.Ordinal)
        For Each item In entries
            Dim entryItem = item
            Dim groupName = GroupFor(item)
            If Not groups.ContainsKey(groupName) Then groups.Add(groupName, New ConversionGroup())
            Dim group = groups(groupName)
            group.Entries.Add(item)
            group.Apply.Add(Sub(design) ConvertEntry(design, entryItem, entries, reference, chassis))
        Next
        ' Try the complete vehicle first: space upgrades and prerequisites can occur after their users in an old save.
        Dim result = CopyDesign(baseDesign)
        Try
            For Each group In groups.Values
                For Each operation As Action(Of StockDesign) In group.Apply
                    operation(result)
                Next
            Next
            result.Calculate(catalog)
            For Each item In entries
                result.LegacyImportNotes.Add($"Repriced using Rigger 3 Revised: {item.Name}.")
            Next
        Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is OverflowException
            result = CopyDesign(baseDesign)
            Dim pending = groups.Values.ToList()
            Dim madeProgress As Boolean
            Do
                madeProgress = False
                For Each group In pending.ToArray()
                    Dim attempt = CopyDesign(result)
                    Try
                        For Each operation As Action(Of StockDesign) In group.Apply
                            operation(attempt)
                        Next
                        attempt.Calculate(catalog)
                        result = attempt
                        pending.Remove(group)
                        madeProgress = True
                        For Each item In group.Entries
                            result.LegacyImportNotes.Add($"Repriced using Rigger 3 Revised: {item.Name}.")
                        Next
                    Catch failure As Exception When TypeOf failure Is ArgumentException OrElse TypeOf failure Is OverflowException
                        group.Failure = failure.Message
                    End Try
                Next
            Loop While madeProgress AndAlso pending.Count > 0
            For Each group In pending
                For Each item In group.Entries
                    result.LegacyImportNotes.Add($"Review manually: {item.Name}. Not applied: {group.Failure}")
                Next
            Next
        End Try
        If result.CatalogModifications.Count = 0 Then result.CatalogModifications = Nothing
        If result.LegacyImportNotes.Count = 0 Then result.LegacyImportNotes = Nothing
        Return result
    End Function

    Private Function CopyDesign(design As StockDesign) As StockDesign
        Return JsonSerializer.Deserialize(Of StockDesign)(JsonSerializer.Serialize(design))
    End Function

    Private Function GroupFor(item As Entry) As String
        Dim id = item.Mapping.Id
        If item.Mapping.Kind = "Upgrade" Then Return "upgrade-" & id
        If item.Mapping.Kind.StartsWith("Turret", StringComparison.Ordinal) OrElse item.Mapping.Kind = "Smartlink" OrElse item.Mapping.Kind = "Recoil" OrElse
           id.Contains("mount") OrElse id = "roll-bar" OrElse id = "weapon-gyro-stabilization" Then Return "weapons"
        If id.StartsWith("drive-by-wire", StringComparison.Ordinal) OrElse id = "structural-agility" Then Return "drive-by-wire"
        If id.StartsWith("engine-customization", StringComparison.Ordinal) OrElse id = "active-thermal-masking" OrElse item.Mapping.Kind = "EngineBudget" Then Return "engine"
        If id = "rigger-adaptation" OrElse id = "datajack-port" Then Return "rigger-interface"
        If id.Contains("seat") Then Return "seating"
        If id.Contains("seal") OrElse id = "cabin-overpressure" OrElse id = "life-support" Then Return "seals"
        If id = "sensors" OrElse id = "remote-control-interface" OrElse id = "advanced-drone-pilot" OrElse id = "autonav" OrElse id = "motorbike-gyro" Then Return "remote-controls"
        If item.Mapping.Kind = "Arm" Then Return "arms"
        If item.Mapping.Kind = "SpareTires" OrElse item.Mapping.Kind = "StandardTires" OrElse id.EndsWith("-tires", StringComparison.Ordinal) OrElse id = "runflat-upgrade" Then Return "tires"
        Return If(id = "", Key(item.Name), id)
    End Function

    Private Sub AddSelection(design As StockDesign, id As String, quantity As Integer)
        If quantity = 0 Then Return
        Dim existing = design.CatalogModifications.SingleOrDefault(Function(item) item.Id = id)
        If existing Is Nothing Then
            design.CatalogModifications.Add(New ModificationSelection With {.Id = id, .Quantity = quantity})
        Else
            existing.Quantity = CInt(CLng(existing.Quantity) + quantity)
        End If
    End Sub

    Private Sub ConvertEntry(design As StockDesign, item As Entry, all As List(Of Entry), reference As JsonElement, chassis As StockChassis)
        Dim mapping = item.Mapping
        Dim quantity = If(mapping.Quantity, If(mapping.Flag, 1, item.Level))
        Select Case mapping.Kind
            Case "Upgrade"
                If design.Upgrades Is Nothing Then design.Upgrades = New DesignUpgrades()
                Dim propertyName = mapping.Id
                If propertyName = "OnRoadHandling" AndAlso chassis.Handling.HasValue Then propertyName = "Handling"
                If propertyName = "CargoSteps" Then quantity = CInt(CLng(quantity) * 5)
                Dim field = GetType(DesignUpgrades).GetProperty(propertyName)
                field.SetValue(design.Upgrades, CInt(CLng(CInt(field.GetValue(design.Upgrades))) + quantity))
                Return
            Case "SensorIncrement"
                Dim oldName = reference.GetProperty("Chassis").GetProperty("Name").GetString()
                Dim oldBook = Key(reference.GetProperty("Chassis").GetProperty("Book").GetString())
                Dim starting As Integer
                If (oldBook <> "rigger2" AndAlso oldBook <> "r2clean") OrElse Not factorySensors.TryGetValue(oldName, starting) Then
                    Throw New ArgumentException("The old chassis's factory Sensor rating is unknown; the saved level is an increase, not a final rating.")
                End If
                quantity = CInt(CLng(starting) + item.Level)
                If quantity <= chassis.Sensor.GetValueOrDefault() Then Return
            Case "CapacityIndex"
                Dim capacities = {5, 25, 200, 750, 2000, 5000, 20000, 30000, 45000, 60000, 75000}
                If item.Level >= capacities.Length Then Throw New ArgumentException("Unknown old crane/winch capacity index.")
                quantity = capacities(item.Level)
            Case "Smartlink"
                If item.Level < 1 OrElse item.Level > 2 Then Throw New ArgumentException("Smartlink grade must be 1 or 2.")
                AddSelection(design, "smartlink-integration-" & item.Level, 1)
                Return
            Case "EngineBudget"
                Dim allocated = all.Where(Function(entry) entry.Mapping.Id.StartsWith("engine-customization-", StringComparison.Ordinal)).Sum(Function(entry) CLng(entry.Level))
                If allocated <> item.Level Then Throw New ArgumentException("The old engine customization total does not match its saved Speed, Acceleration and Load allocation.")
                Return
            Case "Turret"
                Dim size = mapping.Id.Split("-"c)(1)
                Dim variants = all.Where(Function(entry) entry.Mapping.Kind = "TurretVariant" AndAlso entry.Mapping.Id = size).ToArray()
                If variants.Length > 1 Then Throw New ArgumentException("Combined pop-up and anti-aircraft turret variants need a combined revised specification.")
                AddSelection(design, mapping.Id & If(variants.Length = 1, "-" & variants(0).Mapping.TurretStyle, ""), quantity)
                Return
            Case "TurretVariant"
                If Not all.Any(Function(entry) entry.Mapping.Kind = "Turret" AndAlso entry.Mapping.Id.Contains("-" & mapping.Id & "-")) Then Throw New ArgumentException("A turret variant has no saved base turret.")
                Return
            Case "Arm"
                Dim body = chassis.Body.GetValueOrDefault()
                If item.Level > ModificationCatalog.MaximumQuantity(ModificationCatalog.Rules.Single(Function(rule) rule.Id = "mechanical-arm-strength"), chassis) Then Throw New ArgumentException("The old arm enhancement exceeds the revised Strength limit.")
                AddSelection(design, "mechanical-arm", 1)
                If item.Level > 0 Then design.Additions.Add(New DesignAddition With {
                    .Name = item.Name & $" — revised Strength {body * body + item.Level}",
                    .DesignPoints = If(item.Level <= 3, 60D, 75D) * body * item.Level, .LoadConsumedKg = 10D * item.Level})
                Return
            Case "PowerAmplifier"
                If quantity < 1 OrElse quantity > 10 Then Throw New ArgumentException("Power amplifier Flux must be between 1 and 10.")
                design.Additions.Add(New DesignAddition With {
                    .Name = $"Power amplifier — Flux +{quantity} (source transmitter unspecified)", .DesignPoints = 5D * quantity,
                    .CargoConsumed = Math.Floor(quantity * 0.25D), .LoadConsumedKg = quantity})
                Return
            Case "Description"
                design.Additions.Add(New DesignAddition With {.Name = "Convertible top (appearance; no added ratings)"})
                Return
            Case "StandardTires"
                design.Additions.Add(New DesignAddition With {.Name = "Standard tires (revised purchase price)", .DirectCostNuyen = chassis.Body.GetValueOrDefault() * 50D * quantity})
                Return
            Case "SpareTires"
                Dim types = all.Where(Function(entry) {"performance-tires", "off-road-tires", "dual-purpose-tires"}.Contains(entry.Mapping.Id)).ToArray()
                If types.Length > 1 Then Throw New ArgumentException("Multiple saved tire types cannot determine the spare tire type.")
                AddSelection(design, "spare-" & If(types.Length = 1, types(0).Mapping.Id, "standard-tires"), quantity)
                Return
            Case "Recoil"
                Dim target = mapping.Id
                Dim targets = all.Where(Function(entry) (target = "fixed firm" AndAlso entry.Mapping.Id.EndsWith("fixed-firmpoint", StringComparison.Ordinal)) OrElse
                    (target = "fixed hard" AndAlso entry.Mapping.Id.EndsWith("fixed-hardpoint", StringComparison.Ordinal)) OrElse
                    (target.EndsWith(" turret", StringComparison.Ordinal) AndAlso entry.Mapping.Kind = "Turret" AndAlso entry.Mapping.Id.Contains("-" & target.Split(" "c)(0) & "-"))).ToArray()
                Dim maximum = If(target = "fixed firm" OrElse target = "micro turret", 4, If(target = "mini turret", 6, If(target = "fixed hard" OrElse target = "small turret", 9, 12)))
                If quantity > maximum OrElse targets.Length = 0 Then Throw New ArgumentException("Recoil adjusters have no matching mount or exceed its revised limit.")
                If all.Any(Function(entry) entry.Mapping.Id = "weapon-gyro-stabilization") Then Throw New ArgumentException("Recoil adjusters cannot be combined with weapon gyro-stabilization.")
                Dim count = targets.Sum(Function(entry) CLng(entry.Level))
                design.Additions.Add(New DesignAddition With {.Name = item.Name & $" — revised rating {quantity} on {count} mount(s)", .DesignPoints = 10D * quantity * count})
                Return
        End Select
        If mapping.Id = "life-support" Then quantity = CInt(Math.Ceiling(item.Level / 10D))
        If mapping.Id = "improved-anti-theft-security" AndAlso quantity <= 2 Then Return 'Factory rating 2 is standard under the revised rules.
        If mapping.Id = "autonav" AndAlso quantity <= chassis.Autonav.GetValueOrDefault() Then Return
        If mapping.Id = "advanced-drone-pilot" AndAlso quantity <= Math.Max(1, chassis.Pilot.GetValueOrDefault()) Then Return
        If mapping.Id = "datajack-port" AndAlso all.Any(Function(entry) entry.Mapping.Id = "rigger-adaptation") Then Return
        If mapping.Id.StartsWith("living-amenities-", StringComparison.Ordinal) Then AddSelection(design, "living-amenities-people-space", quantity)
        AddSelection(design, mapping.Id, quantity)
    End Sub
End Module
