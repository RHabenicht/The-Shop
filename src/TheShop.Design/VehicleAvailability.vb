Imports System.Text.Json.Serialization

<JsonConverter(GetType(JsonStringEnumConverter(Of VehicleAvailabilityClass)))>
Public Enum VehicleAvailabilityClass
    Standard
    Luxury
    Security
    Military
End Enum

<JsonConverter(GetType(JsonStringEnumConverter(Of VehicleSpecialtyClass)))>
Public Enum VehicleSpecialtyClass
    None
    Medical
    NonCombat
End Enum

Public NotInheritable Class VehicleAvailability
    Public Property Rating As Decimal
    Public Property BaseDays As Decimal
    Public Property PlayerAccessible As Boolean

    Friend Shared Function Calculate(classification As VehicleAvailabilityClass, baseCost As Decimal) As VehicleAvailability
        Dim modifier As Integer
        Select Case classification
            Case VehicleAvailabilityClass.Standard
                modifier = 0
            Case VehicleAvailabilityClass.Luxury
                modifier = 4
            Case VehicleAvailabilityClass.Security
                modifier = 6
            Case VehicleAvailabilityClass.Military
                modifier = 10
            Case Else
                Throw New ArgumentException("Unknown Availability class.")
        End Select
        Dim rating = Math.Max(2D, Math.Ceiling(baseCost / 20000D)) + modifier
        Return New VehicleAvailability With {.Rating = rating, .BaseDays = rating, .PlayerAccessible = rating <= 30D}
    End Function
End Class
