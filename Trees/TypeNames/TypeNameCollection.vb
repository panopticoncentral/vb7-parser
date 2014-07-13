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
''' A read-only collection of type names.
''' </summary>
Public NotInheritable Class TypeNameCollection
    Inherits CommaDelimitedCollection

    ''' <summary>
    ''' An enumerator for a TypeEnumerator.
    ''' </summary>
    Public NotInheritable Class TypeEnumerator
        Implements IEnumerator

        Private ReadOnly _Collection As TypeNameCollection
        Private _Index As Integer = -1

        ''' <summary>
        ''' Constructs an enumerator for a TypeNameCollection.
        ''' </summary>
        ''' <param name="collection">The type names in the collection.</param>
        Public Sub New(ByVal collection As TypeNameCollection)
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
        Public ReadOnly Property Current() As TypeName
            Get
                If _Index = -1 OrElse _Index > _Collection._TypeNames.Length - 1 Then
                    Throw New InvalidOperationException("Enumerator is not positioned on an element.")
                End If

                Return _Collection._TypeNames(_Index)
            End Get
        End Property

        ''' <summary>
        ''' Move to the next value in the collection.
        ''' </summary>
        ''' <returns>Whether the enumerator is positioned on an element.</returns>
        Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
            If _Index < _Collection._TypeNames.Length Then
                _Index += 1
            End If

            Return _Index < _Collection._TypeNames.Length
        End Function

        ''' <summary>
        ''' Reset the enumerator to before the first element.
        ''' </summary>
        Public Sub Reset() Implements IEnumerator.Reset
            _Index = -1
        End Sub
    End Class

    Private ReadOnly _TypeNames() As TypeName

    ''' <summary>
    ''' The number of elements in the collection.
    ''' </summary>
    Public Overrides ReadOnly Property Count() As Integer
        Get
            Return _TypeNames.Length
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
    Default Public ReadOnly Property Item(ByVal index As Integer) As TypeName
        Get
            If index < 0 OrElse index > _TypeNames.Length - 1 Then
                Throw New IndexOutOfRangeException("Invalid Index.")
            End If

            Return _TypeNames(index)
        End Get
    End Property

    ''' <summary>
    ''' Constructs a new type name collection.
    ''' </summary>
    ''' <param name="typeMembers">The type names in the collection.</param>
    ''' <param name="commaLocations">The locations of the commas in the collection.</param>
    ''' <param name="span">The location of the parse tree.</param>
    Public Sub New(ByVal typeMembers As ArrayList, ByVal commaLocations As ArrayList, ByVal span As Span)
        MyBase.New(TreeType.TypeNameCollection, commaLocations, span)

        If typeMembers Is Nothing OrElse typeMembers.Count = 0 Then
            Throw New ArgumentException("TypeNameCollection cannot be empty.")
        End If

        Dim TypeMembersArray() As TypeName

        TypeMembersArray = New TypeName(typeMembers.Count - 1) {}
        typeMembers.CopyTo(TypeMembersArray)

        SetParents(TypeMembersArray)

        _TypeNames = TypeMembersArray
    End Sub

    Protected Overloads Overrides Sub CopyTo(ByVal array As Array, ByVal index As Integer)
        _TypeNames.CopyTo(array, index)
    End Sub

    ''' <summary>
    ''' Make a copy of the values in the collection.
    ''' </summary>
    ''' <param name="array">The array to copy into.</param>
    ''' <param name="index">The index to start.</param>
    Public Overloads Sub CopyTo(ByVal array() As TypeName, ByVal index As Integer)
        _TypeNames.CopyTo(array, index)
    End Sub

    Protected Overrides Function GetIListEnumerator() As IEnumerator
        Return GetEnumerator()
    End Function

    ''' <summary>
    ''' Creates a new enumerator for the collection.
    ''' </summary>
    ''' <returns>A new enumerator.</returns>
    Public Function GetEnumerator() As TypeEnumerator
        Return New TypeEnumerator(Me)
    End Function

    Protected Overloads Overrides Function Contains(ByVal value As Object) As Boolean
        Return IndexOf(value) >= 0
    End Function

    ''' <summary>
    ''' Determines if the collection contains a value.
    ''' </summary>
    ''' <param name="value">The value to lookup.</param>
    ''' <returns>True if the collection contains the value, False otherwise.</returns>
    Public Overloads Function Contains(ByVal value As TypeName) As Boolean
        Return IndexOf(value) >= 0
    End Function

    Protected Overloads Overrides Function IndexOf(ByVal value As Object) As Integer
        Return Array.IndexOf(_TypeNames, value)
    End Function

    ''' <summary>
    ''' Determines the index of the specified value.
    ''' </summary>
    ''' <param name="value">The value to look for.</param>
    ''' <returns>The index of the value if it is in the collection, -1 otherwise.</returns>
    Public Overloads Function IndexOf(ByVal value As TypeName) As Integer
        Dim Index As Integer = 0

        For Each Tree As Tree In _TypeNames
            If Tree Is value Then
                Return Index
            End If

            Index += 1
        Next

        Return -1
    End Function

    Friend Overrides ReadOnly Property SyncRoot() As Object
        Get
            Return _TypeNames.SyncRoot
        End Get
    End Property

    Protected Overrides Sub GetChildTrees(ByVal childList As ArrayList)
        AddChildren(childList, _TypeNames)
    End Sub
End Class