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
''' A collection of trees that are delimited by commas.
''' </summary>
Public MustInherit Class CommaDelimitedCollection
    Inherits BaseCollection

    Private ReadOnly _CommaLocations As LocationCollection

    ''' <summary>
    ''' The location of the commas in the list.
    ''' </summary>
    Public ReadOnly Property CommaLocations() As LocationCollection
        Get
            Return _CommaLocations
        End Get
    End Property

    Protected Sub New(ByVal type As TreeType, ByVal commaLocations As ArrayList, ByVal span As Span)
        MyBase.New(type, span)

        Debug.Assert(type >= TreeType.ArgumentCollection AndAlso type <= TreeType.ImportCollection)

        If Not commaLocations Is Nothing AndAlso commaLocations.Count > 0 Then
            _CommaLocations = New LocationCollection(commaLocations)
        End If
    End Sub
End Class
