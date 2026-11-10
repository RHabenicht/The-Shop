Imports System.Text.Json

Public Module LegacyDesignConversion
    Private ReadOnly supported As New HashSet(Of String)(StringComparer.Ordinal) From {
        "crash-cage", "rigger-adaptation", "datajack-port", "electronics-port", "spotlight", "nitrous",
        "roll-bar"
    }

    Public Function CreateDesign(reference As JsonElement, chassisId As String, plantId As String,
                            catalog As IReadOnlyList(Of StockChassis)) As StockDesign
        Dim result As New StockDesign With {
            .ChassisId = chassisId, .PowerPlantId = plantId, .StreetIndex = 1D,
            .Name = reference.GetProperty("Name").GetString(), .LegacyReference = reference,
            .CatalogModifications = New List(Of ModificationSelection), .LegacyImportNotes = New List(Of String)
        }
        For Each item In reference.GetProperty("Modifications").EnumerateArray()
            Dim name = item.GetProperty("Name").GetString()
            Dim level = item.GetProperty("Level").GetInt32()
            Dim matches = ModificationCatalog.Rules.Where(Function(candidate) supported.Contains(candidate.Id) AndAlso
                String.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray()
            If matches.Length <> 1 OrElse level < 1 Then
                result.LegacyImportNotes.Add($"Review manually: {name} (legacy level {level}).")
                Continue For
            End If
            Dim rule = matches(0)
            Dim choice As New ModificationSelection With {.Id = rule.Id, .Quantity = If(rule.Maximum = 1, 1, level)}
            result.CatalogModifications.Add(choice)
            Try
                result.Calculate(catalog)
                result.LegacyImportNotes.Add($"Repriced using Rigger 3 Revised: {name}.")
            Catch ex As Exception When TypeOf ex Is ArgumentException OrElse TypeOf ex Is OverflowException
                result.CatalogModifications.Remove(choice)
                result.LegacyImportNotes.Add($"Review manually: {name}. {ex.Message}")
            End Try
        Next
        If result.CatalogModifications.Count = 0 Then result.CatalogModifications = Nothing
        If result.LegacyImportNotes.Count = 0 Then result.LegacyImportNotes = Nothing
        Return result
    End Function
End Module
