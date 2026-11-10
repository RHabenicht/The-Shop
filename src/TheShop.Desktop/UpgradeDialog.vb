Imports System.Drawing
Imports System.Windows.Forms
Imports TheShop.Design

Friend NotInheritable Class UpgradeDialog
    Inherits Form

    Public ReadOnly Property Selection As New DesignUpgrades

    Public Sub New(current As DesignUpgrades, chassis As StockChassis, plant As StockPowerPlant)
        Text = "Design upgrades"
        StartPosition = FormStartPosition.CenterParent
        AutoScaleMode = AutoScaleMode.Dpi
        AutoSize = True
        AutoSizeMode = AutoSizeMode.GrowAndShrink
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False

        Dim table As New TableLayoutPanel With {.AutoSize = True, .Dock = DockStyle.Fill, .ColumnCount = 2, .Padding = New Padding(12)}
        Controls.Add(table)
        current = If(current, New DesignUpgrades())
        AddSteps(table, "Speed +1 (2 DP)", current.Speed, Sub(n) Selection.Speed = n,
                 plant.StartingSpeed.HasValue AndAlso Not plant.StartingSecondarySpeed.HasValue)
        AddSteps(table, "Acceleration +1 (25 DP)", current.Acceleration, Sub(n) Selection.Acceleration = n, plant.StartingAcceleration.HasValue)
        AddSteps(table, "Handling −1 (25 DP)", current.Handling, Sub(n) Selection.Handling = n, chassis.Handling.HasValue)
        AddSteps(table, "On-road Handling −1 (25 DP)", current.OnRoadHandling, Sub(n) Selection.OnRoadHandling = n, chassis.OnRoadHandling.HasValue)
        AddSteps(table, "Off-road Handling −1 (25 DP)", current.OffRoadHandling, Sub(n) Selection.OffRoadHandling = n, chassis.OffRoadHandling.HasValue)
        AddSteps(table, "Cargo +0.2 CF (1 DP)", current.CargoSteps, Sub(n) Selection.CargoSteps = n, chassis.MaximumCargo > chassis.StartingCargo)
        Dim kg = If(chassis.Body.GetValueOrDefault() = 0, 1, 10)
        AddSteps(table, $"Load +{kg} kg (1 DP)", current.LoadSteps, Sub(n) Selection.LoadSteps = n, Not chassis.Hull.HasValue AndAlso plant.MaximumLoadKg > plant.StartingLoadKg)
        AddSteps(table, "Signature +1 (200 × levels⁴ DP, GM limit)", current.Signature, Sub(n) Selection.Signature = n, plant.Signature.HasValue)

        AddSteps(table, "Economy +5% of starting rating (5 DP)", current.EconomySteps, Sub(n) Selection.EconomySteps = n,
                 plant.EconomyUnit IsNot Nothing)

        Dim fuel = FuelExpansionRule.ForPlant(chassis, plant)
        Dim fuelLabel = If(fuel Is Nothing, "Fuel capacity", $"Fuel +{fuel?.Amount} {fuel?.Unit} ({fuel?.Points} DP)")
        AddSteps(table, fuelLabel, current.FuelSteps, Sub(n) Selection.FuelSteps = n, fuel IsNot Nothing)
        AddSteps(table, "Depth +10 m (2 DP plus load)", current.DepthSteps, Sub(n) Selection.DepthSteps = n,
                 chassis.StartingDepth.HasValue)

        Dim buttons As New FlowLayoutPanel With {.AutoSize = True, .Dock = DockStyle.Fill}
        Dim apply As New Button With {.Text = "Apply", .DialogResult = DialogResult.OK}
        Dim cancel As New Button With {.Text = "Cancel", .DialogResult = DialogResult.Cancel}
        buttons.Controls.Add(apply)
        buttons.Controls.Add(cancel)
        table.Controls.Add(buttons, 0, table.RowCount)
        table.SetColumnSpan(buttons, 2)
        AcceptButton = apply
        CancelButton = cancel
    End Sub

    Private Shared Sub AddSteps(table As TableLayoutPanel, caption As String, initial As Integer, update As Action(Of Integer), enabled As Boolean)
        Dim row = table.RowCount
        table.RowCount += 1
        Dim input As New NumericUpDown With {.Maximum = If(enabled, Integer.MaxValue, initial), .Value = initial, .Enabled = enabled OrElse initial > 0}
        update(initial)
        AddHandler input.ValueChanged, Sub(sender, e) update(CInt(input.Value))
        table.Controls.Add(New Label With {.Text = caption, .AutoSize = True, .Anchor = AnchorStyles.Left}, 0, row)
        table.Controls.Add(input, 1, row)
    End Sub
End Class
