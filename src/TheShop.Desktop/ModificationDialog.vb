Imports System.Drawing
Imports System.Windows.Forms
Imports TheShop.Design

Friend NotInheritable Class ModificationDialog
    Inherits Form

    Private ReadOnly quantities As New List(Of (Rule As ModificationRule, Input As NumericUpDown))

    Public Sub New(current As IReadOnlyList(Of ModificationSelection), chassis As StockChassis, plant As StockPowerPlant)
        Text = "Catalog modifications"
        StartPosition = FormStartPosition.CenterParent
        AutoScaleMode = AutoScaleMode.Dpi
        ClientSize = New Size(580, 500)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False

        Dim frame As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 2}
        frame.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        frame.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        Controls.Add(frame)
        Dim scroll As New Panel With {.AutoScroll = True, .Dock = DockStyle.Fill}
        frame.Controls.Add(scroll, 0, 0)
        Dim table As New TableLayoutPanel With {.AutoSize = True, .Dock = DockStyle.Top, .ColumnCount = 2, .Padding = New Padding(12)}
        scroll.Controls.Add(table)
        Dim explanation As New Label With {.Text = "Use 0 to remove a modification. Catalog costs are calculated automatically.",
            .AutoSize = True, .Margin = New Padding(3, 3, 3, 12)}
        table.Controls.Add(explanation, 0, 0)
        table.SetColumnSpan(explanation, 2)
        table.RowCount = 1

        For Each rule In ModificationCatalog.Rules
            Dim selected = current?.SingleOrDefault(Function(choice) String.Equals(choice.Id, rule.Id, StringComparison.OrdinalIgnoreCase))
            Dim value = If(selected?.Quantity, 0)
            Dim compatible = ModificationCatalog.IsCompatible(rule, chassis, plant)
            Dim label = rule.Name
            If rule.QuantityLabel.Length > 0 Then label &= $" ({rule.QuantityLabel})"
            If rule.Id = "sensors" AndAlso chassis.Sensor.GetValueOrDefault() > 0 Then
                label &= $" · factory {chassis.Sensor}"
            End If
            If Not compatible Then label &= " · unavailable"
            Dim row = table.RowCount
            table.RowCount += 1
            Dim input As New NumericUpDown With {.Maximum = If(compatible, rule.Maximum, value), .Value = value,
                .Enabled = compatible OrElse value > 0}
            quantities.Add((rule, input))
            table.Controls.Add(New Label With {.Text = label, .AutoSize = True, .Anchor = AnchorStyles.Left}, 0, row)
            table.Controls.Add(input, 1, row)
        Next

        Dim actions As New FlowLayoutPanel With {.AutoSize = True, .Dock = DockStyle.Fill}
        Dim apply As New Button With {.Text = "Apply", .DialogResult = DialogResult.OK}
        Dim cancel As New Button With {.Text = "Cancel", .DialogResult = DialogResult.Cancel}
        actions.Controls.Add(apply)
        actions.Controls.Add(cancel)
        frame.Controls.Add(actions, 0, 1)
        AcceptButton = apply
        CancelButton = cancel
    End Sub

    Public Function ReadSelection() As List(Of ModificationSelection)
        Dim selection = quantities.Where(Function(item) item.Input.Value > 0).
            Select(Function(item) New ModificationSelection With {.Id = item.Rule.Id, .Quantity = CInt(item.Input.Value)}).ToList()
        Return If(selection.Count = 0, Nothing, selection)
    End Function
End Class
