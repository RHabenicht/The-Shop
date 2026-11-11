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
End Class

Public Module VehiclePayload
    Private ReadOnly weaponValues As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase) From {
        {"SMG", 1D}, {"Microwave Designator", 1D}, {"Rifle", 1D}, {"Grenade Launcher", 1D}, {"LMG", 1.5D},
        {"MMG", 2D}, {"HMG", 2D}, {"Missile Launcher", 2D}, {"Rocket Launcher", 2D}, {"Harpoon Gun", 2D}, {"Vindicator", 2D},
        {"Vanquisher", 2.5D}, {"Vengeance", 2.5D}, {"Assault Cannon", 3D}, {"Autocannon", 3D}, {"Vehicle Laser", 3D},
        {"Bolt", 5D}, {"Firelance", 5D}, {"Heavy Railgun", 5D}, {"Talon", 5D}, {"Arbalest", 5D}, {"RASCAM", 5D},
        {"Silencer", 6D}, {"ANDREWS", 6D}, {"Light Naval Gun", 8D}, {"Medium Railgun", 8D}, {"Javelot", 9D}, {"Sirocco", 9D}, {"ASROC", 10D}, {"Medium Naval Gun", 10D}
    }
    Public ReadOnly Property WeaponClasses As IReadOnlyList(Of String)
        Get
            Return weaponValues.Keys.ToArray()
        End Get
    End Property

    Public Function Additions(design As StockDesign, chassis As StockChassis, ByRef configuredListCost As Decimal, ByRef configuredStreetCost As Decimal) As IReadOnlyList(Of DesignAddition)
        Dim result As New List(Of DesignAddition)
        Dim mounts = If(design.CatalogModifications, New List(Of ModificationSelection)).Concat(If(design.InstalledRetrofits, New List(Of ModificationSelection))).ToArray()
        Dim used As New Dictionary(Of String, Decimal)(StringComparer.OrdinalIgnoreCase)
        If design.Payload IsNot Nothing Then
            For Each item In design.Payload
                If item Is Nothing OrElse String.IsNullOrWhiteSpace(item.Name) OrElse item.Quantity < 1 OrElse item.UnitWeightKg < 0D OrElse item.UnitCostNuyen < 0D OrElse item.UnitCargoCf < 0D Then Throw New ArgumentException("Payload requires a name, positive count and nonnegative weight, price and cargo.")
                If Not String.IsNullOrWhiteSpace(item.WeaponClass) Then
                    Dim value As Decimal
                    If Not weaponValues.TryGetValue(item.WeaponClass, value) Then Throw New ArgumentException($"Unknown weapon class '{item.WeaponClass}'.")
                    If item.UnitWeightKg <= 0D Then Throw New ArgumentException("Enter the carried weapon's actual weight.")
                    Dim mount = mounts.Where(Function(choice) String.Equals(choice.Id, item.MountId, StringComparison.OrdinalIgnoreCase)).ToArray()
                    Dim count = mount.Sum(Function(choice) CLng(choice.Quantity))
                    If item.MountNumber < 1 OrElse item.MountNumber > count Then Throw New ArgumentException($"Payload '{item.Name}' has no matching mount instance.")
                    Dim capacity = WeaponCapacity(item.MountId)
                    Dim key = item.MountId & "#" & item.MountNumber
                    used(key) = If(used.ContainsKey(key), used(key), 0D) + value * item.Quantity
                    If used(key) > capacity Then Throw New ArgumentException($"Weapons exceed the capacity of {key}.")
                    If Not {"Forward", "Rear", "Left", "Right"}.Contains(item.Direction, StringComparer.OrdinalIgnoreCase) Then Throw New ArgumentException("Choose Forward, Rear, Left or Right for weapon direction.")
                ElseIf Not String.IsNullOrWhiteSpace(item.MountId) Then
                    Throw New ArgumentException("Mounted weapons require a weapon class.")
                End If
                result.Add(New DesignAddition With {.Name = item.Name & $" ×{item.Quantity}", .DirectCostNuyen = item.UnitCostNuyen * item.Quantity,
                    .LoadConsumedKg = item.UnitWeightKg * item.Quantity, .CargoConsumed = item.UnitCargoCf * item.Quantity})
            Next
        End If
        Dim configured As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        If design.EquipmentSettings IsNot Nothing Then
            For Each setting In design.EquipmentSettings
                If setting Is Nothing OrElse setting.Rating < 1 OrElse setting.Instance < 1 Then Throw New ArgumentException("Equipment settings require a positive instance and rating.")
                Dim id = setting.Component
                Dim target = If(id = "Arm strength", "mechanical-arm", If(id = "Recoil", setting.Target, "power-amplifier"))
                If id <> "Arm strength" AndAlso id <> "Recoil" AndAlso id <> "Amplifier" Then Throw New ArgumentException("Choose Arm strength, Recoil or Amplifier.")
                If Not configured.Add(id & ":" & target & ":" & setting.Instance) Then Throw New ArgumentException("An equipment instance has repeated settings.")
                If id = "Arm strength" OrElse id = "Recoil" Then
                    Dim count = mounts.Where(Function(choice) choice.Id = target).Sum(Function(choice) CLng(choice.Quantity))
                    If setting.Instance > count Then Throw New ArgumentException("The configured arm or mount instance does not exist.")
                End If
                Dim retrofit = design.InstalledRetrofits IsNot Nothing AndAlso design.InstalledRetrofits.Any(Function(choice) choice.Id = target)
                Dim body = VehicleScale.RuleBody(chassis)
                Dim addition As New DesignAddition With {.Name = $"{id} — {target} #{setting.Instance}, rating {setting.Rating}"}
                Select Case id
                    Case "Arm strength"
                        If DesignEnhancements.Has(mounts, "mechanical-arm-strength") Then Throw New ArgumentException("Use either uniform arm enhancement or individual arm settings.")
                        If body * body + setting.Rating > body * 10D Then Throw New ArgumentException("Arm Strength exceeds Body × 10.")
                        If retrofit Then
                            addition.DirectCostNuyen = If(setting.Rating <= 3, 5000D, 6000D) * body * setting.Rating
                        Else
                            addition.DesignPoints = If(setting.Rating <= 3, 60D, 75D) * body * setting.Rating
                        End If
                        addition.LoadConsumedKg = 10D * setting.Rating
                    Case "Recoil"
                        If DesignEnhancements.Has(mounts, "gunnery-recoil-adjusters") OrElse DesignEnhancements.Has(mounts, "weapon-gyro-stabilization") Then Throw New ArgumentException("Individual recoil settings cannot be combined with uniform recoil or gyro-stabilization.")
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
                        addition.DesignPoints = 5D * setting.Rating
                        addition.CargoConsumed = Math.Floor(setting.Rating * 0.25D)
                        addition.LoadConsumedKg = setting.Rating
                End Select
                If retrofit AndAlso addition.DirectCostNuyen > 0D Then
                    configuredListCost += addition.DirectCostNuyen
                    configuredStreetCost += addition.DirectCostNuyen * If(id = "Recoil", 1.5D, 1D)
                    addition.DirectCostNuyen = 0D
                End If
                result.Add(addition)
            Next
        End If
        Return result
    End Function

    Public Function WeaponCapacity(id As String) As Decimal
        If String.IsNullOrEmpty(id) Then Throw New ArgumentException("Select a weapon mount.")
        If id.Contains("turret") Then
            Dim capacity As Decimal = If(id.Contains("micro"), 1D, If(id.Contains("mini"), 2D, If(id.Contains("small"), 3D, If(id.Contains("medium"), 6D, If(id.Contains("extra-large"), 10D, 8D)))))
            If id.EndsWith("pop-up", StringComparison.Ordinal) AndAlso Not id.Contains("micro") Then capacity -= 1D
            Return capacity
        End If
        If id.Contains("firmpoint") OrElse id = "pintle-mount" Then Return 1.5D
        If id.Contains("hardpoint") OrElse id.StartsWith("ring-mount", StringComparison.Ordinal) Then Return 3D
        Throw New ArgumentException("The selected component is not a gun mount.")
    End Function

    Public Function RecoilCapacity(id As String) As Integer
        Dim capacity = WeaponCapacity(id)
        If id.Contains("firmpoint") OrElse id.Contains("micro") Then Return 4
        If id.Contains("mini") Then Return 6
        If id.Contains("hardpoint") OrElse id.Contains("small") Then Return 9
        If id.Contains("turret") Then Return 12
        Throw New ArgumentException("Recoil adjusters require a fixed mount or turret.")
    End Function

    Public Function Arc(id As String) As String
        If id.Contains("turret") Then Return If(id.Contains("anti-aircraft"), "360°; −10° to +75° elevation", "360°; −10° to +45° elevation")
        If id.StartsWith("ring-mount", StringComparison.Ordinal) Then Return "360°; ±30° elevation; manual gunner"
        If id = "pintle-mount" Then Return "±60° horizontal; ±30° elevation; manual gunner"
        Return "±5° horizontal and vertical; aimed by vehicle movement"
    End Function
End Module
