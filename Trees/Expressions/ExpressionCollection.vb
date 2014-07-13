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
''' A read-only collection of expressions.
''' </summary>
Public NotInheritable Class ExpressionCollection
    Inherits CommaDelimitedCollection

    ''' <summary>
    ''' An enumerator for an ExpressionEnumerator.
    ''' </summary>
    Public NotInheritable Class ExpressionEnumerator
        Implements IEnumerator

        Private ReadOnly _Collection As ExpressionCollection
        Private _Index As Integer = -1

        ''' <summary>
        ''' Constructs a new enumerator for an ExpressionCollection.
        ''' </summary>
        ''' <param name="collection">The expressions to enumerate.</param>
        Public Sub New(ByVal collection As ExpressionCollection)
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
        Public ReadOnly Property Current() As Expression
            Get
                If _Index = -1 OrElse _Index > _Collection._Expressions.Length - 1 Then
                    Throw New InvalidOperationException("Enumerator is not positioned on an element.")
                End If

                Return _Collection._Expressions(_Index)
            End Get
        End Property

        ''' <summary>
        ''' Move to the next value in the collection.
        ''' </summary>
        ''' <returns>Whether the enumerator is positioned on an element.</returns>
        Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
            If _Index < _Collection._Expressions.Length Then
                _Index += 1
            End If

            Return _Index < _Collection._Expressions.Length
        End Function

        ''' <summary>
        ''' Reset the enumerator to before the first element.
        ''' </summary>
        Public Sub Reset() Implements IEnumerator.Reset
            _Index = -1
        End Sub
    End Class

    Private ReadOnly _Expressions() As Expression

    ''' <summary>
    ''' The number of elements in the collection.
    ''' </summary>
    Public Overrides ReadOnly Property Count() As Integer
        Get
            Return _Expressions.Length
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
    Default Public ReadOnly Property Item(ByVal index As Integer) As Expression
        Get
            If index < 0 OrElse index > _Expressions.Length - 1 Then
                Throw New IndexOutOfRangeException("Invalid Index.")
            End If

            Return _Expressions(index)
        End Get
    End Property

    ''' <summary>
    ''' Constructs a new collection of expressions.
    ''' </summary>
    ''' <param name="expressions">The expressions in the collection.</param>
    ''' <param name="commaLocations">The locations of the commas in the collection.</param>
    ''' <param name="span">The location of the parse tree.</param>
    Public Sub New(ByVal expressions As ArrayList, ByVal commaLocations As ArrayList, ByVal span As Span)
        MyBase.New(TreeType.ExpressionCollection, commaLocations, span)

        If (expressions Is Nothing OrElse expressions.Count = 0) AndAlso _
           (commaLocations Is Nothing OrElse commaLocations.Count = 0) Then
            Throw New ArgumentException("ExpressionCollection cannot be empty.")
        End If

        If Not expressions Is Nothing AndAlso expressions.Count > 0 Then
            Dim ExpressionsArray() As Expression
            ExpressionsArray = New Expression(expressions.Count - 1) {}
            expressions.CopyTo(ExpressionsArray)
            SetParents(ExpressionsArray)
            _Expressions = ExpressionsArray
        Else
            _Expressions = New Expression(-1) {}
        End If
    End Sub

    Protected Overloads Overrides Sub CopyTo(ByVal array As Array, ByVal index As Integer)
        _Expressions.CopyTo(array, index)
    End Sub

    ''' <summary>
    ''' Make a copy of the values in the collection.
    ''' </summary>
    ''' <param name="array">The array to copy into.</param>
    ''' <param name="index">The index to start.</param>
    Public Overloads Sub CopyTo(ByVal array() As Expression, ByVal index As Integer)
        _Expressions.CopyTo(array, index)
    End Sub

    Protected Overrides Function GetIListEnumerator() As IEnumerator
        Return GetEnumerator()
    End Function

    ''' <summary>
    ''' Creates a new enumerator for the collection.
    ''' </summary>
    ''' <returns>A new enumerator.</returns>
    Public Function GetEnumerator() As ExpressionEnumerator
        Return New ExpressionEnumerator(Me)
    End Function

    Protected Overloads Overrides Function Contains(ByVal value As Object) As Boolean
        Return IndexOf(value) >= 0
    End Function

    ''' <summary>
    ''' Determines if the collection contains a value.
    ''' </summary>
    ''' <param name="value">The value to lookup.</param>
    ''' <returns>True if the collection contains the value, False otherwise.</returns>
    Public Overloads Function Contains(ByVal value As Expression) As Boolean
        Return IndexOf(value) >= 0
    End Function

    Protected Overloads Overrides Function IndexOf(ByVal value As Object) As Integer
        Return Array.IndexOf(_Expressions, value)
    End Function

    ''' <summary>
    ''' Determines the index of the specified value.
    ''' </summary>
    ''' <param name="value">The value to look for.</param>
    ''' <returns>The index of the value if it is in the collection, -1 otherwise.</returns>
    Public Overloads Function IndexOf(ByVal value As Expression) As Integer
        Dim Index As Integer = 0

        For Each Tree As Tree In _Expressions
            If Tree Is value Then
                Return Index
            End If

            Index += 1
        Next

        Return -1
    End Function

    Friend Overrides ReadOnly Property SyncRoot() As Object
        Get
            Return _Expressions.SyncRoot
        End Get
    End Property

    Protected Overrides Sub GetChildTrees(ByVal childList As ArrayList)
        AddChildren(childList, _Expressions)
    End Sub
End Class