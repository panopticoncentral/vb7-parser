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
''' A read-only collection of initializers.
''' </summary>
Public NotInheritable Class InitializerCollection
    Inherits CommaDelimitedCollection

    ''' <summary>
    ''' An enumerator for an InitializerEnumerator.
    ''' </summary>
    Public NotInheritable Class InitializerEnumerator
        Implements IEnumerator

        Private ReadOnly _Collection As InitializerCollection
        Private _Index As Integer = -1

        ''' <summary>
        ''' Constructs an enumerator for an InitializerCollection.
        ''' </summary>
        ''' <param name="collection">The initializers in the collection.</param>
        Public Sub New(ByVal collection As InitializerCollection)
            _Collection = collection
        End Sub

        Private ReadOnly Property IEnumeratorCurrent() As Object Implements IEnumerator.Current
            Get
                Return Current
            End Get
        End Property

        ''' <summary>
        ''' The current value of the enumerator.
        ''' </summary>
        Public ReadOnly Property Current() As Initializer
            Get
                If _Index = -1 OrElse _Index > _Collection._Initializers.Length - 1 Then
                    Throw New InvalidOperationException("Enumerator is not positioned on an element.")
                End If

                Return _Collection._Initializers(_Index)
            End Get
        End Property

        ''' <summary>
        ''' Move to the next value in the collection.
        ''' </summary>
        ''' <returns>Whether the enumerator is positioned on an element.</returns>
        Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
            If _Index < _Collection._Initializers.Length Then
                _Index += 1
            End If

            Return _Index < _Collection._Initializers.Length
        End Function

        ''' <summary>
        ''' Reset the enumerator to before the first element.
        ''' </summary>
        Public Sub Reset() Implements IEnumerator.Reset
            _Index = -1
        End Sub
    End Class

    Private ReadOnly _Initializers() As Initializer
    Private ReadOnly _RightCurlyBraceLocation As Location

    ''' <summary>
    ''' The location of the '}'.
    ''' </summary>
    Public ReadOnly Property RightCurlyBraceLocation() As Location
        Get
            Return _RightCurlyBraceLocation
        End Get
    End Property

    ''' <summary>
    ''' The number of elements in the collection.
    ''' </summary>
    Public Overrides ReadOnly Property Count() As Integer
        Get
            Return _Initializers.Length
        End Get
    End Property

    Friend Overrides Property IListItem(ByVal index As Integer) As Object
        Get
            Return Item(index)
        End Get

        Set(ByVal value As Object)
            Throw New NotSupportedException
        End Set
    End Property

    ''' <summary>
    ''' The values in the collection.
    ''' </summary>
    ''' <param name="index">The index of the value to retrieve.</param>
    Default Public ReadOnly Property Item(ByVal index As Integer) As Initializer
        Get
            If index < 0 OrElse index > _Initializers.Length - 1 Then
                Throw New IndexOutOfRangeException("Invalid Index.")
            End If

            Return _Initializers(index)
        End Get
    End Property

    ''' <summary>
    ''' Constructs a new initializer collection.
    ''' </summary>
    ''' <param name="initializers">The initializers in the collection.</param>
    ''' <param name="commaLocations">The locations of the commas in the collection.</param>
    ''' <param name="rightCurlyBraceLocation">The location of the '}'.</param>
    ''' <param name="span">The location of the parse tree.</param>
    Public Sub New(ByVal initializers As ArrayList, ByVal commaLocations As ArrayList, ByVal rightCurlyBraceLocation As Location, ByVal span As Span)
        MyBase.New(TreeType.InitializerCollection, commaLocations, span)

        If Not initializers Is Nothing AndAlso initializers.Count > 0 Then
            Dim InitializersArray() As Initializer
            InitializersArray = New Initializer(initializers.Count - 1) {}
            initializers.CopyTo(InitializersArray)
            SetParents(InitializersArray)
            _Initializers = InitializersArray
        Else
            _Initializers = New Initializer(-1) {}
        End If

        _RightCurlyBraceLocation = rightCurlyBraceLocation
    End Sub

    Protected Overloads Overrides Sub CopyTo(ByVal array As Array, ByVal index As Integer)
        _Initializers.CopyTo(array, index)
    End Sub

    ''' <summary>
    ''' Make a copy of the values in the collection.
    ''' </summary>
    ''' <param name="array">The array to copy into.</param>
    ''' <param name="index">The index to start.</param>
    Public Overloads Sub CopyTo(ByVal array() As Initializer, ByVal index As Integer)
        _Initializers.CopyTo(array, index)
    End Sub

    Protected Overrides Function GetIListEnumerator() As IEnumerator
        Return GetEnumerator()
    End Function

    ''' <summary>
    ''' Creates a new enumerator for the collection.
    ''' </summary>
    ''' <returns>A new enumerator.</returns>
    Public Function GetEnumerator() As InitializerEnumerator
        Return New InitializerEnumerator(Me)
    End Function

    Protected Overloads Overrides Function Contains(ByVal value As Object) As Boolean
        Return IndexOf(value) >= 0
    End Function

    ''' <summary>
    ''' Determines if the collection contains a value.
    ''' </summary>
    ''' <param name="value">The value to lookup.</param>
    ''' <returns>True if the collection contains the value, False otherwise.</returns>
    Public Overloads Function Contains(ByVal value As Initializer) As Boolean
        Return IndexOf(value) >= 0
    End Function

    Protected Overloads Overrides Function IndexOf(ByVal value As Object) As Integer
        Return Array.IndexOf(_Initializers, value)
    End Function

    ''' <summary>
    ''' Determines the index of the specified value.
    ''' </summary>
    ''' <param name="value">The value to look for.</param>
    ''' <returns>The index of the value if it is in the collection, -1 otherwise.</returns>
    Public Overloads Function IndexOf(ByVal value As Initializer) As Integer
        Dim Index As Integer = 0

        For Each Tree As Tree In _Initializers
            If Tree Is value Then
                Return Index
            End If

            Index += 1
        Next

        Return -1
    End Function

    Friend Overrides ReadOnly Property SyncRoot() As Object
        Get
            Return _Initializers.SyncRoot
        End Get
    End Property

    Protected Overrides Sub GetChildTrees(ByVal childList As ArrayList)
        AddChildren(childList, _Initializers)
    End Sub
End Class