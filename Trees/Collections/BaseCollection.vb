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
''' The base class of any collection of trees.
''' </summary>
Public MustInherit Class BaseCollection
    Inherits Tree
    Implements IList

    Protected Sub New(ByVal type As TreeType, ByVal span As Span)
        MyBase.New(type, span)
        Debug.Assert(type >= TreeType.ArgumentCollection AndAlso type <= TreeType.DeclarationCollection)
    End Sub

    Protected MustOverride Sub CopyTo(ByVal array As Array, ByVal index As Integer) Implements ICollection.CopyTo

    ''' <summary>
    ''' The number of elements in the collection.
    ''' </summary>
    Public MustOverride ReadOnly Property Count() As Integer Implements ICollection.Count

    Protected MustOverride Function GetIListEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator

    ' NOTE: I'd like to make this "Protected," but there's a bug in the compiler that won't let me do that.
    Friend MustOverride Property IListItem(ByVal index As Integer) As Object Implements IList.Item

    Protected MustOverride Function Contains(ByVal value As Object) As Boolean Implements IList.Contains
    Protected MustOverride Function IndexOf(ByVal value As Object) As Integer Implements IList.IndexOf

    Private ReadOnly Property IsSynchronized() As Boolean Implements ICollection.IsSynchronized
        Get
            Return False
        End Get
    End Property

    ' NOTE: I'd like to make this "Protected," but there's a bug in the compiler that won't let me do that.
    Friend MustOverride ReadOnly Property SyncRoot() As Object Implements ICollection.SyncRoot

    Private ReadOnly Property IsFixedSize() As Boolean Implements IList.IsFixedSize
        Get
            Return True
        End Get
    End Property

    Private ReadOnly Property IsReadOnly() As Boolean Implements IList.IsReadOnly
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