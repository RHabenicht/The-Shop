Imports System.Runtime.CompilerServices
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.FileIO
Imports TheShop.Design

<Assembly: InternalsVisibleTo("TheShop.Desktop.Smoke")>

Public NotInheritable Class DesignForm
    Inherits Form

    Private ReadOnly jsonOptions As New JsonSerializerOptions With {
        .WriteIndented = True,
        .UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    }
    Private ReadOnly catalog As IReadOnlyList(Of StockChassis) = StockCatalog.Read(jsonOptions)
    Private ReadOnly chassisBox As New ComboBox With {.Name = "chassisBox", .DropDownStyle = ComboBoxStyle.DropDownList, .Dock = DockStyle.Fill}
    Private ReadOnly powerPlantBox As New ComboBox With {.Name = "powerPlantBox", .DropDownStyle = ComboBoxStyle.DropDownList, .Dock = DockStyle.Fill}
    Private ReadOnly vehicleName As New TextBox With {.Dock = DockStyle.Fill}
    Private ReadOnly streetIndex As New TextBox With {.Name = "streetIndex", .Text = "1", .Dock = DockStyle.Fill}
    Private ReadOnly markup As New TextBox With {.Text = "0", .Dock = DockStyle.Fill}
    Private ReadOnly cargo As New TextBox With {.Dock = DockStyle.Fill}
    Private ReadOnly loadOverride As New TextBox With {.Dock = DockStyle.Fill}
    Private ReadOnly startingValues As New Label With {.AutoSize = True, .Dock = DockStyle.Fill, .Padding = New Padding(0, 4, 0, 4)}
    Private ReadOnly totals As New Label With {.Name = "totals", .AutoSize = True, .Dock = DockStyle.Fill, .Padding = New Padding(0, 4, 0, 4)}
    Private ReadOnly availabilityBox As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Dock = DockStyle.Fill}
    Private upgrades As DesignUpgrades
    Private catalogModifications As List(Of ModificationSelection)
    Private legacyReference As JsonElement?
    Private ReadOnly additions As New DataGridView With {
        .Name = "additions",
        .Dock = DockStyle.Fill,
        .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        .RowHeadersVisible = False,
        .AllowUserToResizeRows = False
    }

    Public Sub New()
        Text = "The Shop | Rigger 3 Revised"
        AutoScaleMode = AutoScaleMode.Dpi
        ClientSize = New Size(830, 500)
        MinimumSize = New Size(720, 440)
        StartPosition = FormStartPosition.CenterScreen

        Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 5, .Padding = New Padding(12)}
        layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Controls.Add(layout)

        Dim fields As New TableLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 4, .RowCount = 4}
        fields.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        fields.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        fields.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        fields.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50))
        AddField(fields, "Chassis", chassisBox, 0, 0)
        AddField(fields, "Power plant", powerPlantBox, 2, 0)
        AddField(fields, "Street Index", streetIndex, 0, 1)
        AddField(fields, "Markup adjustment", markup, 2, 1)
        AddField(fields, "Cargo override (CF)", cargo, 0, 2)
        AddField(fields, "Load override (kg)", loadOverride, 2, 2)
        availabilityBox.Items.Add("Unspecified")
        availabilityBox.Items.AddRange([Enum].GetValues(Of VehicleAvailabilityClass)().Cast(Of Object)().ToArray())
        availabilityBox.SelectedIndex = 0
        AddHandler availabilityBox.SelectedIndexChanged, AddressOf ClearTotals
        AddField(fields, "Availability class", availabilityBox, 0, 3)
        AddField(fields, "Vehicle name", vehicleName, 2, 3)
        layout.Controls.Add(fields, 0, 0)
        layout.Controls.Add(startingValues, 0, 1)

        additions.Columns.Add("Name", "Design option or modification")
        additions.Columns.Add("DesignPoints", "Design Points")
        additions.Columns.Add("DirectCostNuyen", "Direct cost (¥)")
        additions.Columns.Add("CargoConsumed", "Cargo used (CF)")
        additions.Columns.Add("LoadConsumedKg", "Load used (kg)")
        additions.Columns(0).FillWeight = 200
        layout.Controls.Add(additions, 0, 2)
        layout.Controls.Add(totals, 0, 3)

        Dim actions As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .AutoSize = True}
        AddAction(actions, "Calculate", AddressOf Calculate)
        AddAction(actions, "Design upgrades…", AddressOf EditUpgrades)
        AddAction(actions, "Catalog mods…", AddressOf EditModifications)
        AddAction(actions, "Open design…", AddressOf OpenDesign)
        AddAction(actions, "Save design…", AddressOf SaveDesign)
        AddAction(actions, "Export HTML…", AddressOf ExportHtml)
        AddAction(actions, "Import .shp…", AddressOf ImportLegacy)
        AddAction(actions, "Legacy reference…", AddressOf ViewLegacyReference)
        layout.Controls.Add(actions, 0, 4)

        chassisBox.DisplayMember = NameOf(StockChassis.Name)
        powerPlantBox.DisplayMember = NameOf(StockPowerPlant.Name)
        AddHandler chassisBox.SelectedIndexChanged, AddressOf ChassisChanged
        AddHandler powerPlantBox.SelectedIndexChanged, AddressOf PowerPlantChanged
        For Each field As TextBox In {streetIndex, markup, cargo, loadOverride}
            AddHandler field.TextChanged, AddressOf ClearTotals
        Next
        AddHandler additions.CellValueChanged, AddressOf ClearTotals
        AddHandler additions.UserDeletedRow, AddressOf ClearTotals
        chassisBox.DataSource = catalog.ToArray()
    End Sub

    Private Shared Sub AddField(panel As TableLayoutPanel, caption As String, input As Control, column As Integer, row As Integer)
        panel.Controls.Add(New Label With {.Text = caption, .AutoSize = True, .Anchor = AnchorStyles.Left}, column, row)
        panel.Controls.Add(input, column + 1, row)
    End Sub

    Private Shared Sub AddAction(panel As FlowLayoutPanel, caption As String, handler As EventHandler)
        Dim button As New Button With {.Text = caption, .AutoSize = True}
        AddHandler button.Click, handler
        panel.Controls.Add(button)
    End Sub

    Private Sub ChassisChanged(sender As Object, e As EventArgs)
        Dim chassis = TryCast(chassisBox.SelectedItem, StockChassis)
        If chassis IsNot Nothing Then
            powerPlantBox.DataSource = chassis.PowerPlants
            cargo.Clear()
            RefreshStartingValues(sender, e)
        End If
    End Sub

    Private Sub PowerPlantChanged(sender As Object, e As EventArgs)
        loadOverride.Clear()
        RefreshStartingValues(sender, e)
    End Sub

    Private Sub RefreshStartingValues(sender As Object, e As EventArgs)
        Dim chassis = TryCast(chassisBox.SelectedItem, StockChassis)
        Dim plant = TryCast(powerPlantBox.SelectedItem, StockPowerPlant)
        If chassis IsNot Nothing AndAlso plant IsNot Nothing Then
            Dim handling = If(chassis.Handling.HasValue, chassis.Handling.ToString(),
                              If(chassis.OnRoadHandling.HasValue, $"{chassis.OnRoadHandling}/{chassis.OffRoadHandling}", "—"))
            Dim speed = If(plant.StartingStallSpeed.HasValue, $"{plant.StartingStallSpeed}/{plant.StartingSpeed}", If(plant.StartingSpeed?.ToString(), "—"))
            If plant.StartingSecondarySpeed.HasValue Then
                speed = If(chassis.Category = "Ship", $"{plant.StartingSpeed} ({plant.StartingSecondarySpeed})",
                           $"{plant.StartingSpeed}/{plant.StartingSecondarySpeed}")
            End If
            Dim protection = If(chassis.Hull.HasValue, $"Hull {chassis.Hull}   Bulwark {chassis.Bulwark}",
                                $"Body {chassis.Body}   Armor {chassis.Armor}")
            startingValues.Text = $"Handling {handling}   {protection}   Speed {speed}   Accel {If(plant.StartingAcceleration?.ToString(), "—")}" &
                Environment.NewLine & $"Starting cargo {chassis.StartingCargo} CF   Starting load {plant.StartingLoadKg} kg   Chassis markup {chassis.Markup}   Blank overrides use starting values."
        End If
        ClearTotals(sender, e)
    End Sub

    Private Sub ClearTotals(sender As Object, e As EventArgs)
        totals.Text = "Calculate to update design totals."
    End Sub

    Friend Function ReadDesign() As StockDesign
        additions.EndEdit()
        Dim design As New StockDesign With {
            .ChassisId = DirectCast(chassisBox.SelectedItem, StockChassis).Id,
            .PowerPlantId = DirectCast(powerPlantBox.SelectedItem, StockPowerPlant).Id,
            .StreetIndex = ReadNumber(streetIndex.Text, "Street Index"),
            .Name = If(String.IsNullOrWhiteSpace(vehicleName.Text), Nothing, vehicleName.Text),
            .MarkupAdjustment = ReadNumber(markup.Text, "Markup adjustment"),
            .CargoCapacity = ReadOptional(cargo.Text, "Cargo override"),
            .LoadCapacityKg = ReadOptional(loadOverride.Text, "Load override"),
            .Upgrades = upgrades,
            .CatalogModifications = catalogModifications,
            .LegacyReference = legacyReference,
            .AvailabilityClass = If(TypeOf availabilityBox.SelectedItem Is VehicleAvailabilityClass,
                                    CType(availabilityBox.SelectedItem, VehicleAvailabilityClass), CType(Nothing, VehicleAvailabilityClass?))
        }
        For Each row As DataGridViewRow In additions.Rows
            If row.IsNewRow Then Continue For
            Dim values = row.Cells.Cast(Of DataGridViewCell)().
                Select(Function(cell) Convert.ToString(cell.Value, CultureInfo.CurrentCulture)).ToArray()
            If values.All(Function(value) String.IsNullOrWhiteSpace(value)) Then Continue For
            Dim rowLabel = $"Row {row.Index + 1}"
            design.Additions.Add(New DesignAddition With {
                .Name = values(0),
                .DesignPoints = If(ReadOptional(values(1), $"{rowLabel} Design Points"), 0D),
                .DirectCostNuyen = If(ReadOptional(values(2), $"{rowLabel} direct cost"), 0D),
                .CargoConsumed = If(ReadOptional(values(3), $"{rowLabel} cargo"), 0D),
                .LoadConsumedKg = If(ReadOptional(values(4), $"{rowLabel} load"), 0D)
            })
        Next
        Return design
    End Function

    Private Shared Function ReadNumber(value As String, label As String) As Decimal
        Dim number As Decimal
        If Not Decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, number) Then
            Throw New ArgumentException($"{label} needs a number.")
        End If
        Return number
    End Function

    Private Shared Function ReadOptional(value As String, label As String) As Decimal?
        If String.IsNullOrWhiteSpace(value) Then Return Nothing
        Return ReadNumber(value, label)
    End Function

    Private Sub EditUpgrades(sender As Object, e As EventArgs)
        Dim chassis = DirectCast(chassisBox.SelectedItem, StockChassis)
        Dim plant = DirectCast(powerPlantBox.SelectedItem, StockPowerPlant)
        If chassis.Hull.HasValue AndAlso upgrades Is Nothing Then
            ShowError(New ArgumentException("Use manual entries for Hull-rated vehicle options."))
            Return
        End If
        Using dialog As New UpgradeDialog(upgrades, chassis, plant)
            If dialog.ShowDialog(Me) = DialogResult.OK Then
                upgrades = dialog.Selection
                ClearTotals(sender, e)
            End If
        End Using
    End Sub

    Private Sub EditModifications(sender As Object, e As EventArgs)
        Dim chassis = DirectCast(chassisBox.SelectedItem, StockChassis)
        Dim plant = DirectCast(powerPlantBox.SelectedItem, StockPowerPlant)
        Using dialog As New ModificationDialog(catalogModifications, chassis, plant)
            If dialog.ShowDialog(Me) = DialogResult.OK Then
                catalogModifications = dialog.ReadSelection()
                ClearTotals(sender, e)
            End If
        End Using
    End Sub

    Private Sub ShowTotals(result As StockDesignResult)
        Dim totalsValue = result.Totals
        totals.Text = $"{totalsValue.DesignPoints:N} Design Points    Base ¥{totalsValue.BaseCostNuyen:N}    Street ¥{totalsValue.StreetCostNuyen:N}" &
            Environment.NewLine & $"Remaining cargo {totalsValue.CargoRemaining} CF    Remaining load {totalsValue.LoadRemainingKg} kg"
        If result.AppliedModifications IsNot Nothing Then
            totals.Text &= Environment.NewLine & $"{result.AppliedModifications.Count} catalog modifications applied."
        End If
        Dim modified = result.ModifiedRatings
        If modified IsNot Nothing Then
            Dim values As New List(Of String)
            If modified.Armor.HasValue Then values.Add($"Armor {modified.Armor}")
            If modified.Bulwark.HasValue Then values.Add($"Bulwark {modified.Bulwark}")
            If modified.Handling.HasValue Then values.Add($"Final Handling {modified.Handling}")
            If modified.OnRoadHandling.HasValue Then values.Add($"Final Handling {modified.OnRoadHandling}/{modified.OffRoadHandling}")
            If modified.Sensor.HasValue Then values.Add($"Sensor {modified.Sensor}")
            If modified.Sonar.HasValue Then values.Add($"Sonar {modified.Sonar}")
            If modified.TowedArraySonar.HasValue Then values.Add($"Towed-array sonar {modified.TowedArraySonar}")
            totals.Text &= Environment.NewLine & String.Join("   ", values)
        End If
        If result.Availability IsNot Nothing Then
            totals.Text &= Environment.NewLine & $"Availability {result.Availability.Rating} / {result.Availability.BaseDays} days" &
                If(result.Availability.PlayerAccessible, "", " (outside player reach)")
        End If
        Dim ratings = result.UpgradedRatings
        If ratings IsNot Nothing Then
            Dim handling = If(ratings.Handling?.ToString(),
                              If(ratings.OnRoadHandling.HasValue, $"{ratings.OnRoadHandling}/{ratings.OffRoadHandling}", "—"))
            totals.Text &= Environment.NewLine &
                $"Upgraded handling {handling}   Speed {If(ratings.Speed?.ToString(), "—")}   Accel {If(ratings.Acceleration?.ToString(), "—")}   Signature {If(ratings.Signature?.ToString(), "—")}"
            If ratings.Economy.HasValue Then totals.Text &= $"   Economy {ratings.Economy} {ratings.EconomyUnit}"
            If ratings.Depth.HasValue OrElse ratings.FuelCapacity.HasValue Then
                totals.Text &= Environment.NewLine
                If ratings.Depth.HasValue Then totals.Text &= $"Depth {ratings.Depth} m   "
                If ratings.FuelCapacity.HasValue Then totals.Text &= $"Fuel {ratings.FuelCapacity} {ratings.FuelUnit}"
                If ratings.TakeoffLandingDistanceMultiplier.HasValue Then totals.Text &= "   Takeoff and landing distances +10%"
            End If
        End If
    End Sub

    Private Sub Calculate(sender As Object, e As EventArgs)
        Try
            ShowTotals(ReadDesign().Calculate(catalog))
        Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is OverflowException
            ShowError(ex)
        End Try
    End Sub

    Private Sub ExportHtml(sender As Object, e As EventArgs)
        Try
            Dim design = ReadDesign()
            design.Calculate(catalog)
            Using dialog As New SaveFileDialog With {.Filter = "HTML vehicle sheet (*.html)|*.html", .DefaultExt = "html"}
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
                VehicleSheet.Write(dialog.FileName, design, catalog)
                MessageBox.Show(Me, "The HTML vehicle sheet is ready to open and print.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Using
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse
                                   TypeOf ex Is ArgumentException OrElse TypeOf ex Is OverflowException
            ShowError(ex)
        End Try
    End Sub

    Private Sub OpenDesign(sender As Object, e As EventArgs)
        Using dialog As New OpenFileDialog With {.Filter = "The Shop design (*.json)|*.json"}
            If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
            Try
                LoadDesignFile(dialog.FileName)
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse
                                       TypeOf ex Is JsonException OrElse TypeOf ex Is ArgumentException OrElse TypeOf ex Is OverflowException
                ShowError(ex)
            End Try
        End Using
    End Sub

    Private Sub SaveDesign(sender As Object, e As EventArgs)
        Try
            Dim design = ReadDesign()
            Dim result = design.Calculate(catalog)
            Using dialog As New SaveFileDialog With {.Filter = "The Shop design (*.json)|*.json", .DefaultExt = "json"}
                If dialog.ShowDialog(Me) <> DialogResult.OK Then Return
                WriteDesignFile(dialog.FileName, design)
                ShowTotals(result)
            End Using
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse
                                   TypeOf ex Is ArgumentException OrElse TypeOf ex Is OverflowException
            ShowError(ex)
        End Try
    End Sub

    Friend Sub LoadDesignFile(source As String)
        Dim design = JsonSerializer.Deserialize(Of StockDesign)(File.ReadAllText(source), jsonOptions)
        If design Is Nothing Then Throw New JsonException("Expected a stock vehicle design object.")
        DisplayDesign(design)
    End Sub

    Private Sub DisplayDesign(design As StockDesign)
        Dim result = design.Calculate(catalog)

        chassisBox.SelectedItem = StockCatalog.FindById(catalog, result.ChassisId)
        Dim chassis = DirectCast(chassisBox.SelectedItem, StockChassis)
        powerPlantBox.SelectedItem = chassis.PowerPlants.Single(Function(plant) plant.Id = result.PowerPlantId)
        streetIndex.Text = design.StreetIndex.ToString(CultureInfo.CurrentCulture)
        vehicleName.Text = design.Name
        markup.Text = design.MarkupAdjustment.ToString(CultureInfo.CurrentCulture)
        cargo.Text = design.CargoCapacity?.ToString(CultureInfo.CurrentCulture)
        loadOverride.Text = design.LoadCapacityKg?.ToString(CultureInfo.CurrentCulture)
        upgrades = design.Upgrades
        catalogModifications = design.CatalogModifications
        legacyReference = design.LegacyReference
        availabilityBox.SelectedItem = If(design.AvailabilityClass.HasValue, CObj(design.AvailabilityClass.GetValueOrDefault()), "Unspecified")
        additions.Rows.Clear()
        For Each item In design.Additions
            additions.Rows.Add(item.Name, item.DesignPoints, item.DirectCostNuyen, item.CargoConsumed, item.LoadConsumedKg)
        Next
        ShowTotals(result)
    End Sub

    Private Sub ImportLegacy(sender As Object, e As EventArgs)
        Using picker As New OpenFileDialog With {.Filter = "Legacy Shop vehicle (*.shp)|*.shp"}
            If picker.ShowDialog(Me) <> DialogResult.OK Then Return
            Try
                Dim reference = LegacyVehicleReader.Read(picker.FileName)
                Dim chassis = DirectCast(chassisBox.SelectedItem, StockChassis)
                Dim plant = DirectCast(powerPlantBox.SelectedItem, StockPowerPlant)
                Dim message = $"Start a new Rigger 3 Revised design using {chassis.Name} / {plant.Name}." &
                    Environment.NewLine & "The original values are kept for reference. Review and reapply modifications under the revised rules."
                If ShowLegacyReference(reference, message) = DialogResult.OK Then
                    StartLegacyDesign(reference)
                End If
            Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse
                                       TypeOf ex Is FormatException OrElse TypeOf ex Is MalformedLineException
                ShowError(ex)
            End Try
        End Using
    End Sub

    Friend Sub StartLegacyDesign(reference As JsonElement)
        DisplayDesign(New StockDesign With {
            .ChassisId = DirectCast(chassisBox.SelectedItem, StockChassis).Id,
            .PowerPlantId = DirectCast(powerPlantBox.SelectedItem, StockPowerPlant).Id,
            .StreetIndex = 1D,
            .Name = reference.GetProperty("Name").GetString(),
            .LegacyReference = reference
        })
    End Sub

    Private Sub ViewLegacyReference(sender As Object, e As EventArgs)
        If legacyReference.HasValue Then
            ShowLegacyReference(legacyReference.Value, Nothing)
        Else
            MessageBox.Show(Me, "This design has no imported legacy reference.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Function ShowLegacyReference(reference As JsonElement, confirmation As String) As DialogResult
        Using dialog As New Form With {.Text = "Legacy Shop vehicle", .StartPosition = FormStartPosition.CenterParent,
                                       .ClientSize = New Size(640, 455), .MinimizeBox = False, .MaximizeBox = False}
            Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 3, .Padding = New Padding(12)}
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            dialog.Controls.Add(layout)
            layout.Controls.Add(New Label With {.Text = confirmation, .AutoSize = True, .MaximumSize = New Size(610, 0)}, 0, 0)
            layout.Controls.Add(New TextBox With {.Text = JsonSerializer.Serialize(reference, jsonOptions),
                .Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Both, .Dock = DockStyle.Fill}, 0, 1)
            Dim actions As New FlowLayoutPanel With {.AutoSize = True, .Dock = DockStyle.Fill}
            Dim closeButton As New Button With {.Text = If(confirmation Is Nothing, "Close", "Cancel"), .DialogResult = DialogResult.Cancel}
            actions.Controls.Add(closeButton)
            If confirmation IsNot Nothing Then
                actions.Controls.Add(New Button With {.Text = "Start new design", .AutoSize = True, .DialogResult = DialogResult.OK})
            End If
            dialog.CancelButton = closeButton
            layout.Controls.Add(actions, 0, 2)
            Return dialog.ShowDialog(Me)
        End Using
    End Function

    Friend Sub WriteDesignFile(destination As String, design As StockDesign)
        Dim tempPath = Path.Combine(Path.GetDirectoryName(destination), $".the-shop-{Guid.NewGuid():N}.tmp")
        Try
            File.WriteAllText(tempPath, JsonSerializer.Serialize(design, jsonOptions))
            File.Move(tempPath, destination, overwrite:=True)
        Finally
            File.Delete(tempPath)
        End Try
    End Sub

    Private Sub ShowError(ex As Exception)
        MessageBox.Show(Me, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning)
    End Sub
End Class
