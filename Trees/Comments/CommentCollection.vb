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
''' A read-only colleciton of comments.
''' </summary>
''' <remarks>
''' This is not a tree -- it's part of the base Tree type.
''' </remarks>
Public NotInheritable Class CommentCollection
    Implements IList

    ''' <summary>
    ''' An enumerator for a CommentEnumerator.
    ''' </summary>
    Public NotInheritable Class CommentEnumerator
        Implements IEnumerator

        Private ReadOnly _Collection As CommentCollection
        Private _Index As Integer = -1

        ''' <summary>
        ''' Constructs a new enumerator for a CommentCollection.
        ''' </summary>
        ''' <param name="collection">The comments in the collection.</param>
        Public Sub New(ByVal collection As CommentCollection)
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
        Public ReadOnly Property Current() As Comment
            Get
                If _Index = -1 OrElse _Index > _Collection._Comments.Length - 1 Then
                    Throw New InvalidOperationException("Enumerator is not positioned on an element.")
                End If

                Return _Collection._Comments(_Index)
            End Get
        End Property

        ''' <summary>
        ''' Move to the next value in the collection.
        ''' </summary>
        ''' <returns>Whether the enumerator is positioned on an element.</returns>
        Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
            If _Index < _Collection._Comments.Length Then
                _Index += 1
            End If

            Return _Index < _Collection._Comments.Length
        End Function

        ''' <summary>
        ''' Reset the enumerator to before the first element.
        ''' </summary>
        Public Sub Reset() Implements IEnumerator.Reset
            _Index = -1
        End Sub
    End Class

    Private ReadOnly _Comments() As Comment

    ''' <summary>
    ''' The number of elements in the collection.
    ''' </summary>
    Public ReadOnly Property Count() As Integer Implements IList.Count
        Get
            Return _Comments.Length
        End Get
    End Property

    Private Property IListItem(ByVal index As Integer) As Object Implements IList.Item
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
    Default Public ReadOnly Property Item(ByVal index As Integer) As Comment
        Get
            If index < 0 OrElse index > _Comments.Length - 1 Then
                Throw New IndexOutOfRangeException("Invalid Index.")
            End If

            Return _Comments(index)
        End Get
    End Property

    ''' <summary>
    ''' Constructs a new collection of comments.
    ''' </summary>
    ''' <param name="comments">The comments in the collection.</param>
    Public Sub New(ByVal comments As ArrayList)
        Dim CommentArray() As Comment

        If comments Is Nothing OrElse comments.Count = 0 Then
            Throw New ArgumentException("CommentCollection cannot be empty.")
        End If

        CommentArray = New Comment(comments.Count - 1) {}
        comments.CopyTo(CommentArray)

        _Comments = CommentArray
    End Sub

    Public Sub CopyTo(ByVal array As Array, ByVal index As Integer) Implements IList.CopyTo
        _Comments.CopyTo(array, index)
    End Sub

    ''' <summary>
    ''' Make a copy of the values in the collection.
    ''' </summary>
    ''' <param name="array">The array to copy into.</param>
    ''' <param name="index">The index to start.</param>
    Public Sub CopyTo(ByVal array() As Comment, ByVal index As Integer)
        _Comments.CopyTo(array, index)
    End Sub

    Private Function GetIListEnumerator() As IEnumerator Implements IList.GetEnumerator
        Return GetEnumerator()
    End Function

    ''' <summary>
    ''' Creates a new enumerator for the collection.
    ''' </summary>
    ''' <returns>A new enumerator.</returns>
    Public Function GetEnumerator() As CommentEnumerator
        Return New CommentEnumerator(Me)
    End Function

    Public Function Contains(ByVal value As Object) As Boolean Implements IList.Contains
        Return IndexOf(value) >= 0
    End Function

    ''' <summary>
    ''' Determines if the collection contains a value.
    ''' </summary>
    ''' <param name="location">The value to lookup.</param>
    ''' <returns>True if the collection contains the value, False otherwise.</returns>
    Public Function Contains(ByVal value As Comment) As Boolean
        Return IndexOf(value) >= 0
    End Function

    Public Function IndexOf(ByVal value As Object) As Integer Implements IList.IndexOf
        Return Array.IndexOf(_Comments, value)
    End Function

    ''' <summary>
    ''' Determines the index of the specified value.
    ''' </summary>
    ''' <param name="value">The value to look for.</param>
    ''' <returns>The index of the value if it is in the collection, -1 otherwise.</returns>
    Public Function IndexOf(ByVal value As Comment) As Integer
        Dim Index As Integer = 0

        For Each Comment As Comment In _Comments
            If value.Equals(Comment) Then
                Return Index
            End If

            Index += 1
        Next

        Return -1
    End Function

    Public ReadOnly Property IsSynchronized() As Boolean Implements IList.IsSynchronized
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property SyncRoot() As Object Implements IList.SyncRoot
        Get
            Return _Comments.SyncRoot
        End Get
    End Property

    Public ReadOnly Property IsFixedSize() As Boolean Implements IList.IsFixedSize
        Get
            Return True
        End Get
    End Property

    Public ReadOnly Property IsReadOnly() As Boolean Implements IList.IsReadOnly
        Get
            Return True
        End Get
    End Property

    Private Function Add(ByVal value As Object) As Integer Implements IList.Add
        Throw New NotSupportedException
    End Function

    Private Sub Clear() Implements IList.Clear
        Throw New NotSupportedException
    End Sub

    Private Sub Insert(ByVal index As Integer, ByVal value As Object) Implements IList.Insert
        Throw New NotSupportedException
    End Sub

    Private Sub Remove(ByVal value As Object) Implements IList.Remove
        Throw New NotSupportedException
    End Sub

    Private Sub RemoveAt(ByVal index As Integer) Implements IList.RemoveAt
        Throw New NotSupportedException
    End Sub
End Class
