Imports System.Runtime.CompilerServices
Imports System.Drawing
Imports System.Globalization
Imports System.IO
Imports System.Text.Json
Imports System.Text.Json.Serialization
Imports System.Windows.Forms
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
    Private ReadOnly streetIndex As New TextBox With {.Name = "streetIndex", .Text = "1", .Dock = DockStyle.Fill}
    Private ReadOnly markup As New TextBox With {.Text = "0", .Dock = DockStyle.Fill}
    Private ReadOnly cargo As New TextBox With {.Dock = DockStyle.Fill}
    Private ReadOnly loadOverride As New TextBox With {.Dock = DockStyle.Fill}
    Private ReadOnly startingValues As New Label With {.AutoSize = True, .Dock = DockStyle.Fill, .Padding = New Padding(0, 4, 0, 4)}
    Private ReadOnly totals As New Label With {.Name = "totals", .AutoSize = True, .Dock = DockStyle.Fill, .Padding = New Padding(0, 4, 0, 4)}
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

        Dim fields As New TableLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .ColumnCount = 4, .RowCount = 3}
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
        AddAction(actions, "Open design…", AddressOf OpenDesign)
        AddAction(actions, "Save design…", AddressOf SaveDesign)
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
            .MarkupAdjustment = ReadNumber(markup.Text, "Markup adjustment"),
            .CargoCapacity = ReadOptional(cargo.Text, "Cargo override"),
            .LoadCapacityKg = ReadOptional(loadOverride.Text, "Load override")
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

    Private Sub ShowTotals(result As DesignTotals)
        totals.Text = $"{result.DesignPoints:N} Design Points    Base ¥{result.BaseCostNuyen:N}    Street ¥{result.StreetCostNuyen:N}" &
            Environment.NewLine & $"Remaining cargo {result.CargoRemaining} CF    Remaining load {result.LoadRemainingKg} kg"
    End Sub

    Private Sub Calculate(sender As Object, e As EventArgs)
        Try
            ShowTotals(ReadDesign().Calculate(catalog).Totals)
        Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is OverflowException
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
                ShowTotals(result.Totals)
            End Using
        Catch ex As Exception When TypeOf ex Is IOException OrElse TypeOf ex Is UnauthorizedAccessException OrElse
                                   TypeOf ex Is ArgumentException OrElse TypeOf ex Is OverflowException
            ShowError(ex)
        End Try
    End Sub

    Friend Sub LoadDesignFile(source As String)
        Dim design = JsonSerializer.Deserialize(Of StockDesign)(File.ReadAllText(source), jsonOptions)
        If design Is Nothing Then Throw New JsonException("Expected a stock vehicle design object.")
        Dim result = design.Calculate(catalog)

        chassisBox.SelectedItem = StockCatalog.FindById(catalog, result.ChassisId)
        Dim chassis = DirectCast(chassisBox.SelectedItem, StockChassis)
        powerPlantBox.SelectedItem = chassis.PowerPlants.Single(Function(plant) plant.Id = result.PowerPlantId)
        streetIndex.Text = design.StreetIndex.ToString(CultureInfo.CurrentCulture)
        markup.Text = design.MarkupAdjustment.ToString(CultureInfo.CurrentCulture)
        cargo.Text = design.CargoCapacity?.ToString(CultureInfo.CurrentCulture)
        loadOverride.Text = design.LoadCapacityKg?.ToString(CultureInfo.CurrentCulture)
        additions.Rows.Clear()
        For Each item In design.Additions
            additions.Rows.Add(item.Name, item.DesignPoints, item.DirectCostNuyen, item.CargoConsumed, item.LoadConsumedKg)
        Next
        ShowTotals(result.Totals)
    End Sub

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
