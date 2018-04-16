using System;
using System.Collections.Generic;
using System.Text;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;

namespace PersistentMetadata {
    [NonPersistent]   
    public class MyBaseObject : XPObject {
        public MyBaseObject(Session session, XPClassInfo classInfo) : base(session, classInfo) { }
    }

    public abstract class PersistentTypeInfo : XPObject {
        public PersistentTypeInfo(Session session) : base(session) { }

        string _Name;
        public string Name {
            get { return _Name; }
            set { SetPropertyValue("Name", ref _Name, value); }
        }

        [Association]
        public XPCollection<PersistentAttributeInfo> TypeAttributes { get { return GetCollection<PersistentAttributeInfo>("TypeAttributes"); } }

        protected void CreateAttributes(XPTypeInfo ti) {
            foreach(PersistentAttributeInfo a in TypeAttributes) {
                ti.AddAttribute(a.Create());
            }
        }
    }

    public class PersistentClassInfo : PersistentTypeInfo {
        public static void FillDictionary(XPDictionary dictionary, ICollection<PersistentClassInfo> data) {
            foreach(PersistentClassInfo twc in data) {
                twc.CreateClass(dictionary);
            }
            foreach(PersistentClassInfo twc in data) {
                twc.CreateMembers(dictionary);
            }
        }

        public const string AssemblyName = "";

        public PersistentClassInfo(Session session) : base(session) { }

        protected virtual Type GetDefaultBaseClass() {
            return typeof(MyBaseObject);
        }

        public XPClassInfo CreateClass(XPDictionary dictionary) {
            XPClassInfo result = dictionary.QueryClassInfo(AssemblyName, Name);
            if(result == null) {
                XPClassInfo baseClassInfo;
                if(BaseClass != null)
                    baseClassInfo = BaseClass.CreateClass(dictionary);
                else
                    baseClassInfo = dictionary.GetClassInfo(GetDefaultBaseClass());
                result = dictionary.CreateClass(baseClassInfo, Name);
                CreateAttributes(result);
            }
            return result;
        }

        void CreateMembers(XPDictionary dictionary) {
            XPClassInfo ci = dictionary.GetClassInfo(AssemblyName, Name);
            foreach(PersistentMemberInfo mi in OwnMembers) {
                mi.CreateMember(ci);
            }
        }

        PersistentClassInfo _BaseClass;
        public PersistentClassInfo BaseClass {
            get { return _BaseClass; }
            set { SetPropertyValue("BaseClass", ref _BaseClass, value); }
        }

        [Association]
        public XPCollection<PersistentMemberInfo> OwnMembers { get { return GetCollection<PersistentMemberInfo>("OwnMembers"); } }
    }
    public abstract class PersistentMemberInfo : PersistentTypeInfo {
        public PersistentMemberInfo(Session session) : base(session) { }

        internal XPMemberInfo CreateMember(XPClassInfo owner) {
            XPMemberInfo result = owner.FindMember(Name);
            if(result == null)
                result = CreateMemberCore(owner);
            CreateAttributes(result);
            return result;
        }

        protected abstract XPMemberInfo CreateMemberCore(XPClassInfo owner);

        PersistentClassInfo _Owner;
        [Association]
        public PersistentClassInfo Owner {
            get { return _Owner; }
            set { SetPropertyValue("Owner", ref _Owner, value); }
        }
    }

    public class PersistentReferenceMemberInfo : PersistentMemberInfo {
        public PersistentReferenceMemberInfo(Session session) : base(session) { }

        PersistentClassInfo _ReferenceType;
        public PersistentClassInfo ReferenceType {
            get { return _ReferenceType; }
            set { SetPropertyValue("ReferenceType", ref _ReferenceType, value); }
        }

        protected override XPMemberInfo CreateMemberCore(XPClassInfo owner) {
            XPMemberInfo member = owner.CreateMember(Name, ReferenceType.CreateClass(owner.Dictionary));
            return member;
        }
    }

    public class PersistentCollectionMemberInfo : PersistentMemberInfo {
        public PersistentCollectionMemberInfo(Session session) : base(session) { }

        protected override XPMemberInfo CreateMemberCore(XPClassInfo owner) {
            return owner.CreateMember(Name, typeof(XPCollection), true);
        }
    }

    public class PersistentCoreTypeMemberInfo : PersistentMemberInfo {
        public PersistentCoreTypeMemberInfo(Session session) : base(session) { }

        string _TypeName;
        public string TypeName {
            get { return _TypeName; }
            set { SetPropertyValue("TypeName", ref _TypeName, value); }
        }

        protected override XPMemberInfo CreateMemberCore(XPClassInfo owner) {
            return owner.CreateMember(Name, Type.GetType(TypeName, true));
        }
    }

    public abstract class PersistentAttributeInfo : XPObject {
        public PersistentAttributeInfo(Session session) : base(session) { }

        public abstract Attribute Create();
        
        PersistentTypeInfo _Owner;
        [Association]
        public PersistentTypeInfo Owner {
            get { return _Owner; }
            set { SetPropertyValue("Owner", ref _Owner, value); }
        }
    }

    public class PersistentAssociationAttribute : PersistentAttributeInfo {
        public PersistentAssociationAttribute(Session session) : base(session) { }

        string _an;
        public string AssociationName { get { return _an; } set { SetPropertyValue("AssociationName", ref _an, value); } }

        string _ean;
        public string ElementAssemblyName { get { return _ean; } set { SetPropertyValue("ElementAssemblyName", ref _ean, value); } }

        string _etn;
        public string ElementTypeName { get { return _etn; } set { SetPropertyValue("ElementTypeName", ref _etn, value); } }

        public override Attribute Create() {
            return new AssociationAttribute(AssociationName, ElementAssemblyName, ElementTypeName);
        }
    }
}
