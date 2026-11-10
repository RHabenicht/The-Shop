Imports System.Drawing
Imports System.Windows.Forms
Imports TheShop.Design

Friend NotInheritable Class RetrofitQuoteDialog
    Inherits Form

    Public Sub New(chassis As StockChassis, plant As StockPowerPlant, totals As DesignTotals)
        Text = "Retrofit parts quote"
        StartPosition = FormStartPosition.CenterParent
        AutoSize = True
        AutoSizeMode = AutoSizeMode.GrowAndShrink
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False

        Dim layout As New FlowLayoutPanel With {
            .AutoSize = True, .AutoSizeMode = AutoSizeMode.GrowAndShrink, .FlowDirection = FlowDirection.TopDown,
            .WrapContents = False, .Dock = DockStyle.Fill, .Padding = New Padding(12)
        }
        Controls.Add(layout)
        layout.Controls.Add(New Label With {.AutoSize = True, .Text = "Parts only. Capacity is checked against the current design."})
        Dim options As New ComboBox With {.Name = "retrofitOptions", .DropDownStyle = ComboBoxStyle.DropDownList, .Width = 320, .DisplayMember = NameOf(ModificationRule.Name)}
        Dim quantity As New NumericUpDown With {.Name = "retrofitQuantity", .Minimum = 1, .Maximum = Integer.MaxValue, .Value = 1}
        Dim quantityLabel As New Label With {.AutoSize = True}
        Dim output As New Label With {.Name = "retrofitOutput", .AutoSize = True, .MinimumSize = New Size(360, 0)}
        Dim closeButton As New Button With {.Text = "Close", .DialogResult = DialogResult.OK}
        layout.Controls.Add(options)
        layout.Controls.Add(quantityLabel)
        layout.Controls.Add(quantity)
        layout.Controls.Add(output)
        layout.Controls.Add(closeButton)
        CancelButton = closeButton

        Dim refresh As Action = Sub()
                                    Dim rule = TryCast(options.SelectedItem, ModificationRule)
                                    If rule Is Nothing Then
                                        output.Text = "No catalog retrofits are available for this vehicle."
                                        Return
                                    End If
                                    Dim result = RetrofitCatalog.Quote(rule, CInt(quantity.Value), chassis, plant)
                                    Dim cargo = totals.CargoRemaining - result.CargoConsumed
                                    Dim load = totals.LoadRemainingKg - result.LoadConsumedKg
                                    output.Text = $"List parts cost ¥{result.PartsCostNuyen:N2}{Environment.NewLine}" &
                                        $"Street parts cost ¥{result.StreetPartsCostNuyen:N2} (SI {result.StreetIndex}){Environment.NewLine}" &
                                        $"Availability {result.Availability}{Environment.NewLine}" &
                                        $"Remaining cargo {cargo:0.##} CF   Remaining load {load:0.##} kg"
                                    If cargo < 0D OrElse load < 0D Then output.Text &= Environment.NewLine & "Exceeds available capacity."
                                End Sub
        AddHandler quantity.ValueChanged, Sub(sender, e) refresh()
        AddHandler options.SelectedIndexChanged,
            Sub(sender, e)
                Dim rule = TryCast(options.SelectedItem, ModificationRule)
                If rule IsNot Nothing Then
                    quantity.Maximum = ModificationCatalog.MaximumQuantity(rule, chassis)
                    quantity.Enabled = quantity.Maximum > 1
                    quantityLabel.Text = If(rule.QuantityLabel.Length = 0, "Quantity", rule.QuantityLabel)
                End If
                refresh()
            End Sub
        options.DataSource = RetrofitCatalog.AvailableRules(chassis, plant).ToArray()
        refresh()
    End Sub
End Class
