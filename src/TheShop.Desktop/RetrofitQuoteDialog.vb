Imports System.Drawing
Imports System.Windows.Forms
Imports TheShop.Design

Friend NotInheritable Class RetrofitQuoteDialog
    Inherits Form

    Public ReadOnly Property Selection As ModificationSelection

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
        Dim customIndex As New CheckBox With {.Name = "customStreetIndex", .Text = "GM Street Index override", .AutoSize = True}
        Dim streetIndex As New NumericUpDown With {.Name = "retrofitStreetIndex", .Minimum = 0.01D, .Maximum = 1000D, .DecimalPlaces = 2, .Value = 1D, .Enabled = False}
        Dim updating As Boolean
        Dim output As New Label With {.Name = "retrofitOutput", .AutoSize = True, .MinimumSize = New Size(360, 0)}
        Dim closeButton As New Button With {.Text = "Close", .DialogResult = DialogResult.Cancel}
        Dim installButton As New Button With {.Name = "installRetrofit", .Text = "Install", .Enabled = False}
        layout.Controls.Add(options)
        layout.Controls.Add(quantityLabel)
        layout.Controls.Add(quantity)
        layout.Controls.Add(customIndex)
        layout.Controls.Add(streetIndex)
        layout.Controls.Add(output)
        layout.Controls.Add(installButton)
        layout.Controls.Add(closeButton)
        CancelButton = closeButton

        Dim refresh As Action = Sub()
                                    If updating Then Return
                                    Dim rule = TryCast(options.SelectedItem, ModificationRule)
                                    If rule Is Nothing Then
                                        installButton.Enabled = False
                                        output.Text = "No catalog retrofits are available for this vehicle."
                                        Return
                                    End If
                                    Dim result = RetrofitCatalog.Quote(rule, CInt(quantity.Value), chassis, plant, If(customIndex.Checked, CType(streetIndex.Value, Decimal?), Nothing))
                                    Dim cargo = totals.CargoRemaining - result.CargoConsumed
                                    Dim load = totals.LoadRemainingKg - result.LoadConsumedKg
                                    Dim streetCost = If(result.StreetPartsCostNuyen.HasValue,
                                        $"Street parts cost ¥{result.StreetPartsCostNuyen.GetValueOrDefault():N2} (SI {result.StreetIndex})",
                                        "Street Index not listed in Rigger 3 Revised. Street price pending GM review.")
                                    output.Text = $"List parts cost ¥{result.PartsCostNuyen:N2}{Environment.NewLine}" &
                                        streetCost & Environment.NewLine &
                                        $"Availability {result.Availability}{Environment.NewLine}" &
                                        $"Remaining cargo {cargo:0.##} CF   Remaining load {load:0.##} kg"
                                    If result.ExternalCargoCapacity > 0D Then output.Text &= Environment.NewLine & $"External cargo capacity {result.ExternalCargoCapacity} CF."
                                    installButton.Enabled = cargo >= 0D AndAlso load >= 0D
                                    If cargo < 0D OrElse load < 0D Then output.Text &= Environment.NewLine & "Exceeds available capacity."
                                End Sub
        AddHandler installButton.Click,
            Sub(sender, e)
                Dim rule = DirectCast(options.SelectedItem, ModificationRule)
                _Selection = New ModificationSelection With {.Id = rule.Id, .Quantity = CInt(quantity.Value),
                    .StreetIndexOverride = If(customIndex.Checked, CType(streetIndex.Value, Decimal?), Nothing)}
                DialogResult = DialogResult.OK
            End Sub
        AddHandler quantity.ValueChanged, Sub(sender, e) refresh()
        AddHandler streetIndex.ValueChanged, Sub(sender, e) refresh()
        AddHandler customIndex.CheckedChanged,
            Sub(sender, e)
                streetIndex.Enabled = customIndex.Checked
                refresh()
            End Sub
        AddHandler options.SelectedIndexChanged,
            Sub(sender, e)
                Dim rule = TryCast(options.SelectedItem, ModificationRule)
                If rule IsNot Nothing Then
                    updating = True
                    Try
                        quantity.Minimum = 1D
                        quantity.Maximum = ModificationCatalog.MaximumQuantity(rule, chassis)
                        quantity.Minimum = If(rule.Id = "improved-anti-theft-security", 3D, If(rule.Id = "advanced-drone-pilot", 2D, 1D))
                        quantity.Enabled = quantity.Maximum > quantity.Minimum
                        quantityLabel.Text = If(rule.QuantityLabel.Length = 0, "Quantity", rule.QuantityLabel)
                    Finally
                        updating = False
                    End Try
                End If
                refresh()
            End Sub
        options.DataSource = RetrofitCatalog.AvailableRules(chassis, plant).ToArray()
        refresh()
    End Sub
End Class
