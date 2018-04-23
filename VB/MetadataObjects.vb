Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Text
Imports DevExpress.Xpo
Imports DevExpress.Xpo.Metadata

Namespace PersistentMetadata
	<NonPersistent> _
	Public Class MyBaseObject
		Inherits XPObject
		Public Sub New(ByVal session As Session, ByVal classInfo As XPClassInfo)
			MyBase.New(session, classInfo)
		End Sub
	End Class

	Public MustInherit Class PersistentTypeInfo
		Inherits XPObject
		Public Sub New(ByVal session As Session)
			MyBase.New(session)
		End Sub

		Private _Name As String
		Public Property Name() As String
			Get
				Return _Name
			End Get
			Set(ByVal value As String)
				SetPropertyValue("Name", _Name, value)
			End Set
		End Property

		<Association> _
		Public ReadOnly Property TypeAttributes() As XPCollection(Of PersistentAttributeInfo)
			Get
				Return GetCollection(Of PersistentAttributeInfo)("TypeAttributes")
			End Get
		End Property

		Protected Sub CreateAttributes(ByVal ti As XPTypeInfo)
			For Each a As PersistentAttributeInfo In TypeAttributes
				ti.AddAttribute(a.Create())
			Next a
		End Sub
	End Class

	Public Class PersistentClassInfo
		Inherits PersistentTypeInfo
		Public Shared Sub FillDictionary(ByVal dictionary As XPDictionary, ByVal data As ICollection(Of PersistentClassInfo))
			For Each twc As PersistentClassInfo In data
				twc.CreateClass(dictionary)
			Next twc
			For Each twc As PersistentClassInfo In data
				twc.CreateMembers(dictionary)
			Next twc
		End Sub

		Public Const AssemblyName As String = ""

		Public Sub New(ByVal session As Session)
			MyBase.New(session)
		End Sub

		Protected Overridable Function GetDefaultBaseClass() As Type
			Return GetType(MyBaseObject)
		End Function

		Public Function CreateClass(ByVal dictionary As XPDictionary) As XPClassInfo
			Dim result As XPClassInfo = dictionary.QueryClassInfo(AssemblyName, Name)
			If result Is Nothing Then
				Dim baseClassInfo As XPClassInfo
				If Not BaseClass Is Nothing Then
					baseClassInfo = BaseClass.CreateClass(dictionary)
				Else
					baseClassInfo = dictionary.GetClassInfo(GetDefaultBaseClass())
				End If
				result = dictionary.CreateClass(baseClassInfo, Name)
				CreateAttributes(result)
			End If
			Return result
		End Function

		Private Sub CreateMembers(ByVal dictionary As XPDictionary)
			Dim ci As XPClassInfo = dictionary.GetClassInfo(AssemblyName, Name)
			For Each mi As PersistentMemberInfo In OwnMembers
				mi.CreateMember(ci)
			Next mi
		End Sub

		Private _BaseClass As PersistentClassInfo
		Public Property BaseClass() As PersistentClassInfo
			Get
				Return _BaseClass
			End Get
			Set(ByVal value As PersistentClassInfo)
				SetPropertyValue("BaseClass", _BaseClass, value)
			End Set
		End Property

		<Association> _
		Public ReadOnly Property OwnMembers() As XPCollection(Of PersistentMemberInfo)
			Get
				Return GetCollection(Of PersistentMemberInfo)("OwnMembers")
			End Get
		End Property
	End Class
	Public MustInherit Class PersistentMemberInfo
		Inherits PersistentTypeInfo
		Public Sub New(ByVal session As Session)
			MyBase.New(session)
		End Sub

		Friend Function CreateMember(ByVal owner As XPClassInfo) As XPMemberInfo
			Dim result As XPMemberInfo = owner.GetMember(Name)
			If result Is Nothing Then
				result = CreateMemberCore(owner)
			End If
			CreateAttributes(result)
			Return result
		End Function

		Protected MustOverride Function CreateMemberCore(ByVal owner As XPClassInfo) As XPMemberInfo

		Private _Owner As PersistentClassInfo
		<Association> _
		Public Property Owner() As PersistentClassInfo
			Get
				Return _Owner
			End Get
			Set(ByVal value As PersistentClassInfo)
				SetPropertyValue("Owner", _Owner, value)
			End Set
		End Property
	End Class

	Public Class PersistentReferenceMemberInfo
		Inherits PersistentMemberInfo
		Public Sub New(ByVal session As Session)
			MyBase.New(session)
		End Sub

		Private _ReferenceType As PersistentClassInfo
		Public Property ReferenceType() As PersistentClassInfo
			Get
				Return _ReferenceType
			End Get
			Set(ByVal value As PersistentClassInfo)
				SetPropertyValue("ReferenceType", _ReferenceType, value)
			End Set
		End Property

		Protected Overrides Function CreateMemberCore(ByVal owner As XPClassInfo) As XPMemberInfo
			Dim member As XPMemberInfo = owner.CreateMember(Name, ReferenceType.CreateClass(owner.Dictionary))
			Return member
		End Function
	End Class

	Public Class PersistentCollectionMemberInfo
		Inherits PersistentMemberInfo
		Public Sub New(ByVal session As Session)
			MyBase.New(session)
		End Sub

		Protected Overrides Function CreateMemberCore(ByVal owner As XPClassInfo) As XPMemberInfo
			Return owner.CreateMember(Name, GetType(XPCollection), True)
		End Function
	End Class

	Public Class PersistentCoreTypeMemberInfo
		Inherits PersistentMemberInfo
		Public Sub New(ByVal session As Session)
			MyBase.New(session)
		End Sub

		Private _TypeName As String
		Public Property TypeName() As String
			Get
				Return _TypeName
			End Get
			Set(ByVal value As String)
				SetPropertyValue("TypeName", _TypeName, value)
			End Set
		End Property

		Protected Overrides Function CreateMemberCore(ByVal owner As XPClassInfo) As XPMemberInfo
			Return owner.CreateMember(Name, Type.GetType(TypeName, True))
		End Function
	End Class

	Public MustInherit Class PersistentAttributeInfo
		Inherits XPObject
		Public Sub New(ByVal session As Session)
			MyBase.New(session)
		End Sub

		Public MustOverride Function Create() As Attribute

		Private _Owner As PersistentTypeInfo
		<Association> _
		Public Property Owner() As PersistentTypeInfo
			Get
				Return _Owner
			End Get
			Set(ByVal value As PersistentTypeInfo)
				SetPropertyValue("Owner", _Owner, value)
			End Set
		End Property
	End Class

	Public Class PersistentAssociationAttribute
		Inherits PersistentAttributeInfo
		Public Sub New(ByVal session As Session)
			MyBase.New(session)
		End Sub

		Private _an As String
		Public Property AssociationName() As String
			Get
				Return _an
			End Get
			Set(ByVal value As String)
				SetPropertyValue("AssociationName", _an, value)
			End Set
		End Property

		Private _ean As String
		Public Property ElementAssemblyName() As String
			Get
				Return _ean
			End Get
			Set(ByVal value As String)
				SetPropertyValue("ElementAssemblyName", _ean, value)
			End Set
		End Property

		Private _etn As String
		Public Property ElementTypeName() As String
			Get
				Return _etn
			End Get
			Set(ByVal value As String)
				SetPropertyValue("ElementTypeName", _etn, value)
			End Set
		End Property

		Public Overrides Function Create() As Attribute
			Return New AssociationAttribute(AssociationName, ElementAssemblyName, ElementTypeName)
		End Function
	End Class
End Namespace
