'
' Visual Basic .NET Parser
'
' Copyright (C) 2004, Microsoft Corporation. All rights reserved.
'
' THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
' EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
' MERCHANTIBILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
'

''' <summary>
''' Stores source code line and column information.
''' </summary>
Public Structure Location
    Private ReadOnly _Index As Long
    Private ReadOnly _Line As Long
    Private ReadOnly _Column As Long

    ''' <summary>
    ''' The index in the stream (0-based).
    ''' </summary>
    Public ReadOnly Property Index() As Long
        Get
            Return _Index
        End Get
    End Property

    ''' <summary>
    ''' The physical line number (1-based).
    ''' </summary>
    Public ReadOnly Property Line() As Long
        Get
            Return _Line
        End Get
    End Property

    ''' <summary>
    ''' The physical column number (1-based).
    ''' </summary>
    Public ReadOnly Property Column() As Long
        Get
            Return _Column
        End Get
    End Property

    ''' <summary>
    ''' Whether the location is a valid location.
    ''' </summary>
    Public ReadOnly Property IsValid() As Boolean
        Get
            Return Line <> 0 AndAlso Column <> 0
        End Get
    End Property

    ''' <summary>
    ''' Constructs a new Location for a particular source location.
    ''' </summary>
    ''' <param name="index">The index in the stream (0-based).</param>
    ''' <param name="line">The physical line number (1-based).</param>
    ''' <param name="column">The physical column number (1-based).</param>
    Public Sub New(ByVal index As Long, ByVal line As Long, ByVal column As Long)
        _Index = index
        _Line = line
        _Column = column
    End Sub

    ''' <summary>
    ''' Determines whether the other location is before the location in this object.
    ''' </summary>
    ''' <param name="otherLocation">The location to test against.</param>
    ''' <returns>True if otherLocation is after this location, False otherwise.</returns>
    Public Function Before(ByVal otherLocation As Location) As Boolean
        Return Index <= otherLocation.Index
    End Function

    Public Overrides Function ToString() As String
        Return "(" & Me.Column & "," & Me.Line & ")"
    End Function

    Public Overloads Overrides Function Equals(ByVal obj As Object) As Boolean
        If TypeOf obj Is Location Then
            Return Equals(DirectCast(obj, Location))
        Else
            Return False
        End If
    End Function

    Public Overrides Function GetHashCode() As Integer
        ' Mask off the upper 32 bits of the index and use that as
        ' the hash code.
        Return CInt(Index And &HFFFFFFFFL)
    End Function

    ''' <summary>
    ''' Compares two specified Location values.
    ''' </summary>
    ''' <param name="location">The location to compare.</param>
    ''' <returns>True if the locations are the same, False otherwise.</returns>
    Public Overloads Function Equals(ByVal location As Location) As Boolean
        Return location.Index = Index
    End Function
End Structure