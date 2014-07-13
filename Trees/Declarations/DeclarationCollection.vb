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
''' A read-only collection of declarations.
''' </summary>
Public NotInheritable Class DeclarationCollection
    Inherits ColonDelimitedCollection

    ''' <summary>
    ''' An enumerator for a DeclarationEnumerator.
    ''' </summary>
    Public NotInheritable Class DeclarationEnumerator
        Implements IEnumerator

        Private ReadOnly _Collection As DeclarationCollection
        Private _Index As Integer = -1

        ''' <summary>
        ''' Constructs a new enumerator for a DeclarationCollection.
        ''' </summary>
        ''' <param name="collection">The declarations to enumerate.</param>
        Public Sub New(ByVal collection As DeclarationCollection)
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
        Public ReadOnly Property Current() As Declaration
            Get
                If _Index = -1 OrElse _Index > _Collection._Declarations.Length - 1 Then
                    Throw New InvalidOperationException("Enumerator is not positioned on an element.")
                End If

                Return _Collection._Declarations(_Index)
            End Get
        End Property

        ''' <summary>
        ''' Move to the next value in the collection.
        ''' </summary>
        ''' <returns>Whether the enumerator is positioned on an element.</returns>
        Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
            If _Index < _Collection._Declarations.Length Then
                _Index += 1
            End If

            Return _Index < _Collection._Declarations.Length
        End Function

        ''' <summary>
        ''' Reset the enumerator to before the first element.
        ''' </summary>
        Public Sub Reset() Implements IEnumerator.Reset
            _Index = -1
        End Sub
    End Class

    Private ReadOnly _Declarations() As Declaration

    ''' <summary>
    ''' The number of elements in the collection.
    ''' </summary>
    Public Overrides ReadOnly Property Count() As Integer
        Get
            Return _Declarations.Length
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
    Default Public ReadOnly Property Item(ByVal index As Integer) As Declaration
        Get
            If index < 0 OrElse index > _Declarations.Length - 1 Then
                Throw New IndexOutOfRangeException("Invalid Index.")
            End If

            Return _Declarations(index)
        End Get
    End Property

    ''' <summary>
    ''' Constructs a new collection of declarations.
    ''' </summary>
    ''' <param name="declarations">The declarations in the collection.</param>
    ''' <param name="colonLocations">The locations of the colons in the collection.</param>
    ''' <param name="span">The location of the parse tree.</param>
    Public Sub New(ByVal declarations As ArrayList, ByVal colonLocations As ArrayList, ByVal span As Span)
        MyBase.New(TreeType.DeclarationCollection, colonLocations, span)

        ' A declaration collection may need to hold just a colon.
        If (declarations Is Nothing OrElse declarations.Count = 0) AndAlso _
           (colonLocations Is Nothing OrElse colonLocations.Count = 0) Then
            Throw New ArgumentException("DeclarationCollection cannot be empty.")
        End If

        If Not declarations Is Nothing AndAlso declarations.Count > 0 Then
            Dim DeclarationsArray() As Declaration

            DeclarationsArray = New Declaration(declarations.Count - 1) {}
            declarations.CopyTo(DeclarationsArray)

            SetParents(DeclarationsArray)

            _Declarations = DeclarationsArray
        Else
            _Declarations = New Declaration(-1) {}
        End If
    End Sub

    Protected Overloads Overrides Sub CopyTo(ByVal array As Array, ByVal index As Integer)
        _Declarations.CopyTo(array, index)
    End Sub

    ''' <summary>
    ''' Make a copy of the values in the collection.
    ''' </summary>
    ''' <param name="array">The array to copy into.</param>
    ''' <param name="index">The index to start.</param>
    Public Overloads Sub CopyTo(ByVal array() As Declaration, ByVal index As Integer)
        _Declarations.CopyTo(array, index)
    End Sub

    Protected Overrides Function GetIListEnumerator() As IEnumerator
        Return GetEnumerator()
    End Function

    ''' <summary>
    ''' Creates a new enumerator for the collection.
    ''' </summary>
    ''' <returns>A new enumerator.</returns>
    Public Function GetEnumerator() As DeclarationEnumerator
        Return New DeclarationEnumerator(Me)
    End Function

    Protected Overloads Overrides Function Contains(ByVal value As Object) As Boolean
        Return IndexOf(value) >= 0
    End Function

    ''' <summary>
    ''' Determines if the collection contains a value.
    ''' </summary>
    ''' <param name="value">The value to lookup.</param>
    ''' <returns>True if the collection contains the value, False otherwise.</returns>
    Public Overloads Function Contains(ByVal value As Declaration) As Boolean
        Return IndexOf(value) >= 0
    End Function

    Protected Overloads Overrides Function IndexOf(ByVal value As Object) As Integer
        Return Array.IndexOf(_Declarations, value)
    End Function

    ''' <summary>
    ''' Determines the index of the specified value.
    ''' </summary>
    ''' <param name="value">The value to look for.</param>
    ''' <returns>The index of the value if it is in the collection, -1 otherwise.</returns>
    Public Overloads Function IndexOf(ByVal value As Declaration) As Integer
        Dim Index As Integer = 0

        For Each Tree As Tree In _Declarations
            If Tree Is value Then
                Return Index
            End If

            Index += 1
        Next

        Return -1
    End Function

    Friend Overrides ReadOnly Property SyncRoot() As Object
        Get
            Return _Declarations.SyncRoot
        End Get
    End Property

    Protected Overrides Sub GetChildTrees(ByVal childList As ArrayList)
        AddChildren(childList, _Declarations)
    End Sub
End Class