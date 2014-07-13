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
''' A collection of trees that are colon delimited.
''' </summary>
Public MustInherit Class ColonDelimitedCollection
    Inherits BaseCollection

    Private ReadOnly _ColonLocations As LocationCollection

    ''' <summary>
    ''' The locations of the colons in the collection.
    ''' </summary>
    Public ReadOnly Property ColonLocations() As LocationCollection
        Get
            Return _ColonLocations
        End Get
    End Property

    Protected Sub New(ByVal type As TreeType, ByVal colonLocations As ArrayList, ByVal span As Span)
        MyBase.New(type, span)

        Debug.Assert(type = TreeType.StatementCollection OrElse type = TreeType.DeclarationCollection)

        If Not colonLocations Is Nothing AndAlso colonLocations.Count > 0 Then
            _ColonLocations = New LocationCollection(colonLocations)
        End If
    End Sub
End Class