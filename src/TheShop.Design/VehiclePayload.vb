Imports System.Text.Json.Serialization

Public NotInheritable Class VehiclePayloadItem
    Public Property Name As String
    Public Property Quantity As Integer = 1
    Public Property UnitWeightKg As Decimal
    Public Property UnitCostNuyen As Decimal
    Public Property UnitCargoCf As Decimal
    Public Property WeaponClass As String
    Public Property MountId As String
    Public Property MountNumber As Integer = 1
    Public Property Direction As String = "Forward"
End Class

Public NotInheritable Class EquipmentSetting
    Public Property Component As String
    Public Property Instance As Integer = 1
    Public Property Rating As Integer
    Public Property Target As String
    Public Property Retrofit As Boolean
End Class

Public Module VehiclePayload
    Private ReadOnly weaponValues As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase) From {
        {"Pistol", 1D}, {"Shotgun", 1D}, {"Missile", 0D}, {"Heavy Missile", 0D}, {"Torpedo", 0D}, {"Mine", 0D}, {"SMG", 1D}, {"Microwave Designator", 1D}, {"Rifle", 1D}, {"Grenade Launcher", 1D}, {"LMG", 1.5D},
        {"MMG", 2D}, {"HMG", 2D}, {"Missile Launcher", 2D}, {"Rocket Launcher", 2D}, {"Harpoon Gun", 2D}, {"Vindicator", 2D},
        {"Vanquisher", 2.5D}, {"Vengeance", 2.5D}, {"Assault Cannon", 3D}, {"Autocannon", 3D}, {"Vehicle Laser", 3D},
        {"Water Cannon", 3D}, {"Mortar", 4D}, {"Light Railgun", 5D}, {"Outlaw Missile System", 5D}, {"RASCAM", 5D},
        {"Silencer", 6D}, {"Silencer AARM", 6D}, {"ANDREWS", 6D}, {"Light Naval Gun", 8D}, {"Medium Railgun", 8D}, {"Javelot", 9D}, {"Sirocco", 9D}, {"ASROC", 10D}, {"Medium Naval Gun", 10D}, {"Heavy Railgun", 10D}, {"Sea Saber", 10D}
    }
    Public ReadOnly Property WeaponClasses As IReadOnlyList(Of String)
        Get
            Return weaponValues.Keys.ToArray()
        End Get
    End Property

    Public Function Additions(design As StockDesign, chassis As StockChassis, ByRef configuredListCost As Decimal, ByRef configuredStreetCost As Decimal) As IReadOnlyList(Of DesignAddition)
        Dim result As New List(Of DesignAddition)
        Dim manufactured = StockCatalog.FactoryMountSelections(chassis).Concat(RetrofitCatalog.NormalizeSelections(design.CatalogModifications)).ToArray()
        Dim installed = RetrofitCatalog.NormalizeSelections(design.InstalledRetrofits)
        Dim mounts = manufactured.Concat(installed).ToArray()
        Dim usedStorage As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        Dim used As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        If design.Payload IsNot Nothing Then
            For Each item In design.Payload
                If item Is Nothing OrElse String.IsNullOrWhiteSpace(item.Name) OrElse item.Quantity < 1 OrElse item.UnitWeightKg < 0D OrElse item.UnitCostNuyen < 0D OrElse item.UnitCargoCf < 0D Then Throw New ArgumentException("Payload requires a name, positive count and nonnegative weight, price and cargo.")
                Dim storedInTurret = False
                Dim mountId = If(item.MountId, "").ToLowerInvariant()
                If Not String.IsNullOrWhiteSpace(item.MountId) Then
                    Dim mountCount = mounts.Where(Function(choice) String.Equals(choice.Id, item.MountId, StringComparison.OrdinalIgnoreCase)).Sum(Function(choice) CLng(choice.Quantity))
                    If item.MountNumber < 1 OrElse item.MountNumber > mountCount Then Throw New ArgumentException("The selected mount instance does not exist.")
                    If mountId.Contains("turret") AndAlso item.UnitCargoCf > 0D Then
                        Dim key = mountId & "#" & item.MountNumber
                        usedStorage(key) = If(usedStorage.ContainsKey(key), usedStorage(key), 0D) + item.UnitCargoCf * item.Quantity
                        If usedStorage(key) > TurretStorage(mountId) Then Throw New ArgumentException("Payload exceeds the turret's internal CF.")
                        storedInTurret = True
                    End If
                End If
                If Not String.IsNullOrWhiteSpace(item.WeaponClass) Then
                    Dim value As Decimal
                    If Not weaponValues.TryGetValue(item.WeaponClass, value) Then Throw New ArgumentException($"Unknown weapon class '{item.WeaponClass}'.")
                    If item.UnitWeightKg <= 0D Then Throw New ArgumentException("Enter the carried weapon's actual weight.")
                    Dim mount = mounts.Where(Function(choice) String.Equals(choice.Id, item.MountId, StringComparison.OrdinalIgnoreCase)).ToArray()
                    Dim count = mount.Sum(Function(choice) CLng(choice.Quantity))
                    If item.MountNumber < 1 OrElse item.MountNumber > count Then Throw New ArgumentException($"Payload '{item.Name}' has no matching mount instance.")
                    If (mountId.StartsWith("torpedo-", StringComparison.Ordinal) OrElse mountId = "factory-torpedo-tube") AndAlso Not {"Forward", "Rear"}.Contains(item.Direction, StringComparer.OrdinalIgnoreCase) Then Throw New ArgumentException("Torpedo installations must face Forward or Rear.")
                    If chassis.Category = "Bike" AndAlso Not String.Equals(item.Direction, "Forward", StringComparison.OrdinalIgnoreCase) Then Throw New ArgumentException("A motorcycle sidecar weapon must fire forwards.")
                    Dim ordnance = value = 0D
                    Dim capacity As Decimal
                    Dim key = mountId & "#" & item.MountNumber
                    If ordnance Then
                        ValidateOrdnanceMount(mountId, item.WeaponClass, chassis)
                        If mountId.StartsWith("external-missile-mounts", StringComparison.Ordinal) AndAlso (chassis.Category.Contains("Aircraft") OrElse chassis.Category = "Rotor Craft") Then
                            capacity = If(mountId.EndsWith("reinforced", StringComparison.Ordinal), 1500D, 300D)
                            value = item.UnitWeightKg
                        Else
                            capacity = 1D
                            value = 1D
                            ' Storage quantities reserve consecutive numbered slots, never the same slot twice.
                            If mountId = "internal-missile-storage" OrElse mountId = "internal-heavy-missile-storage" OrElse mountId = "torpedo-storage-rack" Then
                                If CLng(item.MountNumber) + item.Quantity - 1L > count Then Throw New ArgumentException("Ordnance exceeds the allocated storage slots.")
                                For slot As Integer = item.MountNumber To item.MountNumber + item.Quantity - 1
                                    Dim slotKey = mountId & "#" & slot
                                    If used.ContainsKey(slotKey) Then Throw New ArgumentException("An ordnance storage slot is already occupied.")
                                    used(slotKey) = 1D
                                Next
                                capacity = item.Quantity
                                value = 0D
                            End If
                        End If
                    Else
                        capacity = WeaponCapacity(mountId)
                        If (mountId.StartsWith("ring-mount", StringComparison.Ordinal) OrElse mountId = "pintle-mount") AndAlso value > 3D Then Throw New ArgumentException("A manual pintle or ring mount requires a man-portable firearm.")
                        If mountId.StartsWith("ring-mount", StringComparison.Ordinal) AndAlso {"Autocannon", "Vehicle Laser", "Water Cannon"}.Contains(item.WeaponClass, StringComparer.OrdinalIgnoreCase) Then Throw New ArgumentException("A ring mount requires a man-portable firearm.")
                        If Not mountId.Contains("turret") Then
                            If used.ContainsKey(key) OrElse item.Quantity <> 1 Then Throw New ArgumentException("A fixed, pintle or ring mount holds one weapon.")
                        End If
                    End If
                    used(key) = If(used.ContainsKey(key), used(key), 0D) + value * item.Quantity
                    If used(key) > capacity Then Throw New ArgumentException($"Weapons exceed the capacity of {key}.")
                    If Not {"Forward", "Rear", "Left", "Right"}.Contains(item.Direction, StringComparer.OrdinalIgnoreCase) Then Throw New ArgumentException("Choose Forward, Rear, Left or Right for weapon direction.")
                ElseIf Not String.IsNullOrWhiteSpace(item.MountId) AndAlso Not storedInTurret Then
                    Throw New ArgumentException("Mounted equipment requires a weapon class or allocated turret storage.")
                End If
                result.Add(New DesignAddition With {.Name = item.Name & $" ×{item.Quantity}", .DirectCostNuyen = item.UnitCostNuyen * item.Quantity,
                    .LoadConsumedKg = item.UnitWeightKg * item.Quantity, .CargoConsumed = If(storedInTurret, 0D, item.UnitCargoCf * item.Quantity)})
            Next
        End If
        Dim configured As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        If design.EquipmentSettings IsNot Nothing Then
            For Each setting In design.EquipmentSettings
                If setting Is Nothing OrElse setting.Rating < 1 OrElse setting.Instance < 1 Then Throw New ArgumentException("Equipment settings require a positive instance and rating.")
                Dim id = setting.Component
                Dim target = If(id = "Arm strength", "mechanical-arm", If(id = "Recoil", If(setting.Target, "").ToLowerInvariant(), "power-amplifier"))
                If id <> "Arm strength" AndAlso id <> "Recoil" AndAlso id <> "Amplifier" Then Throw New ArgumentException("Choose Arm strength, Recoil or Amplifier.")
                If Not configured.Add(id & ":" & target & ":" & setting.Instance) Then Throw New ArgumentException("An equipment instance has repeated settings.")
                If id = "Arm strength" OrElse id = "Recoil" Then
                    Dim count = mounts.Where(Function(choice) choice.Id = target).Sum(Function(choice) CLng(choice.Quantity))
                    If setting.Instance > count Then Throw New ArgumentException("The configured arm or mount instance does not exist.")
                End If
                Dim factoryCount = manufactured.Where(Function(choice) choice.Id = target).Sum(Function(choice) CLng(choice.Quantity))
                Dim retrofit = setting.Retrofit OrElse ((id = "Arm strength" OrElse id = "Recoil") AndAlso setting.Instance > factoryCount)
                Dim body = VehicleScale.RuleBody(chassis)
                Dim addition As New DesignAddition With {.Name = $"{id} — {target} #{setting.Instance}, rating {setting.Rating}"}
                Select Case id
                    Case "Arm strength"
                        If DesignEnhancements.Has(mounts, "mechanical-arm-strength") Then Throw New ArgumentException("Use either uniform arm enhancement or individual arm settings.")
                        Dim factoryArms = If(chassis.FactoryEquipment, New List(Of ModificationSelection)).Where(Function(item) item.Id = "mechanical-arm").Sum(Function(item) item.Quantity)
                        Dim initialStrength = If(setting.Instance <= factoryArms, chassis.FactoryArmStrength.GetValueOrDefault(CInt(body * body)), body * body)
                        If initialStrength + setting.Rating > body * 10D Then Throw New ArgumentException("Arm Strength exceeds Body × 10.")
                        If retrofit Then
                            addition.DirectCostNuyen = If(setting.Rating <= 3, 5000D, 6000D) * body * setting.Rating
                        Else
                            addition.DesignPoints = If(setting.Rating <= 3, 60D, 75D) * body * setting.Rating
                        End If
                        addition.LoadConsumedKg = 10D * setting.Rating
                    Case "Recoil"
                        If DesignEnhancements.Has(installed, "gunnery-recoil-adjusters") OrElse
                            (DesignEnhancements.Has(manufactured, "gunnery-recoil-adjusters") AndAlso setting.Instance <= factoryCount) OrElse
                            DesignEnhancements.Has(mounts, "weapon-gyro-stabilization") Then Throw New ArgumentException("This mount already has uniform recoil adjustment or incompatible gyro-stabilization.")
                        Dim capacity = RecoilCapacity(target)
                        If setting.Rating > capacity Then Throw New ArgumentException("Recoil setting exceeds the mount's limit.")
                        If retrofit Then
                            addition.DirectCostNuyen = setting.Rating * 500D
                            addition.CargoConsumed = 1D
                            addition.LoadConsumedKg = setting.Rating + 24D
                        Else
                            addition.DesignPoints = setting.Rating * 10D
                        End If
                    Case "Amplifier"
                        If setting.Rating > 10 Then Throw New ArgumentException("Amplifier Flux cannot exceed 10.")
                        If String.IsNullOrWhiteSpace(setting.Target) Then Throw New ArgumentException("Identify the device served by the amplifier.")
                        addition.Name = $"Amplifier — {setting.Target} #{setting.Instance}, Flux +{setting.Rating}"
                        If retrofit Then
                            addition.DirectCostNuyen = 250D * setting.Rating
                        Else
                            addition.DesignPoints = 5D * setting.Rating
                        End If
                        addition.CargoConsumed = If(retrofit, Math.Ceiling(setting.Rating * 0.5D), Math.Floor(setting.Rating * 0.25D))
                        addition.LoadConsumedKg = setting.Rating
                End Select
                If retrofit AndAlso addition.DirectCostNuyen > 0D Then
                    configuredListCost += addition.DirectCostNuyen
                    configuredStreetCost += addition.DirectCostNuyen * If(id = "Recoil" OrElse id = "Amplifier", 1.5D, 1D)
                    addition.DirectCostNuyen = 0D
                End If
                result.Add(addition)
            Next
        End If
        Return result
    End Function

    Public Function WeaponCapacity(id As String) As Decimal
        If String.IsNullOrEmpty(id) Then Throw New ArgumentException("Select a weapon mount.")
        id = id.ToLowerInvariant()
        If id.Contains("turret") Then
            Dim capacity As Decimal = If(id.Contains("micro"), 1D, If(id.Contains("mini"), 2D, If(id.Contains("small"), 3D, If(id.Contains("medium"), 6D, If(id.Contains("extra-large"), 10D, 8D)))))
            If id.EndsWith("pop-up", StringComparison.Ordinal) AndAlso Not id.Contains("micro") Then capacity -= 1D
            Return capacity
        End If
        If id.Contains("firmpoint") OrElse id = "pintle-mount" Then Return 1.5D
        If id.Contains("hardpoint") Then Return 10D
        If id.StartsWith("ring-mount", StringComparison.Ordinal) Then Return 3D
        Throw New ArgumentException("The selected component is not a gun mount.")
    End Function

    Public Function TurretStorage(id As String) As Decimal
        If id.Contains("micro") Then Return 0D
        Return If(id.Contains("mini"), 1D, If(id.Contains("small"), 2D, If(id.Contains("medium"), 4D, If(id.Contains("extra-large"), 16D, 8D))))
    End Function
    Public Function TurretSeating(id As String) As Integer
        Return If(id.Contains("micro") OrElse id.Contains("mini") OrElse id.Contains("small"), 0, If(id.Contains("extra-large"), 3, 2))
    End Function

    Public Function RecoilCapacity(id As String) As Integer
        id = If(id, "").ToLowerInvariant()
        Dim capacity = WeaponCapacity(id)
        If id.Contains("firmpoint") OrElse id.Contains("micro") Then Return 4
        If id.Contains("mini") Then Return 6
        If id.Contains("hardpoint") OrElse id.Contains("small") Then Return 9
        If id.Contains("turret") Then Return 12
        Throw New ArgumentException("Recoil adjusters require a fixed mount or turret.")
    End Function

    Private Sub ValidateOrdnanceMount(id As String, weaponClass As String, chassis As StockChassis)
        Dim kind = weaponClass.ToLowerInvariant()
        Select Case id
            Case "internal-missile-storage"
                If kind <> "missile" Then Throw New ArgumentException("Standard internal storage accepts normal missiles and rockets.")
            Case "internal-heavy-missile-storage"
                If kind <> "heavy missile" AndAlso Not (kind = "torpedo" AndAlso chassis.Category = "Ship") Then Throw New ArgumentException("Heavy internal storage accepts heavy missiles or surface-ship torpedoes.")
            Case "torpedo-storage-rack"
                If kind <> "torpedo" Then Throw New ArgumentException("A torpedo storage rack accepts torpedoes.")
            Case "torpedo-tube-manual", "torpedo-tube-autoloader", "factory-torpedo-tube"
                ' The GM identifies the specific tube-launched missile, mine or torpedo.
            Case "external-missile-mounts", "external-missile-mounts-reinforced"
                If kind = "mine" Then Throw New ArgumentException("Sea mines require an appropriate torpedo installation.")
                If kind = "torpedo" AndAlso chassis.Category <> "Ship" AndAlso id <> "external-missile-mounts-reinforced" Then Throw New ArgumentException("Torpedoes require a ship launch mount or a reinforced aircraft mount.")
            Case Else
                Throw New ArgumentException("Ordnance requires an appropriate launch mount, tube or storage rack.")
        End Select
    End Sub

    Public Function Arc(id As String) As String
        id = If(id, "").ToLowerInvariant()
        If id.StartsWith("torpedo-", StringComparison.Ordinal) OrElse id = "factory-torpedo-tube" Then Return "Fore or aft tube/storage group; separate torpedo stores"
        If id.Contains("missile") Then Return "Ordnance launch mount"
        If id.Contains("turret") Then Return If(id.Contains("anti-aircraft"), "360°; −10° to +75° elevation", "360°; −10° to +45° elevation")
        If id.StartsWith("ring-mount", StringComparison.Ordinal) Then Return "360°; ±30° elevation; manual gunner"
        If id = "pintle-mount" Then Return "±60° horizontal; ±30° elevation; manual gunner"
        Return "±5° horizontal and vertical; aimed by vehicle movement"
    End Function
End Module
